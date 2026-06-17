namespace UMCMS.Domain.Entities
{
    public class Prescription
    {
        public int Id { get; set; }
        public int MedicalRecordId { get; set; }
        public int? InventoryItemId { get; set; }   // linked stock item (deducted on dispense)
        public string Medicine { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;

        /// <summary>Doses per day (e.g. 3 = TDS).</summary>
        public int TimesPerDay { get; set; } = 1;

        /// <summary>"Before meals", "After meals", "With meals".</summary>
        public string MealTiming { get; set; } = "After meals";

        public bool Dispensed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
