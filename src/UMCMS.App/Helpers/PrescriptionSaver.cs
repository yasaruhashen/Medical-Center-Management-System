using System.Text;
using UMCMS.App.Views;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Helpers
{
    /// <summary>
    /// Persists prescription lines for a medical record. Either sends them to the
    /// pharmacy (Dispensed=false) or dispenses them on the spot (FEFO deduct, Dispensed=true).
    /// Stock-short items during a dispense-here fall back to the pharmacy queue.
    /// </summary>
    public static class PrescriptionSaver
    {
        /// <returns>(fellBackToPharmacy, summary) — summary describes per-line dispense outcomes.</returns>
        public static (bool fellBackToPharmacy, string summary) Save(
            int medicalRecordId, IReadOnlyList<RxLine> lines, bool dispenseHere)
        {
            var report = new StringBuilder();
            bool fellBack = false;

            foreach (var line in lines)
            {
                bool dispensed = false;

                if (dispenseHere)
                {
                    if (line.InventoryItemId is int itemId)
                    {
                        var result = App.Inventory.Dispense(itemId, line.Quantity);
                        if (result.Success) { dispensed = true; report.AppendLine($"✓ {line.Medicine} ×{line.Quantity}"); }
                        else { fellBack = true; report.AppendLine($"→ {line.Medicine}: {result.Message} (sent to pharmacy)"); }
                    }
                    else
                    {
                        dispensed = true;   // free-text medicine handed over by the doctor
                        report.AppendLine($"✓ {line.Medicine} ×{line.Quantity} (issued)");
                    }
                }

                App.MedicalRecords.AddPrescription(new Prescription
                {
                    MedicalRecordId = medicalRecordId,
                    InventoryItemId = line.InventoryItemId,
                    Medicine = line.Medicine,
                    Quantity = line.Quantity,
                    TimesPerDay = line.TimesPerDay,
                    MealTiming = line.Timing,
                    Instructions = $"{line.TimesPerDay} time(s)/day, {line.Timing}",
                    Dispensed = dispensed
                });
            }

            return (fellBack, report.ToString());
        }
    }
}
