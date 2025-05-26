using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Pages.Manager
{
    public class DepartmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DepartmentModel(ApplicationDbContext context) => _context = context;

        // Sử dụng Dictionary để lưu các phòng ban theo chi nhánh
        public Dictionary<Branch, List<Department>> DepartmentsByBranch { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Lấy danh sách phòng ban và chi nhánh
            var departments = await _context.Departments
                .Include(d => d.Branch)
                .ToListAsync();

            // Nhóm phòng ban theo chi nhánh
            DepartmentsByBranch = departments
                .GroupBy(d => d.Branch ?? new Branch { BranchName = "No Branch" }) // Xử lý trường hợp không có chi nhánh
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Branch)
                .FirstOrDefaultAsync(d => d.AutoID == id);

            if (department == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                department.AutoID,
                department.DepartmentName,
                department.DepartmentSymbol,
                department.DepartmentDescription,
                department.Representative,
                department.IsActive,
                BranchName = department.Branch?.BranchName
            });
        }
    }
}