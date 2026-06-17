using System.Data;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UMCMS.App.Helpers;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Dentist dental records with an interactive 32-tooth chart, plus an optional
    /// prescription entry. Medicines are routed to the pharmacy (default) or dispensed
    /// on the spot, exactly like a doctor's consultation.
    /// </summary>
    public partial class DentalView : UserControl
    {
        private readonly User _user;

        private static readonly int[] UpperTeeth = { 18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28 };
        private static readonly int[] LowerTeeth = { 48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38 };
        private static readonly string[] Cycle = { "Healthy", "Cavity", "Filled", "Missing", "Crown" };
        private static readonly Dictionary<string, Color> StatusColor = new()
        {
            ["Healthy"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["Cavity"] = Color.FromRgb(0x8A, 0x00, 0x07),
            ["Filled"] = Color.FromRgb(0x2D, 0x6A, 0xA8),
            ["Missing"] = Color.FromRgb(0x99, 0x99, 0x99),
            ["Crown"] = Color.FromRgb(0xF7, 0xAA, 0x37),
        };

        private readonly Dictionary<int, string> _status = new();

        public DentalView(User user, int? preselectPatientId = null)
        {
            InitializeComponent();
            _user = user;
            var all = App.Patients.GetAll();
            FPatient.ItemsSource = all;
            RxEntry.Configure("General");   // dentists prescribe oral medicines (general stream)
            BuildLegend();
            if (preselectPatientId is int pid)
                FPatient.SelectedItem = all.FirstOrDefault(p => p.Id == pid);
        }

        private int? PatientId => (FPatient.SelectedItem as Patient)?.Id;

        private void HideId(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id") e.Cancel = true;
        }

        private void FPatient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnNew.IsEnabled = PatientId is not null;
            ReloadHistory();
        }

        private void ReloadHistory()
        {
            if (PatientId is int pid)
            {
                Grid.ItemsSource = App.DentalRecords.GetViewByPatient(pid).DefaultView;
                RxHistoryGrid.ItemsSource = App.MedicalRecords.GetPrescriptionsByPatient(pid).DefaultView;
            }
            else
            {
                Grid.ItemsSource = null;
                RxHistoryGrid.ItemsSource = null;
            }
        }

        // ── Tooth chart ──
        private void BuildLegend()
        {
            foreach (var s in Cycle)
            {
                var chip = new Border
                {
                    Background = new SolidColorBrush(StatusColor[s]),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3),
                    Width = 14, Height = 14, Margin = new Thickness(8, 0, 4, 0)
                };
                LegendPanel.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children = { chip, new TextBlock { Text = s, FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center } }
                });
            }
        }

        private void BuildChart()
        {
            UpperPanel.Children.Clear();
            LowerPanel.Children.Clear();
            _status.Clear();
            foreach (var t in UpperTeeth) UpperPanel.Children.Add(MakeTooth(t));
            foreach (var t in LowerTeeth) LowerPanel.Children.Add(MakeTooth(t));
        }

        private Button MakeTooth(int number)
        {
            _status[number] = "Healthy";
            var btn = new Button
            {
                Content = number.ToString(), Width = 34, Height = 38, Margin = new Thickness(2),
                FontSize = 11, Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(StatusColor["Healthy"]),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                Foreground = Brushes.Black, Tag = number, ToolTip = "Healthy"
            };
            btn.Click += Tooth_Click;
            return btn;
        }

        private void Tooth_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            int number = (int)btn.Tag;
            int next = (Array.IndexOf(Cycle, _status[number]) + 1) % Cycle.Length;
            string status = Cycle[next];
            _status[number] = status;
            btn.Background = new SolidColorBrush(StatusColor[status]);
            btn.Foreground = status == "Healthy" ? Brushes.Black : Brushes.White;
            btn.ToolTip = status;
        }

        // ── New record ──
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            BuildChart();
            DXray.Text = ""; DNotes.Text = ""; DGum.SelectedIndex = 0;
            RxEntry.Clear();
            TxtError.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Visible;
        }

        private void BtnSavePharmacy_Click(object sender, RoutedEventArgs e) => Save(dispenseHere: false);
        private void BtnSaveDispense_Click(object sender, RoutedEventArgs e) => Save(dispenseHere: true);

        private void Save(bool dispenseHere)
        {
            if (PatientId is not int pid) return;

            var charted = _status.Where(kv => kv.Value != "Healthy").ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            string notes = DNotes.Text.Trim();

            App.DentalRecords.Add(new DentalRecord
            {
                PatientId = pid,
                DoctorId = _user.Id,
                ToothChartJson = JsonSerializer.Serialize(charted),
                GumHealth = (DGum.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                XrayNotes = DXray.Text.Trim(),
                Notes = notes,
                RecordDate = DateTime.Now
            });
            App.Audit.Log("DentalRecord", "DentalRecord", $"{(FPatient.SelectedItem as Patient)?.FullName}: {charted.Count} charted teeth");

            var lines = RxEntry.Lines;
            int? slipRecordId = null;
            if (lines.Count > 0)
            {
                int recId = App.MedicalRecords.Add(new MedicalRecord
                {
                    PatientId = pid,
                    DoctorId = _user.Id,
                    Diagnosis = string.IsNullOrWhiteSpace(notes) ? "Dental treatment" : notes,
                    Treatment = notes,
                    RecordDate = DateTime.Now
                });
                PrescriptionSaver.Save(recId, lines, dispenseHere);
                App.Audit.Log(dispenseHere ? "DentalDispense" : "DentalPrescribe", "Prescription",
                    $"{lines.Count} medicine(s) for {(FPatient.SelectedItem as Patient)?.FullName}");
                slipRecordId = recId;
            }

            Overlay.Visibility = Visibility.Collapsed;
            ReloadHistory();

            string msg = lines.Count > 0
                ? $"Dental record saved. {lines.Count} medicine(s) {(dispenseHere ? "dispensed" : "sent to the pharmacy")}.\n\nPrint a prescription slip now?"
                : "Dental record saved.";
            if (slipRecordId is int sid &&
                MessageBox.Show(msg, "Saved", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                SlipPrinter.Print(sid);
            else if (slipRecordId is null)
                MessageBox.Show(msg, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Overlay.Visibility = Visibility.Collapsed;
    }
}
