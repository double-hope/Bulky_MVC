using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
	public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
	{
		private readonly ApplicationDbContext db;
		public ProductImageRepository(ApplicationDbContext db) : base(db)
		{
			this.db = db;
		}

		public void Update(ProductImage productImage)
		{
			db.ProductImages.Update(productImage);
		}
	}
}
