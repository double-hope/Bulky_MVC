namespace Bulky.Models.ViewModels
{
    public class ShoppingCardVM
    {
        public IEnumerable<ShoppingCard> ShoppingCardList { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
