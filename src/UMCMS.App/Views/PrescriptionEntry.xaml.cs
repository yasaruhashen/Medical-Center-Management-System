using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>A prescription line being composed.</summary>
    public class RxLine
    {
        public string Medicine { get; set; } = "";
        public int? InventoryItemId { get; set; }
        public int Quantity { get; set; } = 1;
        public int TimesPerDay { get; set; } = 3;
        public string Timing { get; set; } = "After meals";
    }

    /// <summary>
    /// Reusable fast prescription entry: searchable medicine box → Enter → quantity →
    /// Enter adds a line; Enter on an empty medicine box raises <see cref="Finished"/>.
    /// Lines are editable and removable. Used by both the consultation and dental screens.
    /// </summary>
    public partial class PrescriptionEntry : UserControl
    {
        private readonly ObservableCollection<RxLine> _lines = new();

        /// <summary>Raised when the user presses Enter on an empty medicine box.</summary>
        public event Action? Finished;

        public PrescriptionEntry()
        {
            InitializeComponent();
            RxGrid.ItemsSource = _lines;
        }

        public IReadOnlyList<RxLine> Lines
        {
            get { RxGrid.CommitEdit(DataGridEditingUnit.Row, true); return _lines; }
        }

        /// <summary>Loads the searchable medicine list for the given inventory stream.</summary>
        public void Configure(string stream) => RxMedicine.ItemsSource = App.InventoryRepo.GetByStream(stream);

        public void Clear()
        {
            _lines.Clear();
            RxMedicine.SelectedIndex = -1;
            RxMedicine.Text = "";
            RxQty.Text = "1";
        }

        public void FocusMedicine() => RxMedicine.Focus();

        private void RxMedicine_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (string.IsNullOrWhiteSpace(RxMedicine.Text) && RxMedicine.SelectedItem is null)
                Finished?.Invoke();          // empty → finish the list
            else
                RxQty.Focus();               // move to quantity
        }

        private void RxQty_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            AddLine();
        }

        private void BtnAddRx_Click(object sender, RoutedEventArgs e) => AddLine();

        private void AddLine()
        {
            string medicine; int? itemId = null;
            if (RxMedicine.SelectedItem is InventoryItem it) { medicine = it.ItemName; itemId = it.Id; }
            else medicine = RxMedicine.Text.Trim();

            if (string.IsNullOrWhiteSpace(medicine)) { RxMedicine.Focus(); return; }

            _lines.Add(new RxLine
            {
                Medicine = medicine,
                InventoryItemId = itemId,
                Quantity = int.TryParse(RxQty.Text, out var q) && q > 0 ? q : 1,
                TimesPerDay = int.TryParse((RxTimes.SelectedItem as ComboBoxItem)?.Content?.ToString(), out var t) ? t : 3,
                Timing = (RxTiming.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "After meals"
            });

            RxMedicine.SelectedIndex = -1;
            RxMedicine.Text = "";
            RxQty.Text = "1";
            RxMedicine.Focus();
        }

        private void BtnRemoveRx_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is RxLine line) _lines.Remove(line);
        }
    }
}
