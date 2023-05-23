using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext db;
        [BindProperty]
        public Category Category { get; set; }

        public EditModel(ApplicationDbContext db)
        {
            this.db = db;            
        }

        public void OnGet(int? id)
        {
            if(id != null && id != 0)
            {
                Category = db.Categories.SingleOrDefault(c => c.Id == id);
            }
            
        }

        public IActionResult OnPost()
        {
			if (ModelState.IsValid)
			{
				db.Categories.Update(Category);
				db.SaveChanges();
				TempData["success"] = "Category updated successfully";
				return RedirectToPage(nameof(Index));
			}

            return Page();
        }
    }
}
