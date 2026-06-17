using UMCMS.Domain;
using UMCMS.Domain.Entities;

namespace UMCMS.Services
{
    /// <summary>
    /// Builds the navigation menu for a user, showing ONLY the sections that role
    /// (and, for doctors, that specialization) requires — nothing else.
    /// </summary>
    public static class NavigationCatalog
    {
        public static IReadOnlyList<NavItem> For(User user) => user.Role switch
        {
            "Admin" => new List<NavItem>
            {
                new("dashboard", "Dashboard",        "🏠"),
                new("users",     "User Management",  "👥"),
                new("inventory", "Inventory",        "📦"),
                new("forecast",  "Reorder Forecast", "📈"),
                new("reports",        "Reports",        "📊"),
                new("notifications",  "Notifications",  "🔔"),
                new("audit",          "Audit Log",      "📜"),
                new("backup",    "Backup & Restore", "💾"),
                new("settings",  "Settings",         "⚙️"),
            },

            "Receptionist" => new List<NavItem>
            {
                new("dashboard",    "Dashboard",    "🏠"),
                new("patients",     "Patients",     "🧑‍🤝‍🧑"),
                new("appointments", "Appointments", "📅"),
                new("waitlist",     "Waitlist",     "⏳"),
            },

            "Pharmacist" => new List<NavItem>
            {
                new("dashboard",  "Dashboard",        "🏠"),
                new("dispensary", "Dispensary",       "💊"),
                new("inventory",  "Inventory",        "📦"),
                new("forecast",   "Reorder Forecast", "📈"),
            },

            "Doctor" when user.IsDentist => new List<NavItem>
            {
                new("dashboard",    "Dashboard",       "🏠"),
                new("my-queue",     "My Appointments", "📅"),
                new("patients",     "Patients",        "🧑‍🤝‍🧑"),
                new("dental",       "Dental Records",  "🦷"),
                new("dental-stock", "Dental Supplies", "📦"),
                new("availability", "My Availability", "🟢"),
            },

            "Doctor" => new List<NavItem> // General physician
            {
                new("dashboard",     "Dashboard",          "🏠"),
                new("my-queue",      "My Appointments",    "📅"),
                new("patients",      "Patients",           "🧑‍🤝‍🧑"),
                new("consultations", "Consultations",      "🩺"),
                new("gen-meds",      "General Medication", "💊"),
                new("availability",  "My Availability",    "🟢"),
            },

            _ => new List<NavItem> { new("dashboard", "Dashboard", "🏠") }
        };

        public static string RoleLabel(User user) => user.Role switch
        {
            "Doctor" when user.IsDentist => "Dentist",
            "Doctor" => "General Physician",
            "Receptionist" => "Receptionist",
            "Pharmacist" => "Pharmacist",
            "Admin" => "Administrator",
            _ => user.Role
        };
    }
}
