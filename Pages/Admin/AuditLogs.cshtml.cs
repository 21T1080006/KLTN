using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CMS.Pages.AuditLogs
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public List<User> Users { get; set; } = new List<User>();

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TableName { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Load users
            Users = await _context.Users.ToListAsync();

            // Build query
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(UserId))
            {
                query = query.Where(a => a.UserID == UserId);
            }

            if (!string.IsNullOrEmpty(TableName))
            {
                query = query.Where(a => a.Tables == TableName);
            }

            if (FromDate.HasValue)
            {
                query = query.Where(a => a.CreateDate >= FromDate.Value);
            }

            if (ToDate.HasValue)
            {
                query = query.Where(a => a.CreateDate <= ToDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            // Load all data without pagination
            AuditLogs = await query
                .OrderByDescending(a => a.CreateDate)
                .ToListAsync();

            return Page();
        }
    }
}