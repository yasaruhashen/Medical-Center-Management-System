using System.Data;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UMCMS.App.Helpers;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Doctor consultation: patient demographics + previous records, fast prescription
    /// entry, and a choice at completion to send the medicines to the pharmacy (default)
    /// or dispense them on the spot.
    /// </summary>
    public partial class ConsultationsView : UserControl
    {
        private readonly User _user;
        private readonly string _stream;
        private readonly int? _appointmentId;

        public ConsultationsView(User user, int? preselectPatientId = null, int? appointmentId = null)
        {
            InitializeComponent();
            _user = user;
            _stream = user.IsDentist ? "Dental" : "General";
            _appointmentId = appointmentId;

            RxEntry.Configure(_stream);
            RxEntry.Finished += () => BtnSendPharmacy.Focus();

            var all = App.Patients.GetAll();
            FPatient.ItemsSource = all;
            if (preselectPatientId is int pid)
                FPatient.SelectedItem = all.FirstOrDefault(p => p.Id == pid);
        }

        private Patient? Current => FPatient.SelectedItem as Patient;
        private int? RecordId => RecordsGrid.SelectedItem is DataRowView r ? Convert.ToInt32(r["Id"]) : null;

        private void HideId(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id") e.Cancel = true;
        }

        private void FPatient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderHeader();
            ConsultPanel.IsEnabled = Current is not null;
            BtnExportEhr.IsEnabled = Current is not null;
            LoadRecords();
        }

        private void BtnExportEhr_Click(object sender, RoutedEventArgs e)
        {
            if (Current is Patient p) SlipPrinter.PrintEhr(p.Id);
        }

        private void RenderHeader()
        {
            DemoPanel.Children.Clear();
            if (Current is not Patient p) { TxtPatientName.Text = "Select a patient"; return; }
            TxtPatientName.Text = p.FullName;

            void Chip(string label, string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var b = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xFB, 0xE9, 0xE9)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(0, 0, 8, 0)
                };
                b.Child = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0x02, 0x05)),
                    Text = $"{label}: {value}" };
                DemoPanel.Children.Add(b);
            }

            Chip("Gender", p.Gender);
            Chip("Age", p.Age?.ToString() ?? "");
            Chip("Type", p.PatientType);
            Chip("Reg. No", p.RegistrationNumber ?? "");
            Chip("Faculty", p.Faculty ?? "");
            Chip("Year", p.YearOfStudy?.ToString() ?? "");
            Chip("Phone", p.PhoneNumber);
        }

        private void LoadRecords()
        {
            RecordsGrid.ItemsSource = Current is Patient p ? App.MedicalRecords.GetViewByPatient(p.Id).DefaultView : null;
            RxHistoryGrid.ItemsSource = null;
            DetailPanel.Children.Clear();
            TxtDetailHeader.Text = "Select a record above to view its full details.";
            TxtDetailHeader.Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));
            TxtRxLabel.Visibility = Visibility.Collapsed;
        }

        private void RecordsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DetailPanel.Children.Clear();
            if (RecordId is not int rid)
            {
                TxtDetailHeader.Text = "Select a record above to view its full details.";
                TxtDetailHeader.Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));
                TxtRxLabel.Visibility = Visibility.Collapsed;
                RxHistoryGrid.ItemsSource = null;
                return;
            }

            var dt = App.MedicalRecords.GetRecordDetail(rid);
            if (dt.Rows.Count > 0)
            {
                var r = dt.Rows[0];
                TxtDetailHeader.Text = $"{r["RecordDate"]}  ·  Dr. {r["Doctor"]}";
                TxtDetailHeader.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0x02, 0x05));

                string vitals = string.Join("   ", new[]
                {
                    Field("BP", r["BloodPressure"]), Field("Pulse", r["Pulse"]),
                    Field("Temp", r["Temperature"]), Field("Weight", r["Weight"])
                }.Where(s => s is not null));

                AddDetail("Symptoms", r["Symptoms"]);
                AddDetail("Diagnosis", r["Diagnosis"]);
                AddDetail("Treatment", r["Treatment"]);
                if (vitals.Length > 0) AddDetailText("Vitals", vitals);
                AddDetail("Notes", r["Notes"]);
            }

            RxHistoryGrid.ItemsSource = App.MedicalRecords.GetPrescriptions(rid).DefaultView;
            TxtRxLabel.Visibility = Visibility.Visible;
        }

        private static string? Field(string label, object? value) =>
            value is null or DBNull || string.IsNullOrWhiteSpace(value.ToString()) ? null : $"{label}: {value}";

        private void AddDetail(string label, object? value)
        {
            if (value is null or DBNull || string.IsNullOrWhiteSpace(value.ToString())) return;
            AddDetailText(label, value.ToString()!);
        }

        private void AddDetailText(string label, string value)
        {
            DetailPanel.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x00, 0x07)), Margin = new Thickness(0, 6, 0, 0) });
            DetailPanel.Children.Add(new TextBlock { Text = value, FontSize = 13, TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) });
        }

        // ── Complete ──
        private void BtnSendPharmacy_Click(object sender, RoutedEventArgs e) => Complete(dispenseHere: false);
        private void BtnDispenseHere_Click(object sender, RoutedEventArgs e) => Complete(dispenseHere: true);

        private void Complete(bool dispenseHere)
        {
            if (Current is not Patient p) return;
            if (string.IsNullOrWhiteSpace(RDiagnosis.Text)) { Error("Diagnosis is required."); return; }

            var lines = RxEntry.Lines;

            int recId = App.MedicalRecords.Add(new MedicalRecord
            {
                PatientId = p.Id,
                DoctorId = _user.Id,
                AppointmentId = _appointmentId,
                Symptoms = RSymptoms.Text.Trim(),
                Diagnosis = RDiagnosis.Text.Trim(),
                Treatment = RTreatment.Text.Trim(),
                BloodPressure = NullIfEmpty(RBp.Text),
                Pulse = int.TryParse(RPulse.Text, out var pu) ? pu : null,
                Temperature = double.TryParse(RTemp.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var te) ? te : null,
                Weight = double.TryParse(RWeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var we) ? we : null,
                RecordDate = DateTime.Now
            });

            var (sentToPharmacy, summary) = PrescriptionSaver.Save(recId, lines, dispenseHere);

            if (_appointmentId is int aid) App.Appointments.SetStatus(aid, "Completed");
            App.Audit.Log("Consultation", "MedicalRecord", $"{p.FullName}: {RDiagnosis.Text.Trim()} ({lines.Count} medicine line(s), {(dispenseHere ? "dispensed here" : "sent to pharmacy")})");

            ResetForm();
            LoadRecords();

            string head = dispenseHere
                ? $"Consultation completed for {p.FullName}.\n\n{summary}"
                : (lines.Count > 0
                    ? $"Consultation completed. {lines.Count} medicine(s) sent to the pharmacy for {p.FullName}."
                    : "Consultation completed.");
            if (sentToPharmacy && dispenseHere) head += "\nSome items were short on stock and were sent to the pharmacy instead.";

            if (MessageBox.Show(head + "\n\nPrint a prescription slip now?", "Done",
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                SlipPrinter.Print(recId);
        }

        private void ResetForm()
        {
            foreach (var tb in new[] { RSymptoms, RDiagnosis, RTreatment, RBp, RPulse, RTemp, RWeight }) tb.Text = "";
            RxEntry.Clear();
            TxtError.Visibility = Visibility.Collapsed;
        }

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        private void Error(string m) { TxtError.Text = m; TxtError.Visibility = Visibility.Visible; }
    }
}
