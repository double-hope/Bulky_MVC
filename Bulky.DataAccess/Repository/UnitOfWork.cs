using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ApplicationDbContext db;
		public ICategoryRepository Category { get; private set; }
		public IProductRepository Product { get; private set; }
		public ICompanyRepository Company { get; private set; }
		public IShoppingCardRepository ShoppingCard { get; private set; }
		public IApplicationUserRepository User { get; private set; }
		public IOrderHeaderRepository OrderHeader { get; private set; }
		public IOrderDetailRepository OrderDetail { get; private set; }
		public UnitOfWork(ApplicationDbContext db)
		{
			this.db = db;
			Category = new CategoryRepository(db);
			Product = new ProductRepository(db);
			Company = new CompanyRepository(db);
			ShoppingCard = new ShoppingCardRepository(db);
			User = new ApplicationUserRepository(db);
			OrderHeader = new OrderHeaderRepository(db);
			OrderDetail = new OrderDetailRepository(db);
		}

		public void Save()
		{
			db.SaveChanges();
		}
	}
}
