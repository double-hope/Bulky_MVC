using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> products = unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(products);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCard shoppingCard = new()
            {
                ProductId = productId,
                Product = unitOfWork.Product.GetFirstOrDefault(u => u.Id == productId, includeProperties: "Category"),
                Count = 1
            };
            
            return View(shoppingCard);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCard shoppingCard)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCard.ApplicationUserId = userId;

            ShoppingCard cardFromDb = unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.ApplicationUserId == userId && u.ProductId == shoppingCard.ProductId);

            if (cardFromDb != null)
            {
                cardFromDb.Count += shoppingCard.Count;
                unitOfWork.ShoppingCard.Update(cardFromDb);
            }else
            {
                unitOfWork.ShoppingCard.Add(shoppingCard);
            }

            TempData["success"] = "Card updated succesfully";
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}