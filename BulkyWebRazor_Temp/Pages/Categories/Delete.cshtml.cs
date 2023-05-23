using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext db;
        [BindProperty]
        public Category Category { get; set; }

        public DeleteModel(ApplicationDbContext db)
        {
            this.db = db;
        }
        public void OnGet(int? id)
        {
            if(id != null && id != 0)
            {
                Category = db.Categories.FirstOrDefault(c => c.Id == id);
            }
        }

        public IActionResult OnPost()
        {
			Category? category = db.Categories.Find(Category.Id);
			if (category == null)
			{
				return NotFound();
			}
			db.Categories.Remove(category);
            db.SaveChanges();
			TempData["success"] = "Category deleted successfully";
			return RedirectToPage(nameof(Index));
        }
    }
}
