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
			List<Product> products = unitOfWork.Product.GetAll(includeProperties: "Category").ToList();

			return View(products);
		}

		public IActionResult Upsert(int? id)
		{

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
			}
			else
			{
				// update
				productVM.Product = unitOfWork.Product.GetFirstOrDefault(u => u.Id == id, includeProperties: "ProductImages");
				return View(productVM);
			}
		}
		[HttpPost]
		public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
		{
			if (ModelState.IsValid)
			{
				if (productVM.Product.Id == 0)
				{
					unitOfWork.Product.Add(productVM.Product);
				}
				else
				{
					unitOfWork.Product.Update(productVM.Product);
				}

				unitOfWork.Save();

				string wwwRootPath = webHostEnvironment.WebRootPath;
				if (files != null)
				{

					foreach (IFormFile file in files)
					{
						string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
						string productPath = @"images\products\product-" + productVM.Product.Id;
						string finalPath = Path.Combine(wwwRootPath, productPath);

						if(!Directory.Exists(finalPath))
							Directory.CreateDirectory(finalPath);
						
						using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
						{
							file.CopyTo(fileStream);
						}
						ProductImage productImage = new()
						{
							ImageUrl = @"\" + productPath + @"\" + fileName,
							ProductId = productVM.Product.Id,
						};

						if(productVM.Product.ProductImages == null)
							productVM.Product.ProductImages = new List<ProductImage>();

						productVM.Product.ProductImages.Add(productImage);
					}

					unitOfWork.Product.Update(productVM.Product);
					unitOfWork.Save();
				}

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

		public IActionResult DeleteImage(int imageId)
		{
			var imageToBeDeleted = unitOfWork.ProductImage.GetFirstOrDefault(x => x.Id == imageId);
			int productId = imageToBeDeleted.ProductId;

			if (imageToBeDeleted != null)
			{
				if(!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
				{
					var oldImagePath = Path.Combine(webHostEnvironment.WebRootPath,
								 imageToBeDeleted.ImageUrl.TrimStart('\\'));

					if (System.IO.File.Exists(oldImagePath))
					{
						System.IO.File.Delete(oldImagePath);
					}
				}

				unitOfWork.ProductImage.Remove(imageToBeDeleted);
				unitOfWork.Save();

				TempData["success"] = "Image deleted successfully";
			}

			return RedirectToAction(nameof(Upsert), new { id = productId });
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
			if (productToBeDeleted == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}

			string productPath = @"images\products\product-" + id;
			string finalPath = Path.Combine(webHostEnvironment.WebRootPath, productPath);

			if (Directory.Exists(finalPath))
			{
				string[] filePaths = Directory.GetFiles(finalPath);

				foreach (string filePath in filePaths)
				{
					System.IO.File.Delete(filePath);
				}
				Directory.Delete(finalPath);
			}

			unitOfWork.Product.Remove(productToBeDeleted);
			unitOfWork.Save();

			return Json(new { success = true, message = "Delete successful" });
		}
		#endregion
	}
}
