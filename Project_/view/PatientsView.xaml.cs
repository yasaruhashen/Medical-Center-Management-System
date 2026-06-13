using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Project_.Views
{
    public partial class PatientsView : UserControl
    {
        public ObservableCollection<PatientDisplay> Patients { get; set; } = new ObservableCollection<PatientDisplay>();

        public PatientsView()
        {
            InitializeComponent();
            LoadPatients();
        }

        private void LoadPatients(string filter = "")
        {
            Patients.Clear();
            var sql = "SELECT * FROM Patients WHERE IsArchived = 0";
            if (!string.IsNullOrWhiteSpace(filter))
            {
                sql += $" AND (FirstName LIKE '%{filter}%' OR LastName LIKE '%{filter}%' OR RegistrationNumber LIKE '%{filter}%')";
            }

            try
            {
                var data = App.Database.ExecuteQuery(sql);
                foreach (DataRow row in data.Rows)
                {
                    Patients.Add(new PatientDisplay
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        FullName = $"{row["FirstName"]} {row["LastName"]}",
                        RegistrationNumber = row["RegistrationNumber"]?.ToString() ?? "—",
                        Email = string.IsNullOrWhiteSpace(row["Email"]?.ToString()) ? "No email provided" : row["Email"].ToString(),
                        Gender = row["Gender"]?.ToString() ?? "—",
                        PhoneNumber = row["PhoneNumber"]?.ToString() ?? "—",
                        PatientType = row["PatientType"]?.ToString() ?? "External"
                    });
                }
                PatientsList.ItemsSource = Patients;
            }
            catch
            {
                // Ignore DB errors on load
            }
        }

        private void BtnAddPatient_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Patient clicked. Provide patient details window here.", "WIP");
        }

        private void BtnEditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                MessageBox.Show($"Edit Patient ID: {id}", "WIP");
            }
        }

        private void BtnDeletePatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this patient?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    int id = Convert.ToInt32(btn.Tag);
                    App.Database.ExecuteNonQuery("UPDATE Patients SET IsArchived = 1 WHERE Id = @Id", new System.Collections.Generic.Dictionary<string, object?> { { "Id", id } });
                    LoadPatients(TxtSearch.Text == "Search patients..." ? "" : TxtSearch.Text);
                }
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtSearch.Text == "Search patients...")
            {
                TxtSearch.Text = "";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55));
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                TxtSearch.Text = "Search patients...";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtSearch.Text != "Search patients...")
            {
                LoadPatients(TxtSearch.Text.Trim());
            }
        }
    }

    public class PatientDisplay
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PatientType { get; set; }
    }
}
