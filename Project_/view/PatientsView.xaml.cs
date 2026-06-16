using System;
using System.Collections.Generic;
<<<<<<< Updated upstream
=======
using System.Collections.ObjectModel;
>>>>>>> Stashed changes
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_.Views
{
    public partial class PatientsView : UserControl
    {
        public ObservableCollection<PatientDisplay> Patients { get; set; } = new();

        public PatientsView()
        {
            InitializeComponent();
            Loaded += PatientsView_Loaded;
<<<<<<< Updated upstream
=======
            PatientsList.ItemsSource = Patients;
>>>>>>> Stashed changes
        }

        private void PatientsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPatients();
        }

<<<<<<< Updated upstream
=======
        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtSearch.Text == "Search by name, student ID, or phone...")
            {
                TxtSearch.Text = "";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                TxtSearch.Text = "Search by name, student ID, or phone...";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txt = TxtSearch.Text;
            if (txt == "Search by name, student ID, or phone...") return;
            LoadPatients(txt);
        }

>>>>>>> Stashed changes
        private void LoadPatients(string query = "")
        {
            try
            {
<<<<<<< Updated upstream
                var sql = "SELECT Id, FirstName || ' ' || LastName AS FullName, PatientType, RegistrationNumber, Faculty, Gender, " +
                          "CAST((julianday('now') - julianday(DateOfBirth))/365.25 AS INTEGER) AS Age, PhoneNumber " +
                          "FROM Patients WHERE IsArchived = 0";
=======
                Patients.Clear();
                var sql = "SELECT * FROM Patients WHERE IsArchived = 0";
>>>>>>> Stashed changes
                var p = new Dictionary<string, object?>();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    sql += " AND (FirstName LIKE @Q OR LastName LIKE @Q OR RegistrationNumber LIKE @Q OR PhoneNumber LIKE @Q)";
                    p.Add("Q", "%" + query + "%");
                }
                
                sql += " ORDER BY FirstName, LastName";

                var data = App.Database.ExecuteQuery(sql, p);
<<<<<<< Updated upstream
                Grid.ItemsSource = data.DefaultView;
            }
            catch {}
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) 
        { 
            EditorOverlay.Visibility = Visibility.Visible; 
            TxtEditorTitle.Text = "Add Patient";
=======
                foreach (DataRow row in data.Rows)
                {
                    string bloodGroup = "";
                    if (row.Table.Columns.Contains("BloodGroup"))
                    {
                        bloodGroup = row["BloodGroup"]?.ToString() ?? "";
                    }
                    
                    string bloodColor = "#6b7280";
                    if (bloodGroup.StartsWith("A+")) bloodColor = "#8A0007";
                    else if (bloodGroup.StartsWith("A-")) bloodColor = "#4E0205";
                    else if (bloodGroup.StartsWith("B+")) bloodColor = "#b45309";
                    else if (bloodGroup.StartsWith("B-")) bloodColor = "#92400e";
                    else if (bloodGroup.StartsWith("O+")) bloodColor = "#15803d";
                    else if (bloodGroup.StartsWith("O-")) bloodColor = "#166534";
                    else if (bloodGroup.StartsWith("AB")) bloodColor = "#1d4ed8";

                    Patients.Add(new PatientDisplay
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        FullName = $"{row["FirstName"]} {row["LastName"]}",
                        StudentId = string.IsNullOrWhiteSpace(row["RegistrationNumber"]?.ToString()) ? "—" : row["RegistrationNumber"].ToString(),
                        Gender = row["Gender"]?.ToString() ?? "Unknown",
                        BloodGroup = string.IsNullOrWhiteSpace(bloodGroup) ? "—" : bloodGroup,
                        BloodGroupBg = bloodColor,
                        Phone = row["PhoneNumber"]?.ToString() ?? "—",
                        RegisteredDate = row["CreatedAt"]?.ToString()?.Split(' ')[0] ?? "—",
                        Initial = (row["FirstName"]?.ToString() ?? "P").Substring(0, 1).ToUpper()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patients: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int? _editingPatientId = null;

        private void BtnAdd_Click(object sender, RoutedEventArgs e) 
        { 
            _editingPatientId = null;
            EditorOverlay.Visibility = Visibility.Visible; 
            TxtEditorTitle.Text = "Register New Patient";
>>>>>>> Stashed changes
            FFirst.Text = FLast.Text = FPhone.Text = FEmail.Text = FAddress.Text = FReg.Text = FFaculty.Text = FYear.Text = "";
            FDob.SelectedDate = null; FGender.SelectedItem = null; FType.SelectedItem = null;
            TxtError.Visibility = Visibility.Collapsed;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { EditorOverlay.Visibility = Visibility.Collapsed; }
        
        private void BtnSave_Click(object sender, RoutedEventArgs e) 
        { 
            try
            {
                if (string.IsNullOrWhiteSpace(FFirst.Text) || string.IsNullOrWhiteSpace(FLast.Text))
                {
                    TxtError.Text = "First and last name are required.";
                    TxtError.Visibility = Visibility.Visible;
                    return;
                }

<<<<<<< Updated upstream
                var sql = "INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, PatientType, RegistrationNumber, Faculty, YearOfStudy, CreatedAt) " +
                          "VALUES (@First, @Last, @DOB, @Gen, @Ph, @Em, @Addr, @Type, @Reg, @Fac, @Yr, @CreatedAt)";
                
=======
>>>>>>> Stashed changes
                var p = new Dictionary<string, object?> {
                    {"First", FFirst.Text.Trim()},
                    {"Last", FLast.Text.Trim()},
                    {"DOB", FDob.SelectedDate.HasValue ? FDob.SelectedDate.Value.ToString("yyyy-MM-dd") : null},
                    {"Gen", FGender.SelectedItem != null ? ((ComboBoxItem)FGender.SelectedItem).Content.ToString() : null},
                    {"Ph", FPhone.Text},
                    {"Em", FEmail.Text},
                    {"Addr", FAddress.Text},
                    {"Type", FType.SelectedItem != null ? ((ComboBoxItem)FType.SelectedItem).Content.ToString() : "External"},
                    {"Reg", string.IsNullOrWhiteSpace(FReg.Text) ? null : FReg.Text},
                    {"Fac", FFaculty.Text},
<<<<<<< Updated upstream
                    {"Yr", string.IsNullOrWhiteSpace(FYear.Text) ? null : int.Parse(FYear.Text)},
                    {"CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
                };

                App.Database.ExecuteNonQuery(sql, p);
=======
                    {"Yr", string.IsNullOrWhiteSpace(FYear.Text) ? null : int.Parse(FYear.Text)}
                };

                if (_editingPatientId.HasValue)
                {
                    var sql = "UPDATE Patients SET FirstName=@First, LastName=@Last, DateOfBirth=@DOB, Gender=@Gen, PhoneNumber=@Ph, Email=@Em, Address=@Addr, PatientType=@Type, RegistrationNumber=@Reg, Faculty=@Fac, YearOfStudy=@Yr WHERE Id=@Id";
                    p.Add("Id", _editingPatientId.Value);
                    App.Database.ExecuteNonQuery(sql, p);
                }
                else
                {
                    var sql = "INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, PatientType, RegistrationNumber, Faculty, YearOfStudy, CreatedAt) " +
                              "VALUES (@First, @Last, @DOB, @Gen, @Ph, @Em, @Addr, @Type, @Reg, @Fac, @Yr, @CreatedAt)";
                    p.Add("CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    App.Database.ExecuteNonQuery(sql, p);
                }

>>>>>>> Stashed changes
                EditorOverlay.Visibility = Visibility.Collapsed;
                LoadPatients();
            }
            catch (Exception ex)
            {
                TxtError.Text = ex.Message;
                TxtError.Visibility = Visibility.Visible;
            }
        }

<<<<<<< Updated upstream
        private void TxtSearch_KeyDown(object sender, KeyEventArgs e) 
        { 
            if (e.Key == Key.Enter) LoadPatients(TxtSearch.Text); 
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) { LoadPatients(TxtSearch.Text); }
        private void BtnClear_Click(object sender, RoutedEventArgs e) { TxtSearch.Text = ""; LoadPatients(); }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) 
        { 
            bool hasSelection = Grid.SelectedItem != null;
            BtnEdit.IsEnabled = hasSelection;
            BtnArchive.IsEnabled = hasSelection;
        }

=======
>>>>>>> Stashed changes
        private void FType_SelectionChanged(object sender, SelectionChangedEventArgs e) 
        { 
            if (FType.SelectedItem is ComboBoxItem item && item.Content.ToString() == "Internal")
            {
                if (PanelFaculty != null) PanelFaculty.Visibility = Visibility.Visible;
                if (PanelYear != null) PanelYear.Visibility = Visibility.Visible;
            }
            else
            {
                if (PanelFaculty != null) PanelFaculty.Visibility = Visibility.Collapsed;
                if (PanelYear != null) PanelYear.Visibility = Visibility.Collapsed;
            }
        }

<<<<<<< Updated upstream
        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e) { }
        private void BtnImport_Click(object sender, RoutedEventArgs e) { }
        private void BtnEdit_Click(object sender, RoutedEventArgs e) { }
        private void BtnArchive_Click(object sender, RoutedEventArgs e) { }
=======
        private void BtnEditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    int id = Convert.ToInt32(btn.Tag);
                    var dt = App.Database.ExecuteQuery("SELECT * FROM Patients WHERE Id = @Id", new Dictionary<string, object?> { { "Id", id } });
                    
                    if (dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        _editingPatientId = id;
                        TxtEditorTitle.Text = "Edit Patient";
                        
                        FFirst.Text = row["FirstName"]?.ToString() ?? "";
                        FLast.Text = row["LastName"]?.ToString() ?? "";
                        FPhone.Text = row["PhoneNumber"]?.ToString() ?? "";
                        FEmail.Text = row["Email"]?.ToString() ?? "";
                        FAddress.Text = row["Address"]?.ToString() ?? "";
                        FReg.Text = row["RegistrationNumber"]?.ToString() ?? "";
                        FFaculty.Text = row["Faculty"]?.ToString() ?? "";
                        FYear.Text = row["YearOfStudy"]?.ToString() ?? "";
                        
                        if (DateTime.TryParse(row["DateOfBirth"]?.ToString(), out DateTime dob))
                            FDob.SelectedDate = dob;
                        else
                            FDob.SelectedDate = null;
                            
                        string gen = row["Gender"]?.ToString() ?? "";
                        foreach (ComboBoxItem item in FGender.Items)
                        {
                            if (item.Content.ToString() == gen) { FGender.SelectedItem = item; break; }
                        }
                        
                        string typ = row["PatientType"]?.ToString() ?? "";
                        foreach (ComboBoxItem item in FType.Items)
                        {
                            if (item.Content.ToString() == typ) { FType.SelectedItem = item; break; }
                        }

                        TxtError.Visibility = Visibility.Collapsed;
                        EditorOverlay.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading patient details: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeletePatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    int id = Convert.ToInt32(btn.Tag);
                    if (MessageBox.Show("Are you sure you want to delete this patient?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        App.Database.ExecuteNonQuery("UPDATE Patients SET IsArchived = 1 WHERE Id = @Id", new Dictionary<string, object?> { { "Id", id } });
                        LoadPatients();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewPatient_Click(object sender, MouseButtonEventArgs e)
        {
            // Future feature: detail view
        }
    }

    public class PatientDisplay
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Initial { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = string.Empty;
        public string BloodGroupBg { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RegisteredDate { get; set; } = string.Empty;
>>>>>>> Stashed changes
    }
}
