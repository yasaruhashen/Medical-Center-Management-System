using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Project_.Views
{
    public partial class PatientDetailsView : UserControl
    {
        public ObservableCollection<PatientListItem> AllPatients { get; set; } = new();
        public ObservableCollection<PatientListItem> FilteredPatients { get; set; } = new();
        
        public ObservableCollection<ApptHistoryItem> ApptHistory { get; set; } = new();
        public ObservableCollection<RxHistoryDetailItem> RxHistory { get; set; } = new();

        private PatientListItem? _selectedPatient;

        public PatientDetailsView()
        {
            InitializeComponent();
            PatientsList.ItemsSource = FilteredPatients;
            ApptHistoryList.ItemsSource = ApptHistory;
            RxHistoryList.ItemsSource = RxHistory;
            Loaded += PatientDetailsView_Loaded;
        }

        private void PatientDetailsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPatients();
        }

        private void LoadPatients()
        {
            try
            {
                var sql = "SELECT Id, FirstName || ' ' || LastName AS FullName, RegistrationNumber, PhoneNumber, " +
                          "Gender, DateOfBirth, BloodGroup, Allergies, Address, CreatedAt " +
                          "FROM Patients WHERE IsArchived = 0 ORDER BY FirstName, LastName";
                var data = App.Database.ExecuteQuery(sql);
                
                AllPatients.Clear();
                foreach (DataRow row in data.Rows)
                {
                    AllPatients.Add(new PatientListItem
                    {
                        Id = Convert.ToInt64(row["Id"]),
                        Name = row["FullName"].ToString(),
                        Initial = row["FullName"].ToString()?.Substring(0, 1).ToUpper() ?? "U",
                        RegNo = row["RegistrationNumber"]?.ToString(),
                        Phone = row["PhoneNumber"]?.ToString(),
                        Gender = row["Gender"]?.ToString(),
                        Dob = row["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(row["DateOfBirth"]).ToString("yyyy-MM-dd") : "-",
                        BloodGroup = row["BloodGroup"]?.ToString(),
                        Allergies = row["Allergies"]?.ToString(),
                        Address = row["Address"]?.ToString(),
                        CreatedAt = row["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(row["CreatedAt"]).ToString("yyyy-MM-dd") : "-"
                    });
                }
                
                FilterPatients();
            }
            catch { }
        }

        private void FilterPatients()
        {
            var q = TxtSearch.Text.ToLower();
            FilteredPatients.Clear();
            foreach (var p in AllPatients)
            {
                if (string.IsNullOrWhiteSpace(q) || 
                    p.Name?.ToLower().Contains(q) == true || 
                    p.RegNo?.ToLower().Contains(q) == true || 
                    p.Phone?.ToLower().Contains(q) == true)
                {
                    FilteredPatients.Add(p);
                }
            }
            
            TxtPatientsCount.Text = $"Patients ({FilteredPatients.Count})";
            TxtEmptyList.Visibility = FilteredPatients.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterPatients();
        }

        private void Patient_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is PatientListItem p)
            {
                SelectPatient(p);
            }
        }

        private void SelectPatient(PatientListItem p)
        {
            foreach (var item in FilteredPatients)
            {
                item.IsSelected = (item.Id == p.Id);
            }

            _selectedPatient = p;
            EmptySelectionPanel.Visibility = Visibility.Collapsed;
            DetailPanel.Visibility = Visibility.Visible;

            DetInitial.Text = p.Initial;
            DetName.Text = p.Name;
            DetSub.Text = $"{(string.IsNullOrWhiteSpace(p.RegNo) ? "No Student ID" : p.RegNo)} • {p.Gender}";
            DetDob.Text = p.Dob;
            DetPhone.Text = string.IsNullOrWhiteSpace(p.Phone) ? "-" : p.Phone;
            DetRegDate.Text = p.CreatedAt;
            DetAddress.Text = string.IsNullOrWhiteSpace(p.Address) ? "" : $"📍 {p.Address}";

            DetTags.Children.Clear();
            if (!string.IsNullOrWhiteSpace(p.BloodGroup))
            {
                var bgColors = new Dictionary<string, Color>
                {
                    { "A+", Color.FromRgb(0x8A, 0x00, 0x07) },
                    { "A-", Color.FromRgb(0x4E, 0x02, 0x05) },
                    { "B+", Color.FromRgb(0xB4, 0x53, 0x09) },
                    { "O+", Color.FromRgb(0x15, 0x80, 0x3D) },
                    { "O-", Color.FromRgb(0x16, 0x65, 0x34) },
                    { "AB+", Color.FromRgb(0x1D, 0x4E, 0xD8) },
                    { "AB-", Color.FromRgb(0x1E, 0x40, 0xAF) }
                };
                var color = bgColors.ContainsKey(p.BloodGroup) ? bgColors[p.BloodGroup] : Color.FromRgb(0x6B, 0x72, 0x80);
                
                var border = new Border { Background = new SolidColorBrush(color), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 8, 0) };
                border.Child = new TextBlock { Text = p.BloodGroup, Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.Bold };
                DetTags.Children.Add(border);
            }

            if (!string.IsNullOrWhiteSpace(p.Allergies) && p.Allergies.ToLower() != "none")
            {
                var border = new Border { Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 8, 0) };
                border.Child = new TextBlock { Text = $"⚠ {p.Allergies}", Foreground = new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)), FontSize = 12, FontWeight = FontWeights.SemiBold };
                DetTags.Children.Add(border);
            }

            LoadPatientHistory(p.Id);
        }

        private void LoadPatientHistory(long patId)
        {
            try
            {
                // Appointments
                var aSql = "SELECT a.AppointmentDate, a.Notes, a.Status, u.FirstName || ' ' || u.LastName AS DoctorName " +
                           "FROM Appointments a JOIN Users u ON a.DoctorId = u.Id " +
                           "WHERE a.PatientId = @Id ORDER BY a.AppointmentDate DESC";
                var aData = App.Database.ExecuteQuery(aSql, new Dictionary<string, object?> { { "Id", patId } });
                
                ApptHistory.Clear();
                foreach (DataRow row in aData.Rows)
                {
                    var status = row["Status"]?.ToString() ?? "Scheduled";
                    var dateStr = row["AppointmentDate"] != DBNull.Value ? Convert.ToDateTime(row["AppointmentDate"]).ToString("yyyy-MM-dd HH:mm") : "";
                    
                    var bg = status == "Completed" ? Color.FromRgb(0xF0, 0xFD, 0xF4) : (status == "Scheduled" ? Color.FromRgb(0xEF, 0xF6, 0xFF) : Color.FromRgb(0xFE, 0xF2, 0xF2));
                    var fg = status == "Completed" ? Color.FromRgb(0x16, 0xA3, 0x4A) : (status == "Scheduled" ? Color.FromRgb(0x1D, 0x4E, 0xD8) : Color.FromRgb(0x99, 0x1B, 0x1B));

                    var notes = row["Notes"]?.ToString();
                    
                    ApptHistory.Add(new ApptHistoryItem
                    {
                        Reason = string.IsNullOrWhiteSpace(notes) ? "General Consultation" : notes,
                        Details = $"{row["DoctorName"]} • {dateStr}",
                        Status = status,
                        StatusBg = new SolidColorBrush(bg),
                        StatusFg = new SolidColorBrush(fg)
                    });
                }
                
                TxtApptCount.Text = $"Appointment History ({ApptHistory.Count})";
                TxtEmptyAppts.Visibility = ApptHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                // Prescriptions
                var pSql = "SELECT m.Id, m.RecordDate, m.Diagnosis " +
                           "FROM MedicalRecords m WHERE m.PatientId = @Id ORDER BY m.RecordDate DESC";
                var pData = App.Database.ExecuteQuery(pSql, new Dictionary<string, object?> { { "Id", patId } });
                
                RxHistory.Clear();
                int rxCount = 0;

                foreach (DataRow row in pData.Rows)
                {
                    var recordId = Convert.ToInt64(row["Id"]);
                    var iSql = "SELECT Medicine, Dosage, Instructions, Quantity, IsProcessed FROM Prescriptions WHERE MedicalRecordId = @RecId";
                    var iData = App.Database.ExecuteQuery(iSql, new Dictionary<string, object?> { { "RecId", recordId } });
                    
                    if (iData.Rows.Count == 0) continue;
                    rxCount++;

                    bool isProcessed = true;
                    var items = new List<string>();
                    foreach (DataRow iRow in iData.Rows)
                    {
                        if (Convert.ToInt32(iRow["IsProcessed"]) == 0) isProcessed = false;
                        items.Add($"• {iRow["Medicine"]} {iRow["Dosage"]} × {iRow["Quantity"]} — {iRow["Instructions"]}");
                    }

                    var diag = row["Diagnosis"]?.ToString();
                    if (string.IsNullOrWhiteSpace(diag)) diag = "General";
                    var dateStr = row["RecordDate"] != DBNull.Value ? Convert.ToDateTime(row["RecordDate"]).ToString("yyyy-MM-dd") : "";

                    var status = isProcessed ? "Processed" : "Pending";
                    var bg = isProcessed ? Color.FromRgb(0xF0, 0xFD, 0xF4) : Color.FromRgb(0xFF, 0xFB, 0xEB);
                    var fg = isProcessed ? Color.FromRgb(0x16, 0xA3, 0x4A) : Color.FromRgb(0x92, 0x40, 0x0E);

                    RxHistory.Add(new RxHistoryDetailItem
                    {
                        Title = $"{diag} — {dateStr}",
                        Status = status,
                        StatusBg = new SolidColorBrush(bg),
                        StatusFg = new SolidColorBrush(fg),
                        Items = items
                    });
                }

                TxtRxCount.Text = $"Prescription History ({rxCount})";
                TxtEmptyRx.Visibility = rxCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }
    }

    public class PatientListItem : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public long Id { get; set; }
        public string? Initial { get; set; }
        public string? Name { get; set; }
        public string? RegNo { get; set; }
        public string? Phone { get; set; }
        public string Details => string.IsNullOrWhiteSpace(RegNo) ? Phone ?? "" : RegNo;

        // Extra details
        public string? Gender { get; set; }
        public string? Dob { get; set; }
        public string? BloodGroup { get; set; }
        public string? Allergies { get; set; }
        public string? Address { get; set; }
        public string? CreatedAt { get; set; }

        private bool _isSelected;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set 
            { 
                _isSelected = value; 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("BackgroundBrush")); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("NameWeight")); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("ArrowVisibility")); 
            } 
        }

        public SolidColorBrush BackgroundBrush => IsSelected ? new SolidColorBrush(Color.FromRgb(0xFF, 0xF0, 0xF0)) : Brushes.Transparent;
        public FontWeight NameWeight => IsSelected ? FontWeights.Bold : FontWeights.Medium;
        public Visibility ArrowVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    public class ApptHistoryItem
    {
        public string? Reason { get; set; }
        public string? Details { get; set; }
        public string? Status { get; set; }
        public SolidColorBrush? StatusBg { get; set; }
        public SolidColorBrush? StatusFg { get; set; }
    }

    public class RxHistoryDetailItem
    {
        public string? Title { get; set; }
        public string? Status { get; set; }
        public SolidColorBrush? StatusBg { get; set; }
        public SolidColorBrush? StatusFg { get; set; }
        public List<string> Items { get; set; } = new();
    }
}
