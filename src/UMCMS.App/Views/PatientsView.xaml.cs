using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using UMCMS.App.Helpers;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Patient register/search. Receptionists get full add/edit/archive;
    /// doctors get a read-only view (no editing controls).
    /// </summary>
    public partial class PatientsView : UserControl
    {
        private readonly User _user;
        private readonly bool _canEdit;
        private Patient? _editing;

        public PatientsView(User user)
        {
            InitializeComponent();
            _user = user;
            _canEdit = user.Role == "Receptionist";   // doctors are read-only here

            if (!_canEdit)
            {
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnImport.Visibility = Visibility.Collapsed;
                BtnEdit.Visibility = Visibility.Collapsed;
                BtnArchive.Visibility = Visibility.Collapsed;
            }
            LoadPatients();
        }

        private void LoadPatients(string? search = null)
        {
            var list = string.IsNullOrWhiteSpace(search) ? App.Patients.GetAll() : App.Patients.Search(search);
            Grid.ItemsSource = list;
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool sel = Grid.SelectedItem is Patient;
            if (_canEdit)
            {
                BtnEdit.IsEnabled = sel;
                BtnArchive.IsEnabled = sel;
            }
            BtnEmergency.IsEnabled = sel;   // available to every role that can see Patients
        }

        private void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not Patient p) return;
            new EmergencyAlertWindow(p) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateActionButtons();

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_canEdit && Grid.SelectedItem is Patient) BtnEdit_Click(sender, e);
        }

        // ── Search ──
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadPatients(TxtSearch.Text);
        private void BtnClear_Click(object sender, RoutedEventArgs e) { TxtSearch.Text = ""; LoadPatients(); }
        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            TxtSearchHint.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (e.Key == Key.Enter) LoadPatients(TxtSearch.Text);
        }

        // ── Add / Edit ──
        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "CSV file|*.csv", Title = "Import patients" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var r = PatientCsvImporter.Import(dlg.FileName);
                App.Audit.Log("ImportPatients", "Patient", $"{r.Imported} imported, {r.Skipped} skipped");
                LoadPatients();
                string msg = $"Imported {r.Imported} patient(s); skipped {r.Skipped}.";
                if (r.Errors.Count > 0)
                    msg += "\n\n" + string.Join("\n", r.Errors.Take(12)) + (r.Errors.Count > 12 ? "\n…" : "");
                MessageBox.Show(msg, "Import complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not import the file:\n\n" + ex.Message, "Import error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            TxtEditorTitle.Text = "Add Patient";
            ClearForm();
            FType.SelectedIndex = 0;
            ShowEditor();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not Patient p) return;
            _editing = p;
            TxtEditorTitle.Text = "Edit Patient";
            FFirst.Text = p.FirstName;
            FLast.Text = p.LastName;
            FDob.SelectedDate = p.DateOfBirth;
            SelectCombo(FGender, p.Gender);
            FPhone.Text = p.PhoneNumber;
            FEmail.Text = p.Email;
            FAddress.Text = p.Address;
            SelectCombo(FType, p.PatientType);
            FReg.Text = p.RegistrationNumber ?? "";
            FFaculty.Text = p.Faculty ?? "";
            FYear.Text = p.YearOfStudy?.ToString() ?? "";
            FEcName.Text = p.EmergencyContactName ?? "";
            FEcPhone.Text = p.EmergencyContactPhone ?? "";
            ShowEditor();
        }

        private void BtnArchive_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not Patient p) return;
            if (MessageBox.Show($"Archive {p.FullName}? They will be hidden from the list.",
                "Confirm Archive", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                App.Patients.Archive(p.Id);
                App.Audit.Log("ArchivePatient", "Patient", p.FullName);
                LoadPatients();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FFirst.Text) || string.IsNullOrWhiteSpace(FLast.Text))
            {
                ShowError("First and last name are required.");
                return;
            }

            int? year = int.TryParse(FYear.Text, out var y) ? y : null;
            var p = _editing ?? new Patient();
            p.FirstName = FFirst.Text.Trim();
            p.LastName = FLast.Text.Trim();
            p.DateOfBirth = FDob.SelectedDate;
            p.Gender = (FGender.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            p.PhoneNumber = FPhone.Text.Trim();
            p.Email = FEmail.Text.Trim();
            p.Address = FAddress.Text.Trim();
            p.PatientType = (FType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "External";
            p.RegistrationNumber = string.IsNullOrWhiteSpace(FReg.Text) ? null : FReg.Text.Trim();
            p.Faculty = string.IsNullOrWhiteSpace(FFaculty.Text) ? null : FFaculty.Text.Trim();
            p.YearOfStudy = year;
            p.EmergencyContactName = string.IsNullOrWhiteSpace(FEcName.Text) ? null : FEcName.Text.Trim();
            p.EmergencyContactPhone = string.IsNullOrWhiteSpace(FEcPhone.Text) ? null : FEcPhone.Text.Trim();

            try
            {
                if (_editing is null) App.Patients.Add(p);
                else App.Patients.Update(p);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message.Contains("UNIQUE") ? "That registration number is already in use." : ex.Message);
                return;
            }
            App.Audit.Log(_editing is null ? "RegisterPatient" : "EditPatient", "Patient", p.FullName);

            HideEditor();
            LoadPatients();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => HideEditor();

        private void FType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool internalType = (FType.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Internal";
            if (PanelFaculty != null) PanelFaculty.IsEnabled = internalType;
            if (PanelYear != null) PanelYear.IsEnabled = internalType;
        }

        // ── helpers ──
        private void ShowEditor() { TxtError.Visibility = Visibility.Collapsed; EditorOverlay.Visibility = Visibility.Visible; FFirst.Focus(); }
        private void HideEditor() => EditorOverlay.Visibility = Visibility.Collapsed;
        private void ShowError(string msg) { TxtError.Text = msg; TxtError.Visibility = Visibility.Visible; }

        private void ClearForm()
        {
            foreach (var tb in new[] { FFirst, FLast, FPhone, FEmail, FAddress, FReg, FFaculty, FYear, FEcName, FEcPhone }) tb.Text = "";
            FDob.SelectedDate = null;
            FGender.SelectedIndex = -1;
        }

        private static void SelectCombo(ComboBox cb, string? value)
        {
            foreach (ComboBoxItem item in cb.Items)
                if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                { cb.SelectedItem = item; return; }
            cb.SelectedIndex = -1;
        }
    }
}
