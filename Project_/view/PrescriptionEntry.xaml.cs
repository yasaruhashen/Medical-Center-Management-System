using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_.Views
{
    public class PrescriptionLine
    {
        public string Medicine { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string TimesPerDay { get; set; } = "";
        public string Timing { get; set; } = "";
    }

    public partial class PrescriptionEntry : UserControl
    {
        public ObservableCollection<PrescriptionLine> Lines { get; } = new ObservableCollection<PrescriptionLine>();

        public PrescriptionEntry()
        {
            InitializeComponent();
            RxGrid.ItemsSource = Lines;
        }

        private void RxMedicine_PreviewKeyDown(object sender, KeyEventArgs e) 
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (!string.IsNullOrWhiteSpace(RxMedicine.Text))
                {
                    RxQty.Focus();
                }
            }
        }
        
        private void RxQty_PreviewKeyDown(object sender, KeyEventArgs e) 
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnAddRx_Click(sender, new RoutedEventArgs());
                RxMedicine.Focus();
            }
        }
        
        private void BtnAddRx_Click(object sender, RoutedEventArgs e) 
        {
            if (!string.IsNullOrWhiteSpace(RxMedicine.Text))
            {
                Lines.Add(new PrescriptionLine
                {
                    Medicine = RxMedicine.Text,
                    Quantity = RxQty.Text,
                    TimesPerDay = RxTimes.Text,
                    Timing = RxTiming.Text
                });
                RxMedicine.Text = "";
                RxQty.Text = "1";
            }
        }
        
        private void BtnRemoveRx_Click(object sender, RoutedEventArgs e) 
        {
            if (sender is Button btn && btn.DataContext is PrescriptionLine line)
            {
                Lines.Remove(line);
            }
        }
        
        public void Clear()
        {
            Lines.Clear();
            RxMedicine.Text = "";
            RxQty.Text = "1";
        }
    }
}
