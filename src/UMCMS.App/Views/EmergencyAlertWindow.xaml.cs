using System.Windows;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Emergency contact lookup: shows the patient's and their parent/guardian's mobile
    /// numbers to call, and records the emergency contact in the audit trail.
    /// </summary>
    public partial class EmergencyAlertWindow : Window
    {
        private readonly Patient _patient;

        public EmergencyAlertWindow(Patient patient)
        {
            InitializeComponent();
            _patient = patient;

            TxtPatient.Text = patient.FullName +
                (string.IsNullOrWhiteSpace(patient.RegistrationNumber) ? "" : $"  ·  Uni ID: {patient.RegistrationNumber}");

            TxtPatientPhone.Text = string.IsNullOrWhiteSpace(patient.PhoneNumber) ? "— not on file —" : patient.PhoneNumber;

            TxtContactLabel.Text = string.IsNullOrWhiteSpace(patient.EmergencyContactName)
                ? "Emergency contact (parent/guardian)"
                : $"Emergency contact — {patient.EmergencyContactName}";
            TxtContactPhone.Text = string.IsNullOrWhiteSpace(patient.EmergencyContactPhone)
                ? "— not on file —" : patient.EmergencyContactPhone;
        }

        private void Log_Click(object sender, RoutedEventArgs e)
        {
            var note = string.IsNullOrWhiteSpace(FNote.Text) ? "(no note)" : FNote.Text.Trim();
            App.Audit.Log("EmergencyContact", "Patient",
                $"{_patient.FullName}: patient {_patient.PhoneNumber}, guardian {_patient.EmergencyContactPhone} — {note}");
            MessageBox.Show("Emergency contact recorded in the audit log.", "Logged",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
