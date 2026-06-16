using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project_.Views
{
    public partial class PrescriptionProcessingView : UserControl
    {
        public ObservableCollection<PrescriptionDisplay> Prescriptions { get; set; } = new();

        public PrescriptionProcessingView()
        {
            InitializeComponent();
            Loaded += PrescriptionProcessingView_Loaded;
            PrescriptionList.ItemsSource = Prescriptions;
        }

        private void PrescriptionProcessingView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPrescriptions();
        }

        private void LoadPrescriptions()
        {
            try
            {
                Prescriptions.Clear();
                
                var sql = @"
                    SELECT pr.Id AS PrescriptionId, pr.MedicalRecordId, pr.Medicine, pr.Dosage, pr.Instructions, pr.Quantity, pr.Status, pr.InventoryItemId,
                           mr.Diagnosis, mr.Notes, DATE(mr.RecordDate) AS RecordDate,
                           p.FirstName || ' ' || p.LastName AS PatientName,
                           d.FirstName || ' ' || d.LastName AS DoctorName,
                           i.Quantity AS StockLevel, i.Unit
                    FROM Prescriptions pr
                    JOIN MedicalRecords mr ON pr.MedicalRecordId = mr.Id
                    JOIN Patients p ON mr.PatientId = p.Id
                    JOIN Users d ON mr.DoctorId = d.Id
                    LEFT JOIN Inventory i ON pr.InventoryItemId = i.Id
                    WHERE 1=1";
                
                var p = new Dictionary<string, object?>();

                var query = TxtSearch.Text == "Search by patient or doctor..." ? "" : TxtSearch.Text;
                if (!string.IsNullOrWhiteSpace(query))
                {
                    sql += " AND (p.FirstName LIKE @Q OR p.LastName LIKE @Q OR d.FirstName LIKE @Q OR d.LastName LIKE @Q)";
                    p.Add("Q", "%" + query + "%");
                }

                sql += " ORDER BY mr.RecordDate DESC, pr.MedicalRecordId";

                var data = App.Database.ExecuteQuery(sql, p);

                // Group by MedicalRecordId
                var dict = new Dictionary<int, PrescriptionDisplay>();

                foreach (DataRow row in data.Rows)
                {
                    int mrId = Convert.ToInt32(row["MedicalRecordId"]);
                    if (!dict.ContainsKey(mrId))
                    {
                        dict[mrId] = new PrescriptionDisplay
                        {
                            Id = mrId,
                            PatientName = row["PatientName"]?.ToString() ?? "",
                            DoctorName = row["DoctorName"]?.ToString() ?? "",
                            Date = row["RecordDate"]?.ToString() ?? "",
                            Diagnosis = row["Diagnosis"]?.ToString() ?? "—",
                            Notes = row["Notes"]?.ToString() ?? "—",
                            Status = "Processed", // Will be set to Pending if any item is pending
                            StatusBg = "#f0fdf4",
                            StatusColor = "#16a34a",
                            BorderColor = new SolidColorBrush(Color.FromRgb(209, 250, 229)),
                            HeaderBg = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
                            ShowProcessButton = Visibility.Collapsed
                        };
                    }

                    var display = dict[mrId];
                    string status = row["Status"]?.ToString() ?? "Pending";
                    if (status == "Pending")
                    {
                        display.Status = "Pending";
                        display.StatusBg = "#fffbeb";
                        display.StatusColor = "#d97706";
                        display.BorderColor = new SolidColorBrush(Color.FromRgb(252, 211, 77));
                        display.HeaderBg = new SolidColorBrush(Color.FromRgb(255, 251, 235));
                        display.ShowProcessButton = Visibility.Visible;
                    }

                    int stock = Convert.ToInt32(row["StockLevel"] ?? 0);
                    int qty = Convert.ToInt32(row["Quantity"] ?? 1);

                    display.Items.Add(new PrescriptionItemDisplay
                    {
                        MedicationName = row["Medicine"]?.ToString() ?? "",
                        Dosage = row["Dosage"]?.ToString() ?? "",
                        Instructions = row["Instructions"]?.ToString() ?? "",
                        Quantity = qty,
                        Stock = stock,
                        Unit = row["Unit"]?.ToString() ?? "",
                        StockColor = stock >= qty ? "#6b7280" : "#ef4444" // Red if not enough stock
                    });
                }

                foreach (var item in dict.Values)
                {
                    Prescriptions.Add(item);
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading prescriptions: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary()
        {
            TxtTotal.Text = Prescriptions.Count.ToString();
            int pending = Prescriptions.Count(rx => rx.Status == "Pending");
            int processed = Prescriptions.Count(rx => rx.Status == "Processed");
            TxtPending.Text = pending.ToString();
            TxtProcessed.Text = processed.ToString();
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtSearch.Text == "Search by patient or doctor...")
            {
                TxtSearch.Text = "";
                TxtSearch.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                TxtSearch.Text = "Search by patient or doctor...";
                TxtSearch.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) LoadPrescriptions(); }
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) { /* Client-side filtering could be done here */ }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int mrId = Convert.ToInt32(btn.Tag);
                
                try
                {
                    App.Database.ExecuteInTransaction((conn, tr) =>
                    {
                        // Deduct inventory
                        var itemsSql = "SELECT Id, InventoryItemId, Quantity FROM Prescriptions WHERE MedicalRecordId = @MrId AND Status = 'Pending'";
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.CommandText = itemsSql;
                            cmd.Parameters.AddWithValue("@MrId", mrId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if (reader["InventoryItemId"] != DBNull.Value)
                                    {
                                        int invId = Convert.ToInt32(reader["InventoryItemId"]);
                                        int qty = Convert.ToInt32(reader["Quantity"]);
                                        
                                        var upCmd = conn.CreateCommand();
                                        upCmd.Transaction = tr;
                                        upCmd.CommandText = "UPDATE Inventory SET Quantity = Quantity - @Qty WHERE Id = @InvId";
                                        upCmd.Parameters.AddWithValue("@Qty", qty);
                                        upCmd.Parameters.AddWithValue("@InvId", invId);
                                        upCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // Update status
                        var statusCmd = conn.CreateCommand();
                        statusCmd.Transaction = tr;
                        statusCmd.CommandText = "UPDATE Prescriptions SET Status = 'Processed' WHERE MedicalRecordId = @MrId AND Status = 'Pending'";
                        statusCmd.Parameters.AddWithValue("@MrId", mrId);
                        statusCmd.ExecuteNonQuery();
                    });

                    TxtMsg.Text = "Prescription processed successfully. Inventory updated.";
                    TxtMsg.Visibility = Visibility.Visible;
                    
                    LoadPrescriptions();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing prescription: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class PrescriptionDisplay
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBg { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public SolidColorBrush BorderColor { get; set; } = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush HeaderBg { get; set; } = new SolidColorBrush(Colors.Transparent);
        public Visibility ShowProcessButton { get; set; } = Visibility.Collapsed;

        public ObservableCollection<PrescriptionItemDisplay> Items { get; set; } = new();
    }

    public class PrescriptionItemDisplay
    {
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string StockColor { get; set; } = string.Empty;
    }
}
