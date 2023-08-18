using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class UserController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly UserManager<IdentityUser> _userManager;
		private readonly IUnitOfWork _unitOfWork;
		[BindProperty]
		public UserVM UserVM { get; set; }
		public UserController(ApplicationDbContext db, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
		{
			_db = db;
			_roleManager = roleManager;
			_userManager = userManager;
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult RoleManagement(string userId)
		{
			var user = _unitOfWork.User.GetFirstOrDefault(x => x.Id == userId, includeProperties: "Company");
			var userRoles = _db.UserRoles.ToList();
			var roles = _db.Roles.ToList();
			var roleId = userRoles.FirstOrDefault(x => x.UserId == user.Id).RoleId;
			UserVM = new()
			{
				Id = user.Id,
				RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
				{
					Text = i,
					Value = i
				}),
				CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
				{
					Text = i.Name,
					Value = i.Id.ToString()
				}),
				Name = user.Name,
				Role = roles.FirstOrDefault(x => x.Id == roleId).Name,
			};
			if (user.CompanyId != null)
			{
				UserVM.CompanyId = user.CompanyId;
			}

			return View(UserVM);
		}

		[HttpPost]
		public async Task<IActionResult> RoleManagement()
		{
			var user = _unitOfWork.User.GetFirstOrDefault(x => x.Id == UserVM.Id, includeProperties: "Company", tracked: true);
			var userRoles = _db.UserRoles.ToList();
			var currentRole = userRoles.FirstOrDefault(x => x.UserId == user.Id);
			var roleId = _db.Roles.FirstOrDefault(x => x.Name == UserVM.Role).Id;

			if (roleId != currentRole.RoleId)
			{
				_db.UserRoles.Remove(currentRole);

				await _userManager.AddToRoleAsync(user, UserVM.Role);

				if(UserVM.Role == SD.Role_Company)
				{
					user.CompanyId = UserVM.CompanyId;
				}
				else
				{
					user.CompanyId = null;
				}
				_db.ApplicationUsers.Update(user);
				_db.SaveChanges();
			}
			else if (UserVM.CompanyId != null)
			{
				user.CompanyId = UserVM.CompanyId;
				_db.ApplicationUsers.Update(user);
				_db.SaveChanges();
			}

			TempData["success"] = "User changed successfully";

			return RedirectToAction(nameof(Index));
		}

		#region APICALLS
		[HttpGet]
		public IActionResult GetAll()
		{
			List<ApplicationUser> users = _db.ApplicationUsers.Include(x => x.Company).ToList();

			var userRoles = _db.UserRoles.ToList();
			var roles = _db.Roles.ToList();

			foreach (var user in users)
			{

				var roleId = userRoles.FirstOrDefault(x => x.UserId == user.Id).RoleId;
				user.Role = roles.FirstOrDefault(x => x.Id == roleId).Name;

				if (user.Company == null)
				{
					user.Company = new Company
					{
						Name = "",
					};
				}
			}

			return Json(new { data = users });
		}
		[HttpPost]
		public IActionResult LockUnlock([FromBody] string id)
		{
			var user = _db.ApplicationUsers.FirstOrDefault(x => x.Id == id);
			if (user == null)
			{
				return Json(new { success = false, message = "Error while Locking/Unlocking" });
			}

			if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
			{
				user.LockoutEnd = DateTime.Now;
			}
			else
			{
				user.LockoutEnd = DateTime.Now.AddYears(1000);
			}
			_db.SaveChanges();
			return Json(new { success = true, message = "Lock/Unlock successful" });
		}
		#endregion
	}
}
