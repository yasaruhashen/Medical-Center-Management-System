namespace UMCMS.Domain.Entities
{
    /// <summary>
    /// A received stock batch for an inventory item. FEFO dispensing draws from the
    /// earliest-expiring batch first.
    /// </summary>
    public class InventoryBatch
    {
        public int Id { get; set; }
        public int InventoryItemId { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; }
    }
}
