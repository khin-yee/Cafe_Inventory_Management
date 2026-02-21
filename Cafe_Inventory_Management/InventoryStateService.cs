namespace Cafe_Inventory_Management.UI
{
    public class InventoryStateService
    {
        public event Action OnStockChanged;

        // The method the Order page will call to trigger the alert
        public void NotifyStockChanged()
        {
            OnStockChanged?.Invoke();
        }
    }
}
