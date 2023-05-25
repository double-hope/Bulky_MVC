using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IWebHostEnvironment webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            this.unitOfWork = unitOfWork;
            this.webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> products = unitOfWork.Product.GetAll(includeProperties:"Category").ToList();

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
                string wwwRootPath = webHostEnvironment.WebRootPath;
                if(file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPaht = Path.Combine(wwwRootPath, @"images\product");

                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPaht, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productVM.Product.ImageUrl = @"\images\product\" + fileName;
                }

                if(productVM.Product.Id == 0)
                {
                    unitOfWork.Product.Add(productVM.Product);
                }
                else
                {
					unitOfWork.Product.Update(productVM.Product);
				}
                
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

        #region APICALLS
        [HttpGet]
        public IActionResult GetAll() 
        {
			List<Product> products = unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = products });
		}

        [HttpDelete]
		public IActionResult Delete(int? id)
		{
			Product? productToBeDeleted = unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
			if(productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

			var oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, 
                productToBeDeleted.ImageUrl.TrimStart('\\'));

			if (System.IO.File.Exists(oldImagePath))
			{
				System.IO.File.Delete(oldImagePath);
			}

            unitOfWork.Product.Remove(productToBeDeleted); 
            unitOfWork.Save();

			return Json(new { success = true, message = "Delete successful" });
		}
		#endregion
	}
}
