using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CardController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        [BindProperty]
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
                ShoppingCardList = unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };
            IEnumerable<ProductImage> productImages = unitOfWork.ProductImage.GetAll();

            foreach(var card in ShoppingCardVM.ShoppingCardList)
            {
                card.Product.ProductImages = productImages.Where(x => x.ProductId == card.Product.Id).ToList();
                card.Price= GetPriceBasedOnQuantity(card);
                ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
            }

            return View(ShoppingCardVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCardVM = new()
            {
                ShoppingCardList = unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCardVM.OrderHeader.ApplicationUser = unitOfWork.User.GetFirstOrDefault(u => u.Id == userId);

            ShoppingCardVM.OrderHeader.Name = ShoppingCardVM.OrderHeader.ApplicationUser.Name;
            ShoppingCardVM.OrderHeader.PhoneNumber = ShoppingCardVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCardVM.OrderHeader.StreetAddress = ShoppingCardVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCardVM.OrderHeader.City = ShoppingCardVM.OrderHeader.ApplicationUser.City;
            ShoppingCardVM.OrderHeader.State = ShoppingCardVM.OrderHeader.ApplicationUser.State;
            ShoppingCardVM.OrderHeader.PostalCode = ShoppingCardVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var card in ShoppingCardVM.ShoppingCardList)
            {
                card.Price = GetPriceBasedOnQuantity(card);
                ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
            }
            return View(ShoppingCardVM);
        }

        [HttpPost]
        [ActionName(nameof(Summary))]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCardVM.ShoppingCardList = unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

            ShoppingCardVM.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCardVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = unitOfWork.User.GetFirstOrDefault(u => u.Id == userId);

			foreach (var card in ShoppingCardVM.ShoppingCardList)
			{
				card.Price = GetPriceBasedOnQuantity(card);
				ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
			}

            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
            else
            {
				ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}

            unitOfWork.OrderHeader.Add(ShoppingCardVM.OrderHeader);
            unitOfWork.Save();

            foreach (var card in ShoppingCardVM.ShoppingCardList) 
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = card.ProductId,
                    OrderHeaderId = ShoppingCardVM.OrderHeader.Id,
                    Price = card.Price,
                    Count = card.Count
                };

                unitOfWork.OrderDetail.Add(orderDetail);
                unitOfWork.Save();
            }

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				var domain = $"{Request.Scheme}://{Request.Host.Value}/";
				var options = new SessionCreateOptions
                {
                    SuccessUrl = $"{domain}customer/card/OrderConfirmation?id={ShoppingCardVM.OrderHeader.Id}",
                    CancelUrl = $"{domain}customer/card/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment" 
                };

                foreach(var item in ShoppingCardVM.ShoppingCardList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);

                unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCardVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
			}

			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCardVM.OrderHeader.Id });
		}

        public IActionResult OrderConfirmation(int id)
        {

            OrderHeader orderHeader = unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser");
            
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service  = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if(session.PaymentStatus.ToLower() == "paid")
                {
					unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    unitOfWork.Save();
				}
                HttpContext.Session.Clear();
			}

            List<ShoppingCard> shoppingCards = unitOfWork.ShoppingCard
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            unitOfWork.ShoppingCard.RemoveRange(shoppingCards);
            unitOfWork.Save();

            return View(id);
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
            var cardFromDb = unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId, tracked: true);
            
            if(cardFromDb.Count <= 1)
            {
				HttpContext.Session.SetInt32(SD.SessionCart, unitOfWork.ShoppingCard.GetAll(x => x.ApplicationUserId == cardFromDb.ApplicationUserId).Count() - 1);
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
            var cardFromDb = unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId, tracked: true);

            unitOfWork.ShoppingCard.Remove(cardFromDb);
			HttpContext.Session.SetInt32(SD.SessionCart, unitOfWork.ShoppingCard.GetAll(x => x.ApplicationUserId == cardFromDb.ApplicationUserId).Count() - 1);
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
