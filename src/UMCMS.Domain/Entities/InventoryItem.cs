namespace UMCMS.Domain.Entities
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;

        /// <summary>"General" (medication) or "Dental" (supplies).</summary>
        public string Stream { get; set; } = "General";
        public string Category { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int ReorderLevel { get; set; }

        /// <summary>On-hand quantity (sum of non-expired batches; cached for quick display).</summary>
        public int Quantity { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsLowStock => Quantity <= ReorderLevel;
    }
}
