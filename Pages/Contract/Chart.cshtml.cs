using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Text.Json;
using System.Security.Claims;

namespace CMS.Pages.Contract
{
    public class ChartModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ChartModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<StatusStats> StatusStatistics { get; set; }
        public List<QuarterStats> QuarterStatistics { get; set; }

        public async Task OnGetAsync()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Company)
                .Include(c => c.Branch)
                .Include(c => c.Department)
                .Where(c => !c.IsDeleted && c.IsActive)
                .ToListAsync();

            StatusStatistics = contracts
                .GroupBy(c => c.ContractStatus)
                .Select(g => new StatusStats
                {
                    ContractStatus = g.Key,
                    ContractCount = g.Count()
                })
                .OrderBy(s => s.ContractStatus)
                .ToList();

            QuarterStatistics = contracts
                .GroupBy(c => new
                {
                    Quarter = (c.StartDate.Value.Month - 1) / 3 + 1,
                    c.ContractYear
                })
                .Select(g => new QuarterStats
                {
                    Quarter = g.Key.Quarter,
                    ContractYear = g.Key.ContractYear,
                    ContractCount = g.Count()
                })
                .OrderBy(s => s.ContractYear)
                .ThenBy(s => s.Quarter)
                .ToList();
        }

        public async Task<IActionResult> OnGetDownloadExcelAsync()
        {
            await OnGetAsync();

            ExcelPackage.License.SetNonCommercialPersonal("CMS");

            try
            {
                using (var package = new ExcelPackage())
                {
                    var statusSheet = package.Workbook.Worksheets.Add("Status Statistics");
                    statusSheet.Cells[1, 1].Value = "Contract Status";
                    statusSheet.Cells[1, 2].Value = "Contract Count";

                    for (int i = 0; i < StatusStatistics.Count; i++)
                    {
                        statusSheet.Cells[i + 2, 1].Value = StatusStatistics[i].ContractStatus;
                        statusSheet.Cells[i + 2, 2].Value = StatusStatistics[i].ContractCount;
                    }

                    int statusTotalRow = StatusStatistics.Count + 2;
                    statusSheet.Cells[statusTotalRow, 1].Value = "Total";
                    statusSheet.Cells[statusTotalRow, 2].Value = StatusStatistics.Sum(s => s.ContractCount);

                    var statusRange = statusSheet.Cells[1, 1, statusTotalRow, 2];
                    statusRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    statusRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    var quarterSheet = package.Workbook.Worksheets.Add("Quarter Statistics");
                    quarterSheet.Cells[1, 1].Value = "Year";
                    quarterSheet.Cells[1, 2].Value = "Quarter";
                    quarterSheet.Cells[1, 3].Value = "Contract Count";
                    quarterSheet.Cells[1, 4].Value = "Total";

                    var groupedByYear = QuarterStatistics.GroupBy(q => q.ContractYear).OrderBy(g => g.Key);
                    int currentRow = 2;
                    int totalContracts = 0;

                    foreach (var yearGroup in groupedByYear)
                    {
                        var yearData = yearGroup.OrderBy(q => q.Quarter).ToList();
                        int yearStartRow = currentRow;
                        int yearTotal = yearData.Sum(q => q.ContractCount);
                        totalContracts += yearTotal;

                        foreach (var quarter in yearData)
                        {
                            quarterSheet.Cells[currentRow, 2].Value = quarter.Quarter;
                            quarterSheet.Cells[currentRow, 3].Value = quarter.ContractCount;
                            currentRow++;
                        }

                        if (yearData.Count > 0)
                        {
                            quarterSheet.Cells[yearStartRow, 4, currentRow - 1, 4].Merge = true;
                            quarterSheet.Cells[yearStartRow, 4].Value = yearTotal;
                        }

                        quarterSheet.Cells[yearStartRow, 1].Value = yearGroup.Key;
                        if (yearData.Count > 1)
                        {
                            quarterSheet.Cells[yearStartRow, 1, currentRow - 1, 1].Merge = true;
                        }
                    }

                    quarterSheet.Cells[currentRow, 3].Value = "Grand Total";
                    quarterSheet.Cells[currentRow, 4].Value = totalContracts;

                    var quarterRange = quarterSheet.Cells[1, 1, currentRow, 4];
                    quarterRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    quarterRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    statusSheet.Cells[statusSheet.Dimension.Address].AutoFitColumns();
                    quarterSheet.Cells[quarterSheet.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    var fileName = $"ThongKe_{DateTime.Now:dd-MM-yyyy_HH:mm:ss}.xlsx";

                    // Ghi log cho hành động tải file Excel
                    var email = User.FindFirstValue(ClaimTypes.Email);
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (user != null)
                    {
                        var statsData = JsonSerializer.Serialize(new
                        {
                            StatusCount = StatusStatistics.Count,
                            QuarterCount = QuarterStatistics.Count
                        });
                        await LogAuditAsync(
                            userId: user.Id,
                            table: "",
                            action: "Download",
                            note: $"Tải file Excel thống kê hợp đồng (Status: {StatusStatistics.Count}, Quarters: {QuarterStatistics.Count})",
                            data: statsData
                        );
                    }

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error generating Excel file: " + ex.Message);
            }
        }

        private async Task LogAuditAsync(string userId, string table, string action, string note, string data)
        {
            var auditLog = new AuditLog
            {
                UserID = userId,
                Tables = table,
                Action = action,
                Note = note,
                Data = data,
                CreateDate = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public class StatusStats
        {
            public string ContractStatus { get; set; }
            public int ContractCount { get; set; }
        }

        public class QuarterStats
        {
            public int Quarter { get; set; }
            public string ContractYear { get; set; }
            public int ContractCount { get; set; }
        }
    }
}