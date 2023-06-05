namespace Bulky.DataAccess.Repository.IRepository
{
	public interface IUnitOfWork
	{
		ICategoryRepository Category { get; }
		IProductRepository Product { get; }
		ICompanyRepository Company { get; }
        public IShoppingCardRepository ShoppingCard { get; }
        public IApplicationUserRepository User { get; }

        void Save();
	}
}
