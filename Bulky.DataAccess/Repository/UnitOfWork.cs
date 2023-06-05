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
		public UnitOfWork(ApplicationDbContext db)
		{
			this.db = db;
			Category = new CategoryRepository(db);
			Product = new ProductRepository(db);
			Company = new CompanyRepository(db);
			ShoppingCard = new ShoppingCardRepository(db);
			User = new ApplicationUserRepository(db);
		}

		public void Save()
		{
			db.SaveChanges();
		}
	}
}
