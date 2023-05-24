using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class ProductController : Controller
	{
		private readonly IUnitOfWork unitOfWork;

        public ProductController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Product> products = unitOfWork.Product.GetAll().ToList();
            return View(products);
        }

        public IActionResult Create() { 
            return View();
        }
        [HttpPost]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                unitOfWork.Product.Add(product);
                unitOfWork.Save();
				TempData["success"] = "Product created successfully";
				return RedirectToAction(nameof(Index));
            }

            return View();
        }

        public IActionResult Edit(int? id) {
        
            if(id == null || id == 0)
            {
				return NotFound();
            }
            Product? product = unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
            if (product == null)
            {
				return NotFound();
            }

            return View(product);
        }

		[HttpPost]
		public IActionResult Edit(Product product)
		{
            if(ModelState.IsValid)
            {
                unitOfWork.Product.Update(product);
                unitOfWork.Save();
				TempData["success"] = "Product updated successfully";
				return RedirectToAction(nameof(Index));
            }
            return View();
		}
		public IActionResult Delete(int? id) {
			if (id == null || id == 0)
			{
				return NotFound();
			}
			Product? product = unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
			if (product == null)
			{
				return NotFound();
			}

			return View(product);
        }

        [HttpPost, ActionName("Delete")]
		public IActionResult DeletePOST(int? id)
		{
			Product? product = unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
			
            if (product == null)
			{
				return NotFound();
			}
            unitOfWork.Product.Remove(product);
            unitOfWork.Save();
			TempData["success"] = "Product deleted successfully";
			return RedirectToAction(nameof(Index));
		}
	}
}
