using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{

		private readonly IUnitOfWork unitOfWork;
		[BindProperty]
		public OrderVM OrderVM { get; set; }
		public OrderController(IUnitOfWork unitOfWork)
		{
			this.unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Details(int orderId)
		{
			OrderVM = new()
			{
				OrderHeader = unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderId, includeProperties: "ApplicationUser"),
				OrderDetail = unitOfWork.OrderDetail.GetAll(x => x.OrderHeaderId == orderId, includeProperties: "Product")
			};

			return View(OrderVM);
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult UpdateOrderdetail()
		{
			var orderHeaderFromDb = unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id);
			orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
			orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
			orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
			orderHeaderFromDb.City = OrderVM.OrderHeader.City;
			orderHeaderFromDb.State = OrderVM.OrderHeader.State;
			orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

			if(!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier)) {
				orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
			}

			if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
			{
				orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			}

			unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			unitOfWork.Save();

			TempData["Success"] = "Order Details Updated Succesfully";

			return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult StartProcessing()
		{
			unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
			unitOfWork.Save();
			TempData["Success"] = "Order Details Updated Succesfully";

			return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult ShipOrder()
		{
			var orderHeader = unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
			orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
			orderHeader.OrderStatus = SD.StatusShipped;
			orderHeader.ShippingDate = DateTime.Now;

			if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
			}

			unitOfWork.OrderHeader.Update(orderHeader);
			unitOfWork.Save();
			TempData["Success"] = "Order Shipped Succesfully";

			return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult CancelOrder()
		{
			var orderHeader = unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);

			if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeader.PaymentIntentId
				};

				var service = new RefundService();
				Refund refund = service.Create(options);

				unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
			}
			else
			{
				unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
			}

			unitOfWork.Save();
			TempData["Success"] = "Order Cancelled Succesfully";

			return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
		}

		[ActionName("Details")]
		[HttpPost]
		public IActionResult Details_PAY_NOW()
		{
			OrderVM.OrderHeader = unitOfWork.OrderHeader
				.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
			OrderVM.OrderDetail = unitOfWork.OrderDetail
				.GetAll(x => x.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

			var domain = "https://localhost:7096/";
			var options = new SessionCreateOptions
			{
				SuccessUrl = $"{domain}admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
				CancelUrl = $"{domain}admin/order/details?orderId={OrderVM.OrderHeader.Id}",
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment"
			};

			foreach (var item in OrderVM.OrderDetail)
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

			unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			unitOfWork.Save();

			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}

		public IActionResult PaymentConfirmation(int orderHeaderId)
		{

			OrderHeader orderHeader = unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderId);

			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
					unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
					unitOfWork.Save();
				}
			}
			return View(orderHeaderId);
		}

		#region APICALLS
		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaders;
			if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
			{
				orderHeaders = unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
			}
			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

				orderHeaders = unitOfWork.OrderHeader
					.GetAll(x => x.ApplicationUserId.Equals("17862997-f656-4325-953a-5011690e05e8"), includeProperties: "ApplicationUser");
			}

			switch (status)
			{
				case "inprocess":
					orderHeaders = orderHeaders.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
					break;
				case "pending":
					orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess);
					break;
				case "completed":
					orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusShipped);
					break;
				case "approved":
					orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusApproved);
					break;
				default:
					break;
			}

			return Json(new { data = orderHeaders });
		}
		#endregion
	}
}
