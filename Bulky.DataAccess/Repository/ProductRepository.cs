using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
	public class ProductRepository : Repository<Product>, IProductRepository
	{
		private readonly ApplicationDbContext db;
		public ProductRepository(ApplicationDbContext db) : base(db)
		{
			this.db = db;
		}
		public void Update(Product product)
		{
			var productFromDb = db.Products.FirstOrDefault(u => u.Id == product.Id);
			if(productFromDb != null) { 
				productFromDb.Title = product.Title;
				productFromDb.Description = product.Description;
				productFromDb.ISBN = product.ISBN;
				productFromDb.Price = product.Price;
				productFromDb.Price50 = product.Price50;
				productFromDb.Price100 = product.Price100;
				productFromDb.ListPrice = product.ListPrice;
				productFromDb.CategoryId = product.CategoryId;
				productFromDb.ProductImages = product.ProductImages;
			}
		}
	}
}
