namespace Bulky.DataAccess.Repository.IRepository
{
	public interface IUnitOfWork
	{
		ICategoryRepository Category { get; }
		IProductRepository Product { get; }
		IProductImageRepository ProductImage { get; }
		ICompanyRepository Company { get; }
        IShoppingCardRepository ShoppingCard { get; }
        IApplicationUserRepository User { get; }
		IOrderHeaderRepository OrderHeader { get; }
		IOrderDetailRepository OrderDetail { get; }

        void Save();
	}
}
