using System.IO;

namespace UMCMS.App.Helpers
{
    /// <summary>
    /// Resolves where the SQLite database lives. By default it is local
    /// (<c>data/umcms.db</c> next to the app). For LAN multi-user operation, put a
    /// <c>umcms.config</c> file next to the executable whose first line is the shared
    /// path, e.g. <c>\\RECEPTION-PC\MediHelp\umcms.db</c> — every client then opens the
    /// same database (no copies, no sync conflicts).
    /// </summary>
    public static class DbConfig
    {
        public static string ConfigFile => Path.Combine(AppContext.BaseDirectory, "umcms.config");

        /// <summary>The configured shared path, or null if none / using local default.</summary>
        public static string? ConfiguredPath()
        {
            try
            {
                if (File.Exists(ConfigFile))
                    foreach (var line in File.ReadAllLines(ConfigFile))
                    {
                        var t = line.Trim();
                        if (t.Length > 0 && !t.StartsWith('#')) return t;
                    }
            }
            catch { /* fall back to local */ }
            return null;
        }

        /// <summary>Effective DB path used at startup (configured shared path or local default).</summary>
        public static string ResolvePath()
        {
            var configured = ConfiguredPath();
            if (!string.IsNullOrWhiteSpace(configured)) return configured;

            var dir = Path.Combine(AppContext.BaseDirectory, "data");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "umcms.db");
        }

        /// <summary>Writes the shared path to the config file (empty clears it → local default).</summary>
        public static void Save(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                if (File.Exists(ConfigFile)) File.Delete(ConfigFile);
                return;
            }
            File.WriteAllText(ConfigFile,
                "# UMCMS shared database path (one shared file for all LAN clients)\r\n" + path.Trim());
        }
    }
}
