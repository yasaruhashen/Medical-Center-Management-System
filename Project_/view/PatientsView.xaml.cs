using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_.Views
{
    public partial class PatientsView : UserControl
    {
        public PatientsView()
        {
            InitializeComponent();
            Loaded += PatientsView_Loaded;
        }

        private void PatientsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPatients();
        }

        private void LoadPatients(string query = "")
        {
            try
            {
                var sql = "SELECT Id, FirstName || ' ' || LastName AS FullName, PatientType, RegistrationNumber, Faculty, Gender, " +
                          "CAST((julianday('now') - julianday(DateOfBirth))/365.25 AS INTEGER) AS Age, PhoneNumber " +
                          "FROM Patients WHERE IsArchived = 0";
                var p = new Dictionary<string, object?>();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    sql += " AND (FirstName LIKE @Q OR LastName LIKE @Q OR RegistrationNumber LIKE @Q OR PhoneNumber LIKE @Q)";
                    p.Add("Q", "%" + query + "%");
                }
                
                sql += " ORDER BY FirstName, LastName";

                var data = App.Database.ExecuteQuery(sql, p);
                Grid.ItemsSource = data.DefaultView;
            }
            catch {}
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) 
        { 
            EditorOverlay.Visibility = Visibility.Visible; 
            TxtEditorTitle.Text = "Add Patient";
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

                var sql = "INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, PatientType, RegistrationNumber, Faculty, YearOfStudy, CreatedAt) " +
                          "VALUES (@First, @Last, @DOB, @Gen, @Ph, @Em, @Addr, @Type, @Reg, @Fac, @Yr, @CreatedAt)";
                
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
                    {"Yr", string.IsNullOrWhiteSpace(FYear.Text) ? null : int.Parse(FYear.Text)},
                    {"CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
                };

                App.Database.ExecuteNonQuery(sql, p);
                EditorOverlay.Visibility = Visibility.Collapsed;
                LoadPatients();
            }
            catch (Exception ex)
            {
                TxtError.Text = ex.Message;
                TxtError.Visibility = Visibility.Visible;
            }
        }

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

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e) { }
        private void BtnImport_Click(object sender, RoutedEventArgs e) { }
        private void BtnEdit_Click(object sender, RoutedEventArgs e) { }
        private void BtnArchive_Click(object sender, RoutedEventArgs e) { }
    }
}
