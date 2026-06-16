using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class InventoryView : UserControl
    {
        public ObservableCollection<InventoryItemDisplay> Inventory { get; set; } = new();
        private int? _editingItemId = null;
        private int? _deleteItemId = null;

        public InventoryView()
        {
            InitializeComponent();
            Loaded += InventoryView_Loaded;
            InventoryList.ItemsSource = Inventory;
        }

        private void InventoryView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInventory();
        }

        private void LoadInventory()
        {
            try
            {
                Inventory.Clear();
                var sql = "SELECT * FROM Inventory WHERE 1=1";
                var p = new Dictionary<string, object?>();

                var query = TxtSearch.Text == "Search items..." ? "" : TxtSearch.Text;
                if (!string.IsNullOrWhiteSpace(query))
                {
                    sql += " AND (ItemName LIKE @Q OR Supplier LIKE @Q)";
                    p.Add("Q", "%" + query + "%");
                }

                if (CmbCategory.SelectedItem is ComboBoxItem item && item.Content.ToString() != "All Categories")
                {
                    sql += " AND Category = @Cat";
                    p.Add("Cat", item.Content.ToString());
                }

                sql += " ORDER BY ItemName";

                var data = App.Database.ExecuteQuery(sql, p);

                int total = 0, inStock = 0, lowStock = 0, outStock = 0;

                foreach (DataRow row in data.Rows)
                {
                    int qty = Convert.ToInt32(row["Quantity"] ?? 0);
                    int minStock = Convert.ToInt32(row["ReorderLevel"] ?? 0);
                    
                    string statusLabel = "In Stock";
                    string statusBg = "#f0fdf4";
                    string statusColor = "#16a34a";
                    string qtyColor = "#374151";

                    total++;
                    if (qty == 0)
                    {
                        statusLabel = "Out of Stock";
                        statusBg = "#fef2f2";
                        statusColor = "#991b1b";
                        qtyColor = "#8A0007";
                        outStock++;
                    }
                    else if (qty <= minStock)
                    {
                        statusLabel = "Low Stock";
                        statusBg = "#fffbeb";
                        statusColor = "#92400e";
                        qtyColor = "#8A0007";
                        lowStock++;
                    }
                    else
                    {
                        inStock++;
                    }

                    Inventory.Add(new InventoryItemDisplay
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Name = row["ItemName"]?.ToString() ?? "",
                        Category = row["Category"]?.ToString() ?? "",
                        Quantity = qty,
                        Unit = row["Unit"]?.ToString() ?? "",
                        ExpiryDate = row["ExpiryDate"]?.ToString()?.Split(' ')[0] ?? "",
                        MinStock = minStock,
                        Supplier = row["Supplier"]?.ToString() ?? "",
                        StatusLabel = statusLabel,
                        StatusBg = statusBg,
                        StatusColor = statusColor,
                        QtyColor = qtyColor
                    });
                }

                TxtTotalItems.Text = total.ToString();
                TxtInStock.Text = inStock.ToString();
                TxtLowStock.Text = lowStock.ToString();
                TxtOutStock.Text = outStock.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading inventory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtSearch.Text == "Search items...")
            {
                TxtSearch.Text = "";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                TxtSearch.Text = "Search items...";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) LoadInventory(); }
        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) LoadInventory(); }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            _editingItemId = null;
            TxtItemTitle.Text = "Add Inventory Item";
            IName.Text = ICategory.Text = IUnit.Text = IQuantity.Text = IMinStock.Text = ISupplier.Text = "";
            IExpiry.SelectedDate = null;
            OverlayItem.Visibility = Visibility.Visible;
        }

        private void BtnEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                var item = Inventory.FirstOrDefault(i => i.Id == id);
                if (item != null)
                {
                    _editingItemId = id;
                    TxtItemTitle.Text = "Edit Item";
                    IName.Text = item.Name;
                    ICategory.Text = item.Category;
                    IUnit.Text = item.Unit;
                    IQuantity.Text = item.Quantity.ToString();
                    IMinStock.Text = item.MinStock.ToString();
                    ISupplier.Text = item.Supplier;
                    if (DateTime.TryParse(item.ExpiryDate, out DateTime d)) IExpiry.SelectedDate = d;
                    else IExpiry.SelectedDate = null;
                    
                    OverlayItem.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(IName.Text) || string.IsNullOrWhiteSpace(ICategory.Text))
                {
                    MessageBox.Show("Name and Category are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int qty = int.TryParse(IQuantity.Text, out int q) ? q : 0;
                int min = int.TryParse(IMinStock.Text, out int m) ? m : 0;
                string exp = IExpiry.SelectedDate.HasValue ? IExpiry.SelectedDate.Value.ToString("yyyy-MM-dd") : null;

                var p = new Dictionary<string, object?>
                {
                    {"Name", IName.Text.Trim()},
                    {"Cat", ICategory.Text.Trim()},
                    {"Qty", qty},
                    {"Unit", IUnit.Text.Trim()},
                    {"Min", min},
                    {"Exp", exp},
                    {"Sup", ISupplier.Text.Trim()},
                    {"Upd", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
                };

                if (_editingItemId.HasValue)
                {
                    p.Add("Id", _editingItemId.Value);
                    App.Database.ExecuteNonQuery("UPDATE Inventory SET ItemName=@Name, Category=@Cat, Quantity=@Qty, Unit=@Unit, ReorderLevel=@Min, ExpiryDate=@Exp, Supplier=@Sup, UpdatedAt=@Upd WHERE Id=@Id", p);
                }
                else
                {
                    App.Database.ExecuteNonQuery("INSERT INTO Inventory (ItemName, Category, Quantity, Unit, ReorderLevel, ExpiryDate, Supplier, UpdatedAt) VALUES (@Name, @Cat, @Qty, @Unit, @Min, @Exp, @Sup, @Upd)", p);
                }

                CloseOverlays(sender, e);
                LoadInventory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving item: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                _deleteItemId = Convert.ToInt32(btn.Tag);
                OverlayDelete.Visibility = Visibility.Visible;
            }
        }

        private void BtnConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_deleteItemId.HasValue)
                {
                    App.Database.ExecuteNonQuery("DELETE FROM Inventory WHERE Id = @Id", new Dictionary<string, object?> { { "Id", _deleteItemId.Value } });
                    _deleteItemId = null;
                }
                CloseOverlays(sender, e);
                LoadInventory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting item: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseOverlays(object sender, RoutedEventArgs e)
        {
            OverlayItem.Visibility = Visibility.Collapsed;
            OverlayDelete.Visibility = Visibility.Collapsed;
        }
    }

    public class InventoryItemDisplay
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public int MinStock { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusBg { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string QtyColor { get; set; } = string.Empty;
    }
}
