using System.Windows;
using UMCMS.App.Helpers;
using UMCMS.Data;
using UMCMS.Data.Repositories;
using UMCMS.Services;

namespace UMCMS.App
{
    /// <summary>
    /// Application composition root. Builds the shared data/service objects and
    /// initializes the database on startup. (A lightweight stand-in for a DI container.)
    /// </summary>
    public partial class App : Application
    {
        // Path comes from umcms.config if present (shared LAN DB), else local default.
        public static DatabaseService Database { get; } = new DatabaseService(DbConfig.ResolvePath());

        public static IUserRepository Users { get; } = new UserRepository(Database);
        public static IPatientRepository Patients { get; } = new PatientRepository(Database);
        public static IInventoryRepository InventoryRepo { get; } = new InventoryRepository(Database);
        public static IAppointmentRepository Appointments { get; } = new AppointmentRepository(Database);
        public static IWaitlistRepository Waitlist { get; } = new WaitlistRepository(Database);
        public static IMedicalRecordRepository MedicalRecords { get; } = new MedicalRecordRepository(Database);
        public static IDentalRecordRepository DentalRecords { get; } = new DentalRecordRepository(Database);

        public static AuthService Auth { get; } = new AuthService(Users);
        public static SettingsService Settings { get; } = new SettingsService(Database);
        public static InventoryService Inventory { get; } = new InventoryService(Database, InventoryRepo);
        public static ForecastService Forecast { get; } = new ForecastService(InventoryRepo, Settings);
        public static ReportService Reports { get; } = new ReportService(Database);
        public static NotificationService Notifications { get; } = new NotificationService(Database);
        public static AuditService Audit { get; } = new AuditService(Database);
        public static AppointmentService AppointmentSvc { get; } =
            new AppointmentService(Appointments, Waitlist, Database, Notifications, Audit);

        protected override void OnStartup(StartupEventArgs e)
        {
            // Global safety net — no unhandled exception reaches the user as a crash.
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    "Something went wrong while completing that action:\n\n" + args.Exception.Message,
                    "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };

            try
            {
                Database.InitializeDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The application could not open its database and will now close.\n\n" +
                    $"Database: {Database.DatabasePath}\n\n" +
                    "If this is a shared (LAN) database, check that the host PC is on and the shared " +
                    "folder is reachable.\n\n" + ex.Message,
                    "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            base.OnStartup(e);
        }
    }
}
