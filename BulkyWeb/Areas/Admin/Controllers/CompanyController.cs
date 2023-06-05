using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
	{
        private readonly IUnitOfWork unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public IActionResult Index()
		{
            
			List<Company> companies = unitOfWork.Company.GetAll().ToList();
			return View(companies);
		}
		public IActionResult Upsert(int? id)
		{

			Company company = new Company();

			if (id == null || id == 0)
			{
				// create
				return View(company);
			}
			else
			{
				// update
				company = unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
				return View(company);
			}
		}
		[HttpPost]
		public IActionResult Upsert(Company company)
		{
			if (ModelState.IsValid)
			{

				if (company.Id == 0)
				{
					unitOfWork.Company.Add(company);
				}
				else
				{
					unitOfWork.Company.Update(company);
				}

				unitOfWork.Save();
				TempData["success"] = "Company created successfully";
				return RedirectToAction(nameof(Index));
			}
			else
			{
				return View(company);
			}

		}

		#region APICALLS
		[HttpGet]
		public IActionResult GetAll()
		{
			List<Company> companies = unitOfWork.Company.GetAll().ToList();
			return Json(new { data = companies });
		}

		[HttpDelete]
		public IActionResult Delete(int? id)
		{
			Company? companyToBeDeleted = unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
			if (companyToBeDeleted == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}

			unitOfWork.Company.Remove(companyToBeDeleted);
			unitOfWork.Save();

			return Json(new { success = true, message = "Delete successful" });
		}
		#endregion
	}
}
