using System.Diagnostics;
using System.IO;
using System.Windows;
using UMCMS.Services;

namespace UMCMS.App.Helpers
{
    /// <summary>Generates a prescription-slip PDF to the user's Documents and opens it.</summary>
    public static class SlipPrinter
    {
        public static void Print(int medicalRecordId) => Generate("Prescriptions",
            $"prescription_{medicalRecordId}", path => App.Reports.GeneratePrescriptionSlipPdf(medicalRecordId, path, Session.CurrentUser?.Id));

        /// <summary>Generates and opens a full patient EHR PDF.</summary>
        public static void PrintEhr(int patientId) => Generate("EHR",
            $"ehr_{patientId}", path => App.Reports.GeneratePatientEhrPdf(patientId, path, Session.CurrentUser?.Id));

        /// <summary>Generates and opens dispensing labels for a consultation's medicines.</summary>
        public static void PrintLabels(int medicalRecordId) => Generate("Labels",
            $"labels_{medicalRecordId}", path => App.Reports.GenerateDispensingLabelsPdf(medicalRecordId, path, Session.CurrentUser?.Id));

        private static void Generate(string subfolder, string namePrefix, Action<string> build)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MediHelp", subfolder);
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"{namePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                build(path);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not generate the document:\n\n" + ex.Message,
                    "Print error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
