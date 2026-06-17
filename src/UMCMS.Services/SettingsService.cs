using System.Globalization;
using UMCMS.Data;

namespace UMCMS.Services
{
    /// <summary>Typed accessor over the key/value Settings table.</summary>
    public class SettingsService
    {
        private readonly DatabaseService _db;
        public SettingsService(DatabaseService db) => _db = db;

        public string? Get(string key) => _db.GetSetting(key);
        public void Set(string key, string value) => _db.SetSetting(key, value);

        public double GetDouble(string key, double fallback) =>
            double.TryParse(_db.GetSetting(key), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : fallback;

        public void SetDouble(string key, double value) =>
            _db.SetSetting(key, value.ToString(CultureInfo.InvariantCulture));
    }
}
