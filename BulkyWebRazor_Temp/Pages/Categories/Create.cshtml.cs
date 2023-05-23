using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    public class CreateModel : PageModel
	{
		private readonly ApplicationDbContext db;
		[BindProperty]
		public Category Category { get; set; }
		public CreateModel(ApplicationDbContext db)
		{
			this.db = db;
		}
		public void OnGet()
        {
        }

		public IActionResult OnPost() 
		{
			db.Categories.Add(Category);
			db.SaveChanges();
			TempData["success"] = "Category created successfully";
			return RedirectToPage(nameof(Index));
		}
    }
}
