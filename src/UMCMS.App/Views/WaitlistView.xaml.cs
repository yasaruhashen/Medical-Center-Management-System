using System.Data;
using System.Windows;
using System.Windows.Controls;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Waitlist management: add, manually offer a slot, and accept/decline offers.
    /// Stale offers (past the acceptance window) auto-expire and cascade on load.
    /// </summary>
    public partial class WaitlistView : UserControl
    {
        public WaitlistView(User user)
        {
            InitializeComponent();
            Load();
        }

        private string? StreamFilter()
        {
            var s = (FFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return s == "All" ? null : s;
        }

        private void Load()
        {
            int expired = App.AppointmentSvc.ExpireStaleOffers();
            TxtNote.Text = expired > 0 ? $"{expired} expired offer(s) cascaded to the next patient." : "";
            Grid.ItemsSource = App.Waitlist.GetView(StreamFilter()).DefaultView;
            UpdateButtons();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Load(); }

        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id") e.Cancel = true;
        }

        private DataRowView? SelectedRow => Grid.SelectedItem as DataRowView;
        private int? SelectedId => SelectedRow is { } r ? Convert.ToInt32(r["Id"]) : null;
        private string SelectedStatus => SelectedRow?["Status"]?.ToString() ?? "";

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

        private void UpdateButtons()
        {
            bool sel = SelectedId is not null;
            bool waiting = sel && SelectedStatus == "Waiting";
            bool offered = sel && SelectedStatus == "Offered";
            BtnOffer.IsEnabled = waiting;
            BtnAccept.IsEnabled = offered;
            BtnDecline.IsEnabled = offered;
            BtnRemove.IsEnabled = sel;
        }

        private void BtnOffer_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is int id) { Info(App.AppointmentSvc.OfferSpecific(id)); Load(); }
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is int id) { Info(App.AppointmentSvc.RespondToOffer(id, accepted: true)); Load(); }
        }

        private void BtnDecline_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is int id) { Info(App.AppointmentSvc.RespondToOffer(id, accepted: false)); Load(); }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is int id) { App.Waitlist.Remove(id); Load(); }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            FPatient.ItemsSource = App.Patients.GetAll();
            TxtError.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Visible;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (FPatient.SelectedItem is not Patient p)
            {
                TxtError.Text = "Please choose a patient."; TxtError.Visibility = Visibility.Visible; return;
            }
            App.Waitlist.Add(new WaitlistEntry
            {
                PatientId = p.Id,
                Stream = (FStream.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "General",
                Priority = int.Parse((FPriority.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "5")
            });
            Overlay.Visibility = Visibility.Collapsed;
            Load();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Overlay.Visibility = Visibility.Collapsed;

        private static void Info(string msg) =>
            MessageBox.Show(msg, "Waitlist", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
