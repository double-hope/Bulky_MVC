using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ShoppingCardRepository : Repository<ShoppingCard>, IShoppingCardRepository 
    {
        private readonly ApplicationDbContext db;

        public ShoppingCardRepository(ApplicationDbContext db) : base(db)
        {
            this.db = db;
        }

        public void Update(ShoppingCard shoppingCard)
        {
            db.ShoppingCards.Update(shoppingCard);
        }
    }
}
