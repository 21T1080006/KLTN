using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Pages.Manager
{
    public class BranchModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BranchModel(ApplicationDbContext context) => _context = context;

        public List<Branch> Branches { get; set; } = [];

        public async Task OnGetAsync()
        {
            Branches = await _context.Branches
                .Include(b => b.Company)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.Company)
                .FirstOrDefaultAsync(b => b.AutoID == id);

            if (branch == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                branch.AutoID,
                branch.BranchName,
                branch.BranchSymbol,
                branch.Address,
                branch.City,
                branch.Country,
                branch.Phone,
                branch.HomePhone,
                branch.Representative,
                branch.IsActive,
                CompanyName = branch.Company?.CompanyName
            });
        }
    }
}