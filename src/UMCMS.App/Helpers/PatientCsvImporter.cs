using System.IO;
using System.Text;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Helpers
{
    /// <summary>
    /// Imports patients from a CSV file. The header row names the columns (case-insensitive);
    /// FirstName and LastName are required. Duplicate registration numbers are skipped.
    /// </summary>
    public static class PatientCsvImporter
    {
        public sealed record Result(int Imported, int Skipped, List<string> Errors);

        public const string TemplateHeader =
            "FirstName,LastName,DateOfBirth,Gender,PhoneNumber,Email,Address,PatientType,RegistrationNumber,Faculty,Program,YearOfStudy,EmergencyContactName,EmergencyContactPhone";

        public static Result Import(string path)
        {
            var lines = File.ReadAllLines(path);
            var errors = new List<string>();
            int imported = 0, skipped = 0;

            if (lines.Length < 2) return new Result(0, 0, new() { "File is empty or has no data rows." });

            var header = SplitCsv(lines[0]);
            int Col(string name) => header.FindIndex(h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            int iFirst = Col("FirstName"), iLast = Col("LastName");
            if (iFirst < 0 || iLast < 0)
                return new Result(0, 0, new() { "CSV must contain at least FirstName and LastName columns." });

            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row])) continue;
                var f = SplitCsv(lines[row]);
                string Get(string name) { int i = Col(name); return i >= 0 && i < f.Count ? f[i].Trim() : ""; }

                string first = Get("FirstName"), last = Get("LastName");
                if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
                {
                    errors.Add($"Row {row + 1}: missing first/last name — skipped.");
                    skipped++;
                    continue;
                }

                var p = new Patient
                {
                    FirstName = first,
                    LastName = last,
                    Gender = Get("Gender"),
                    PhoneNumber = Get("PhoneNumber"),
                    Email = Get("Email"),
                    Address = Get("Address"),
                    PatientType = string.IsNullOrWhiteSpace(Get("PatientType")) ? "External" : Get("PatientType"),
                    RegistrationNumber = NullIfEmpty(Get("RegistrationNumber")),
                    Faculty = NullIfEmpty(Get("Faculty")),
                    Program = NullIfEmpty(Get("Program")),
                    DateOfBirth = DateTime.TryParse(Get("DateOfBirth"), out var dob) ? dob : null,
                    YearOfStudy = int.TryParse(Get("YearOfStudy"), out var yr) ? yr : null,
                    EmergencyContactName = NullIfEmpty(Get("EmergencyContactName")),
                    EmergencyContactPhone = NullIfEmpty(Get("EmergencyContactPhone"))
                };

                try { App.Patients.Add(p); imported++; }
                catch (Exception ex)
                {
                    skipped++;
                    errors.Add(ex.Message.Contains("UNIQUE")
                        ? $"Row {row + 1}: registration number '{p.RegistrationNumber}' already exists — skipped."
                        : $"Row {row + 1}: {ex.Message}");
                }
            }

            return new Result(imported, skipped, errors);
        }

        /// <summary>Minimal RFC-4180 line splitter (handles quoted fields and escaped quotes).</summary>
        private static List<string> SplitCsv(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields;
        }

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
