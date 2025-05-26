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
    public class CompanyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CompanyModel(ApplicationDbContext context) => _context = context;

        public List<Company> Companies { get; set; } = [];

        public async Task OnGetAsync()
        {
            Companies = await _context.Companies
                .Include(c => c.Corporation)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var company = await _context.Companies
                .Include(c => c.Corporation)
                .FirstOrDefaultAsync(c => c.AutoID == id);

            if (company == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                company.AutoID,
                company.CompanyName,
                company.CompanySymbol,
                company.Address,
                company.City,
                company.Country,
                company.Phone,
                company.HomePhone,
                company.Representative,
                company.IsActive,
                CorporationName = company.Corporation?.CorporationName 
            });
        }
    }
}