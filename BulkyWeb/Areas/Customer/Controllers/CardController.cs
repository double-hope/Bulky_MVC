using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CardController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        public ShoppingCardVM ShoppingCardVM { get; set; }

        public CardController(IUnitOfWork unitOfWork) { 
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCardVM = new()
            {
                ShoppingCardList = unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product")
            };

            foreach(var card in ShoppingCardVM.ShoppingCardList)
            {
                card.Price= GetPriceBasedOnQuantity(card);
                ShoppingCardVM.OrderTotal += (card.Price * card.Count);
            }

            return View(ShoppingCardVM);
        }

        public IActionResult Summary()
        {
            return View();
        }

        public IActionResult Plus(int cardId)
        {
            var cardFromDb = unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId);
            cardFromDb.Count++;
            unitOfWork.ShoppingCard.Update(cardFromDb);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int cardId)
        {
            var cardFromDb = unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId);
            
            if(cardFromDb.Count <= 1)
            {
                unitOfWork.ShoppingCard.Remove(cardFromDb);
            }
            else
            {
                cardFromDb.Count--;
                unitOfWork.ShoppingCard.Update(cardFromDb);
            }
            
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cardId)
        {
            var cardFromDb = unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId);

            unitOfWork.ShoppingCard.Remove(cardFromDb);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCard shoppingCard)
        {
            if(shoppingCard.Count <= 50)
            {
                return shoppingCard.Product.Price;
            }
            else
            {
                if(shoppingCard.Count <= 1000)
                {
                    return shoppingCard.Product.Price50;
                }
                else
                {
                    return shoppingCard.Product.Price100;
                }
            }
        }
    }
}
