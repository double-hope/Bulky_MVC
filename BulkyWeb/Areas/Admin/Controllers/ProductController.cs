using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        public IActionResult Upsert(int? id) {

            ProductVM productVM = new()
            {
                CategoryList = unitOfWork.Category
                .GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };

            if (id == null || id == 0)
            {
                // create
                return View(productVM);
            } else
            {
                // update
                productVM.Product = unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
                return View(productVM);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                unitOfWork.Product.Add(productVM.Product);
                unitOfWork.Save();
				TempData["success"] = "Product created successfully";
				return RedirectToAction(nameof(Index));
            }
            else
            {
                productVM.CategoryList = unitOfWork.Category
                .GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });

                return View(productVM);
			}

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
