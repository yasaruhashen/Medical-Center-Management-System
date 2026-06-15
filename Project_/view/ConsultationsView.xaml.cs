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
    public partial class ConsultationsView : UserControl
    {
        private readonly long? _doctorId;
        
        public ObservableCollection<MedicationInput> Medications { get; set; } = new();
        public ObservableCollection<MedicineOption> AvailableMedicines { get; set; } = new();
        public ObservableCollection<HistoryItem> RxHistory { get; set; } = new();
        public ObservableCollection<HistoryItem> AllRxHistory { get; set; } = new();

        public ConsultationsView(long? doctorId = null)
        {
            _doctorId = doctorId;
            InitializeComponent();
            MedsList.ItemsSource = Medications;
            HistoryList.ItemsSource = RxHistory;
            Loaded += ConsultationsView_Loaded;
        }

        private void ConsultationsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPatients();
            LoadInventory();
            LoadHistory();
            UpdateMedicationsUI();
        }

        private void LoadPatients()
        {
            try
            {
                var sql = "SELECT Id, FirstName || ' ' || LastName AS FullName, RegistrationNumber, Allergies FROM Patients WHERE IsArchived = 0 ORDER BY FirstName, LastName";
                var data = App.Database.ExecuteQuery(sql);
                var list = new List<PatientOption>();
                foreach (DataRow row in data.Rows)
                {
                    list.Add(new PatientOption
                    {
                        Id = Convert.ToInt64(row["Id"]),
                        FullName = row["FullName"].ToString(),
                        RegistrationNumber = row["RegistrationNumber"]?.ToString(),
                        Allergies = row["Allergies"]?.ToString()
                    });
                }
                CmbPatient.ItemsSource = list;
            }
            catch { }
        }

        private void LoadInventory()
        {
            try
            {
                var sql = "SELECT Id, ItemName, Quantity, Unit FROM Inventory WHERE IsArchived = 0 ORDER BY ItemName";
                var data = App.Database.ExecuteQuery(sql);
                AvailableMedicines.Clear();
                foreach (DataRow row in data.Rows)
                {
                    AvailableMedicines.Add(new MedicineOption
                    {
                        Id = Convert.ToInt64(row["Id"]),
                        Name = row["ItemName"].ToString(),
                        Stock = $"{row["Quantity"]} {row["Unit"]}"
                    });
                }
            }
            catch { }
        }

        private void LoadHistory()
        {
            if (!_doctorId.HasValue) return;

            try
            {
                var sql = "SELECT m.Id, p.FirstName || ' ' || p.LastName AS PatientName, m.RecordDate, m.Diagnosis, m.Symptoms, m.Treatment " +
                          "FROM MedicalRecords m JOIN Patients p ON m.PatientId = p.Id " +
                          "WHERE m.DoctorId = @DocId ORDER BY m.RecordDate DESC";
                
                var records = App.Database.ExecuteQuery(sql, new Dictionary<string, object?> { { "DocId", _doctorId.Value } });
                
                AllRxHistory.Clear();
                foreach (DataRow row in records.Rows)
                {
                    var recordId = Convert.ToInt64(row["Id"]);
                    var pSql = "SELECT Medicine, Dosage, Instructions, Quantity, IsProcessed FROM Prescriptions WHERE MedicalRecordId = @RecId";
                    var prescs = App.Database.ExecuteQuery(pSql, new Dictionary<string, object?> { { "RecId", recordId } });
                    
                    if (prescs.Rows.Count == 0) continue;

                    bool isProcessed = true;
                    var items = new List<HistoryMedItem>();
                    foreach (DataRow pRow in prescs.Rows)
                    {
                        if (Convert.ToInt32(pRow["IsProcessed"]) == 0) isProcessed = false;
                        items.Add(new HistoryMedItem
                        {
                            MedAndDosage = $"{pRow["Medicine"]} {pRow["Dosage"]}",
                            QtyAndInstr = $"×{pRow["Quantity"]} — {pRow["Instructions"]}"
                        });
                    }

                    var diag = row["Diagnosis"]?.ToString();
                    if (string.IsNullOrWhiteSpace(diag)) diag = "General";
                    var notes = row["Treatment"]?.ToString();
                    
                    AllRxHistory.Add(new HistoryItem
                    {
                        PatientName = row["PatientName"]?.ToString(),
                        DiagnosisAndDate = $"{diag} • {Convert.ToDateTime(row["RecordDate"]):yyyy-MM-dd}",
                        IsProcessed = isProcessed,
                        Status = isProcessed ? "Processed" : "Pending",
                        Items = items,
                        Notes = !string.IsNullOrWhiteSpace(notes) ? $"📝 {notes}" : "",
                        HasNotes = !string.IsNullOrWhiteSpace(notes) ? Visibility.Visible : Visibility.Collapsed,
                        RawDiag = diag
                    });
                }
                
                FilterHistory();
            }
            catch { }
        }

        private void FilterHistory()
        {
            var query = TxtSearchHistory.Text.ToLower();
            RxHistory.Clear();
            foreach (var item in AllRxHistory)
            {
                if (string.IsNullOrWhiteSpace(query) || 
                    (item.PatientName?.ToLower().Contains(query) == true) || 
                    (item.RawDiag?.ToLower().Contains(query) == true))
                {
                    RxHistory.Add(item);
                }
            }
            
            TxtEmptyHistory.Visibility = RxHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnTabWrite_Click(object sender, RoutedEventArgs e)
        {
            BtnTabWrite.Background = new SolidColorBrush(Color.FromRgb(0x8A, 0x00, 0x07));
            BtnTabWrite.Foreground = Brushes.White;
            BtnTabWrite.FontWeight = FontWeights.SemiBold;
            
            BtnTabHistory.Background = Brushes.White;
            BtnTabHistory.Foreground = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));
            BtnTabHistory.FontWeight = FontWeights.Normal;

            WritePanel.Visibility = Visibility.Visible;
            HistoryPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnTabHistory_Click(object sender, RoutedEventArgs e)
        {
            BtnTabHistory.Background = new SolidColorBrush(Color.FromRgb(0x8A, 0x00, 0x07));
            BtnTabHistory.Foreground = Brushes.White;
            BtnTabHistory.FontWeight = FontWeights.SemiBold;
            
            BtnTabWrite.Background = Brushes.White;
            BtnTabWrite.Foreground = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));
            BtnTabWrite.FontWeight = FontWeights.Normal;

            WritePanel.Visibility = Visibility.Collapsed;
            HistoryPanel.Visibility = Visibility.Visible;
            LoadHistory();
        }

        private void CmbPatient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbPatient.SelectedItem is PatientOption p)
            {
                if (!string.IsNullOrWhiteSpace(p.Allergies) && p.Allergies.ToLower() != "none")
                {
                    TxtAllergies.Text = $"Patient has known allergies: {p.Allergies}";
                    AllergyAlert.Visibility = Visibility.Visible;
                }
                else
                {
                    AllergyAlert.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                AllergyAlert.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAddMedication_Click(object sender, RoutedEventArgs e)
        {
            var med = new MedicationInput { AvailableMedicines = AvailableMedicines };
            Medications.Add(med);
            UpdateMedicationsUI();
        }

        private void BtnRemoveMedication_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MedicationInput med)
            {
                Medications.Remove(med);
                UpdateMedicationsUI();
            }
        }

        private void UpdateMedicationsUI()
        {
            for (int i = 0; i < Medications.Count; i++)
            {
                Medications[i].IndexLabel = $"Medication #{i + 1}";
            }
            TxtEmptyMeds.Visibility = Medications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearForm()
        {
            CmbPatient.SelectedItem = null;
            TxtDiagnosis.Text = "";
            TxtNotes.Text = "";
            Medications.Clear();
            UpdateMedicationsUI();
            MsgPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            MsgPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;

            if (!_doctorId.HasValue)
            {
                ShowError("Doctor profile is invalid. Cannot submit prescription.");
                return;
            }

            if (CmbPatient.SelectedItem == null)
            {
                ShowError("Please select a patient.");
                return;
            }

            if (Medications.Count == 0)
            {
                ShowError("Please add at least one medication.");
                return;
            }

            foreach (var med in Medications)
            {
                if (!med.SelectedMedicineId.HasValue || string.IsNullOrWhiteSpace(med.Dosage) || med.Quantity <= 0 || string.IsNullOrWhiteSpace(med.Instructions))
                {
                    ShowError("Please fill in medicine, dosage, valid quantity, and instructions for all medications.");
                    return;
                }
            }

            try
            {
                var patId = ((PatientOption)CmbPatient.SelectedItem).Id;
                
                var sqlRecord = "INSERT INTO MedicalRecords (PatientId, DoctorId, RecordDate, Diagnosis, Treatment) " +
                                "VALUES (@PId, @DId, @Date, @Diag, @Notes)";
                var pRec = new Dictionary<string, object?> {
                    {"PId", patId},
                    {"DId", _doctorId.Value},
                    {"Date", DateTime.Now.ToString("yyyy-MM-dd")},
                    {"Diag", TxtDiagnosis.Text},
                    {"Notes", TxtNotes.Text}
                };
                App.Database.ExecuteNonQuery(sqlRecord, pRec);
                var recordId = App.Database.ExecuteScalar<long>("SELECT last_insert_rowid()");

                foreach (var med in Medications)
                {
                    var mOpt = AvailableMedicines.First(m => m.Id == med.SelectedMedicineId);
                    var sqlRx = "INSERT INTO Prescriptions (MedicalRecordId, Medicine, Dosage, Quantity, Instructions, CreatedAt, IsProcessed) " +
                                "VALUES (@MId, @Med, @Dos, @Qty, @Instr, @CreatedAt, 0)";
                    var pRx = new Dictionary<string, object?> {
                        {"MId", recordId},
                        {"Med", mOpt.Name},
                        {"Dos", med.Dosage},
                        {"Qty", med.Quantity},
                        {"Instr", med.Instructions},
                        {"CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
                    };
                    App.Database.ExecuteNonQuery(sqlRx, pRx);
                }

                ClearForm();
                MsgPanel.Visibility = Visibility.Visible;
                LoadHistory();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private void TxtSearchHistory_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterHistory();
        }
    }

    public class PatientOption
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Allergies { get; set; }
        public string DisplayValue => $"{FullName} {(string.IsNullOrWhiteSpace(RegistrationNumber) ? "" : $"({RegistrationNumber})")}";
    }

    public class MedicineOption
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Stock { get; set; }
        public string NameWithStock => $"{Name} (Stock: {Stock})";
    }

    public class MedicationInput : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        private string? _indexLabel;
        public string? IndexLabel { get => _indexLabel; set { _indexLabel = value; Notify("IndexLabel"); } }

        public ObservableCollection<MedicineOption>? AvailableMedicines { get; set; }
        
        private long? _selectedMedicineId;
        public long? SelectedMedicineId { get => _selectedMedicineId; set { _selectedMedicineId = value; Notify("SelectedMedicineId"); } }

        private string? _dosage;
        public string? Dosage { get => _dosage; set { _dosage = value; Notify("Dosage"); } }

        private int _quantity = 1;
        public int Quantity { get => _quantity; set { _quantity = value; Notify("Quantity"); } }

        private string? _instructions;
        public string? Instructions { get => _instructions; set { _instructions = value; Notify("Instructions"); } }

        private void Notify(string prop) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
    }

    public class HistoryItem
    {
        public string? PatientName { get; set; }
        public string? DiagnosisAndDate { get; set; }
        public bool IsProcessed { get; set; }
        public string? Status { get; set; }
        public List<HistoryMedItem> Items { get; set; } = new();
        public string? Notes { get; set; }
        public Visibility HasNotes { get; set; }
        public string? RawDiag { get; set; }
    }

    public class HistoryMedItem
    {
        public string? MedAndDosage { get; set; }
        public string? QtyAndInstr { get; set; }
    }
}
