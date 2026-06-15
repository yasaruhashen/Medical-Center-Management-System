using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Project_.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            
            // Set default dates
            ApptStart.SelectedDate = DateTime.Now.AddMonths(-1);
            ApptEnd.SelectedDate = DateTime.Now;
            RxStart.SelectedDate = DateTime.Now.AddMonths(-1);
            RxEnd.SelectedDate = DateTime.Now;
        }

        private void BtnExportAppt_Click(object sender, RoutedEventArgs e)
        {
            if (!ApptStart.SelectedDate.HasValue || !ApptEnd.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a valid date range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var sql = "SELECT a.AppointmentDate, p.FirstName || ' ' || p.LastName AS PatientName, p.RegistrationNumber, " +
                          "u.FirstName || ' ' || u.LastName AS DoctorName, a.Status, a.Notes " +
                          "FROM Appointments a " +
                          "JOIN Patients p ON a.PatientId = p.Id " +
                          "JOIN Users u ON a.DoctorId = u.Id " +
                          "WHERE a.AppointmentDate >= @Start AND a.AppointmentDate <= @End " +
                          "ORDER BY a.AppointmentDate DESC";
                          
                var parameters = new Dictionary<string, object?> {
                    { "Start", ApptStart.SelectedDate.Value.ToString("yyyy-MM-dd 00:00:00") },
                    { "End", ApptEnd.SelectedDate.Value.ToString("yyyy-MM-dd 23:59:59") }
                };

                var data = App.Database.ExecuteQuery(sql, parameters);
                ExportToCsv(data, "Appointment_Report");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportRx_Click(object sender, RoutedEventArgs e)
        {
            if (!RxStart.SelectedDate.HasValue || !RxEnd.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a valid date range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var sql = "SELECT m.RecordDate, p.FirstName || ' ' || p.LastName AS PatientName, m.Diagnosis, " +
                          "u.FirstName || ' ' || u.LastName AS PrescribedBy, " +
                          "rx.Medicine, rx.Dosage, rx.Quantity, rx.Instructions, rx.IsProcessed " +
                          "FROM Prescriptions rx " +
                          "JOIN MedicalRecords m ON rx.MedicalRecordId = m.Id " +
                          "JOIN Patients p ON m.PatientId = p.Id " +
                          "JOIN Users u ON m.DoctorId = u.Id " +
                          "WHERE m.RecordDate >= @Start AND m.RecordDate <= @End " +
                          "ORDER BY m.RecordDate DESC";

                var parameters = new Dictionary<string, object?> {
                    { "Start", RxStart.SelectedDate.Value.ToString("yyyy-MM-dd") },
                    { "End", RxEnd.SelectedDate.Value.ToString("yyyy-MM-dd") }
                };

                var data = App.Database.ExecuteQuery(sql, parameters);
                ExportToCsv(data, "Prescription_Report");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportPatients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sql = "SELECT FirstName, LastName, RegistrationNumber, PatientType, DateOfBirth, Gender, " +
                          "BloodGroup, Allergies, PhoneNumber, Email, Address, Faculty, YearOfStudy, CreatedAt " +
                          "FROM Patients WHERE IsArchived = 0 ORDER BY FirstName, LastName";

                var data = App.Database.ExecuteQuery(sql);
                ExportToCsv(data, "Patient_List");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(DataTable data, string defaultFileName)
        {
            if (data.Rows.Count == 0)
            {
                MessageBox.Show("No data found for the selected criteria.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Save Report"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    
                    // Headers
                    var headers = new List<string>();
                    foreach (DataColumn column in data.Columns)
                    {
                        headers.Add($"\"{column.ColumnName.Replace("\"", "\"\"")}\"");
                    }
                    sb.AppendLine(string.Join(",", headers));

                    // Rows
                    foreach (DataRow row in data.Rows)
                    {
                        var fields = new List<string>();
                        foreach (var item in row.ItemArray)
                        {
                            var value = item?.ToString() ?? "";
                            // Handle booleans (like IsProcessed) appropriately if needed
                            if (value == "1" && data.Columns[row.ItemArray.ToList().IndexOf(item)].ColumnName == "IsProcessed") value = "Yes";
                            else if (value == "0" && data.Columns[row.ItemArray.ToList().IndexOf(item)].ColumnName == "IsProcessed") value = "No";

                            fields.Add($"\"{value.Replace("\"", "\"\"")}\"");
                        }
                        sb.AppendLine(string.Join(",", fields));
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Report exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
