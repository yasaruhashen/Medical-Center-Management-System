using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class InventoryView : UserControl
    {
        public ObservableCollection<InventoryDisplay> InventoryItems { get; set; } = new ObservableCollection<InventoryDisplay>();

        public InventoryView()
        {
            InitializeComponent();
            LoadInventory();
        }

        private void LoadInventory()
        {
            InventoryItems.Clear();
            var sql = "SELECT * FROM Inventory";

            try
            {
                var data = App.Database.ExecuteQuery(sql);
                foreach (DataRow row in data.Rows)
                {
                    InventoryItems.Add(new InventoryDisplay
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        ItemName = row["ItemName"]?.ToString() ?? "—",
                        Category = row["Category"]?.ToString() ?? "—",
                        Quantity = row["Quantity"]?.ToString() + " " + (row["Unit"]?.ToString() ?? ""),
                        UnitPrice = "$" + Convert.ToDouble(row["UnitPrice"]).ToString("0.00"),
                        Supplier = row["Supplier"]?.ToString() ?? "Unknown Supplier"
                    });
                }
                InventoryList.ItemsSource = InventoryItems;
            }
            catch
            {
                // Ignore DB errors on load
            }
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Inventory Item clicked. Provide item details window here.", "WIP");
        }

        private void BtnEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                MessageBox.Show($"Edit Inventory Item ID: {id}", "WIP");
            }
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this item?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    int id = Convert.ToInt32(btn.Tag);
                    App.Database.ExecuteNonQuery("DELETE FROM Inventory WHERE Id = @Id", new System.Collections.Generic.Dictionary<string, object?> { { "Id", id } });
                    LoadInventory();
                }
            }
        }
    }

    public class InventoryDisplay
    {
        public int Id { get; set; }
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public string? Quantity { get; set; }
        public string? UnitPrice { get; set; }
        public string? Supplier { get; set; }
    }
}
