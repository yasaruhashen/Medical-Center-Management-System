using System.Data;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UMCMS.Data;

namespace UMCMS.Services
{
    /// <summary>
    /// Generates and exports reports as PDF (QuestPDF), Excel (ClosedXML) or CSV.
    /// Reports pull data via <see cref="DatabaseService"/>, render the file, log it
    /// in the Reports table, and return the saved path.
    /// </summary>
    public class ReportService
    {
        private readonly DatabaseService _db;
        private const string DeepRed = "#8A0007";
        private const string Maroon = "#4E0205";

        static ReportService() => QuestPDF.Settings.License = LicenseType.Community;

        public ReportService(DatabaseService db) => _db = db;

        // ── Generic exporters ──────────────────────────────────────────

        public string ExportToPdf(string title, DataTable data, string outPath)
        {
            EnsureDirectory(outPath);
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(t => t.FontSize(9));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("Medi Help J'Pura").FontSize(16).Bold().FontColor(Maroon);
                        col.Item().Text("University Medical Centre").FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(6).Text(title).FontSize(13).Bold().FontColor(DeepRed);
                        col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < data.Columns.Count; i++) columns.RelativeColumn();
                        });
                        foreach (DataColumn column in data.Columns)
                            table.Cell().Background(DeepRed).Padding(4).Text(column.ColumnName).FontColor(Colors.White).Bold();
                        foreach (DataRow row in data.Rows)
                            foreach (var cell in row.ItemArray)
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                     .Text(cell?.ToString() ?? string.Empty);
                    });
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Page "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                });
            }).GeneratePdf(outPath);
            return outPath;
        }

        public string ExportToExcel(string sheetName, DataTable data, string outPath)
        {
            EnsureDirectory(outPath);
            using var workbook = new XLWorkbook();
            data.TableName = string.IsNullOrWhiteSpace(sheetName) ? "Report" : Sanitize(sheetName);
            var sheet = workbook.Worksheets.Add(data, data.TableName);
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml(DeepRed);
            sheet.Row(1).Style.Font.FontColor = XLColor.White;
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(outPath);
            return outPath;
        }

        public string ExportToCsv(DataTable data, string outPath)
        {
            EnsureDirectory(outPath);
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", data.Columns.Cast<DataColumn>().Select(c => CsvEscape(c.ColumnName))));
            foreach (DataRow row in data.Rows)
                sb.AppendLine(string.Join(",", row.ItemArray.Select(v => CsvEscape(v?.ToString()))));
            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            return outPath;
        }

        // ── Specific reports ───────────────────────────────────────────

        public string GeneratePatientSummaryPdf(int patientId, string outPath, int? by = null)
        {
            var data = _db.ExecuteQuery(
                @"SELECT m.RecordDate, u.FirstName || ' ' || u.LastName AS Doctor,
                         m.Diagnosis, m.Treatment, m.BloodPressure, m.Pulse, m.Temperature, m.Weight
                  FROM MedicalRecords m JOIN Users u ON u.Id=m.DoctorId
                  WHERE m.PatientId=@pid ORDER BY m.RecordDate DESC;",
                new Dictionary<string, object?> { ["pid"] = patientId });
            var path = ExportToPdf($"Patient Medical Summary (#{patientId})", data, outPath);
            LogReport("PatientSummary", "PDF", path, by, $"patientId={patientId}");
            return path;
        }

        public string GenerateInventoryStockReport(string? stream, bool lowStockOnly, string format, string outPath, int? by = null)
        {
            var sql = "SELECT ItemName, Stream, Category, Quantity, Unit, UnitPrice, ReorderLevel FROM InventoryItems";
            var conditions = new List<string>();
            var p = new Dictionary<string, object?>();
            if (stream is not null) { conditions.Add("Stream=@s"); p["s"] = stream; }
            if (lowStockOnly) conditions.Add("Quantity <= ReorderLevel");
            if (conditions.Count > 0) sql += " WHERE " + string.Join(" AND ", conditions);
            sql += " ORDER BY ItemName;";
            var data = _db.ExecuteQuery(sql, p);
            return Export("InventoryStock", lowStockOnly ? "Low-Stock Report" : "Inventory Stock Report",
                data, format, outPath, by, $"stream={stream};lowStockOnly={lowStockOnly}");
        }

        public string GenerateAppointmentsReport(DateTime from, DateTime to, int? doctorId, string format, string outPath, int? by = null)
        {
            var sql = new StringBuilder(
                @"SELECT a.AppointmentDate, p.FirstName||' '||p.LastName AS Patient,
                         u.FirstName||' '||u.LastName AS Doctor, a.Stream, a.Status
                  FROM Appointments a JOIN Patients p ON p.Id=a.PatientId JOIN Users u ON u.Id=a.DoctorId
                  WHERE a.AppointmentDate BETWEEN @from AND @to");
            var p = new Dictionary<string, object?>
            { ["from"] = from.ToString("yyyy-MM-dd 00:00:00"), ["to"] = to.ToString("yyyy-MM-dd 23:59:59") };
            if (doctorId.HasValue) { sql.Append(" AND a.DoctorId=@doc"); p["doc"] = doctorId.Value; }
            sql.Append(" ORDER BY a.AppointmentDate;");
            var data = _db.ExecuteQuery(sql.ToString(), p);
            return Export("Appointments", "Appointments Report", data, format, outPath, by,
                $"from={from:yyyy-MM-dd};to={to:yyyy-MM-dd};doctorId={doctorId}");
        }

        /// <summary>Renders a printable prescription slip (PDF) for a medical record.</summary>
        public string GeneratePrescriptionSlipPdf(int medicalRecordId, string outPath, int? by = null)
        {
            EnsureDirectory(outPath);

            var hdr = _db.ExecuteQuery(
                @"SELECT m.RecordDate, m.Diagnosis,
                         p.FirstName||' '||p.LastName AS Patient, p.RegistrationNumber AS Reg,
                         p.Gender, p.DateOfBirth,
                         u.FirstName||' '||u.LastName AS Doctor, u.Specialization
                  FROM MedicalRecords m
                  JOIN Patients p ON p.Id=m.PatientId
                  JOIN Users u ON u.Id=m.DoctorId
                  WHERE m.Id=@id;",
                new Dictionary<string, object?> { ["id"] = medicalRecordId });

            if (hdr.Rows.Count == 0) throw new InvalidOperationException("Consultation record not found.");
            var h = hdr.Rows[0];

            string patient = h["Patient"]?.ToString() ?? "";
            string reg = h["Reg"] as string ?? "—";
            string gender = h["Gender"]?.ToString() ?? "";
            string doctor = h["Doctor"]?.ToString() ?? "";
            string? spec = h["Specialization"] as string;
            string role = spec == "Dental" ? "Dentist" : spec == "General" ? "General Physician" : "Doctor";
            string diagnosis = h["Diagnosis"]?.ToString() ?? "";
            DateTime date = DateTime.TryParse(h["RecordDate"]?.ToString(), out var dt) ? dt : DateTime.Now;
            string age = DateTime.TryParse(h["DateOfBirth"]?.ToString(), out var dob)
                ? ((int)((DateTime.Today - dob).TotalDays / 365.25)).ToString() : "—";

            var meds = _db.Query(
                "SELECT Medicine, Quantity, TimesPerDay, MealTiming FROM Prescriptions WHERE MedicalRecordId=@id ORDER BY Id;",
                r => (Medicine: r["Medicine"]?.ToString() ?? "",
                      Qty: Convert.ToInt32(r["Quantity"]),
                      Times: Convert.ToInt32(r["TimesPerDay"]),
                      Timing: r["MealTiming"] as string ?? ""),
                new Dictionary<string, object?> { ["id"] = medicalRecordId });

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.Size(PageSizes.A5);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Medi Help J'Pura").FontSize(18).Bold().FontColor(Maroon);
                        col.Item().Text("University of Sri Jayewardenepura — Medical Centre").FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(DeepRed);
                        col.Item().PaddingTop(6).Text("MEDICAL PRESCRIPTION").FontSize(12).Bold().FontColor(DeepRed);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(4);
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t => { t.Span("Patient: ").SemiBold(); t.Span(patient); });
                            r.RelativeItem().AlignRight().Text(t => { t.Span("Date: ").SemiBold(); t.Span(date.ToString("yyyy-MM-dd HH:mm")); });
                        });
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t => { t.Span("Uni ID: ").SemiBold(); t.Span(reg); });
                            r.RelativeItem().Text(t => { t.Span("Age/Sex: ").SemiBold(); t.Span($"{age} / {gender}"); });
                        });
                        col.Item().Text(t => { t.Span("Doctor: ").SemiBold(); t.Span($"Dr. {doctor} ({role})"); });
                        if (!string.IsNullOrWhiteSpace(diagnosis))
                            col.Item().Text(t => { t.Span("Diagnosis: ").SemiBold(); t.Span(diagnosis); });

                        col.Item().PaddingTop(10).Text("℞").FontSize(26).Bold().FontColor(Maroon);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.ConstantColumn(20); c.RelativeColumn(3); c.ConstantColumn(40); c.RelativeColumn(3); });
                            foreach (var head in new[] { "#", "Medicine", "Qty", "Directions" })
                                table.Cell().Background(DeepRed).Padding(4).Text(head).FontColor(Colors.White).Bold();
                            int n = 1;
                            foreach (var m in meds)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text((n++).ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(m.Medicine);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(m.Qty.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{m.Times} time(s)/day · {m.Timing}");
                            }
                            if (meds.Count == 0)
                                table.Cell().ColumnSpan(4).Padding(6).Text("No medicines prescribed.").FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().PaddingTop(20).AlignRight().Text("_______________________");
                        col.Item().AlignRight().Text("Doctor's signature").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(outPath);

            LogReport("PrescriptionSlip", "PDF", outPath, by, $"recordId={medicalRecordId}");
            return outPath;
        }

        /// <summary>Dispensing labels — one sticker per prescribed medicine for a record.</summary>
        public string GenerateDispensingLabelsPdf(int medicalRecordId, string outPath, int? by = null)
        {
            EnsureDirectory(outPath);

            var hdr = _db.ExecuteQuery(
                @"SELECT p.FirstName||' '||p.LastName AS Patient, COALESCE(p.RegistrationNumber,'—') AS Reg, m.RecordDate
                  FROM MedicalRecords m JOIN Patients p ON p.Id=m.PatientId WHERE m.Id=@id;",
                new Dictionary<string, object?> { ["id"] = medicalRecordId });
            if (hdr.Rows.Count == 0) throw new InvalidOperationException("Consultation record not found.");
            string patient = hdr.Rows[0]["Patient"]?.ToString() ?? "";
            string reg = hdr.Rows[0]["Reg"]?.ToString() ?? "—";

            var meds = _db.Query(
                "SELECT Medicine, Quantity, TimesPerDay, MealTiming FROM Prescriptions WHERE MedicalRecordId=@id ORDER BY Id;",
                r => (Medicine: r["Medicine"]?.ToString() ?? "", Qty: Convert.ToInt32(r["Quantity"]),
                      Times: Convert.ToInt32(r["TimesPerDay"]), Timing: r["MealTiming"] as string ?? ""),
                new Dictionary<string, object?> { ["id"] = medicalRecordId });

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(28);
                    page.Size(PageSizes.A5);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black));
                    page.Header().Text("Dispensing Labels").FontSize(13).Bold().FontColor(Maroon);
                    page.Content().PaddingTop(8).Column(col =>
                    {
                        col.Spacing(8);
                        if (meds.Count == 0) col.Item().Text("No medicines to label.").FontColor(Colors.Grey.Medium);
                        foreach (var m in meds)
                        {
                            col.Item().Border(1).BorderColor(DeepRed).Padding(10).Column(c =>
                            {
                                c.Item().Text("Medi Help J'Pura — University Medical Centre").FontSize(8).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{patient}   (Uni ID: {reg})").FontSize(9);
                                c.Item().PaddingTop(2).Text(m.Medicine).FontSize(13).Bold().FontColor(Maroon);
                                c.Item().Text($"Take {m.Qty} — {m.Times} time(s) per day, {m.Timing}").FontSize(11);
                                c.Item().PaddingTop(2).Text($"Dispensed: {DateTime.Now:yyyy-MM-dd}   ·   Keep out of reach of children").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        }
                    });
                });
            }).GeneratePdf(outPath);

            LogReport("DispensingLabels", "PDF", outPath, by, $"recordId={medicalRecordId}");
            return outPath;
        }

        /// <summary>Full patient EHR: demographics, every consultation (+ its prescriptions) and dental records.</summary>
        public string GeneratePatientEhrPdf(int patientId, string outPath, int? by = null)
        {
            EnsureDirectory(outPath);

            var ph = _db.ExecuteQuery(
                @"SELECT FirstName||' '||LastName AS Name, RegistrationNumber AS Reg, Gender, DateOfBirth,
                         PatientType, Faculty, Program, YearOfStudy, PhoneNumber
                  FROM Patients WHERE Id=@id;",
                new Dictionary<string, object?> { ["id"] = patientId });
            if (ph.Rows.Count == 0) throw new InvalidOperationException("Patient not found.");
            var p = ph.Rows[0];
            string age = DateTime.TryParse(p["DateOfBirth"]?.ToString(), out var dob)
                ? ((int)((DateTime.Today - dob).TotalDays / 365.25)).ToString() : "—";

            var records = _db.Query(
                @"SELECT m.Id AS Id, m.RecordDate AS Dt, u.FirstName||' '||u.LastName AS Doctor,
                         m.Diagnosis, m.Treatment, m.Symptoms, m.BloodPressure AS BP, m.Pulse, m.Temperature AS Tmp, m.Weight
                  FROM MedicalRecords m JOIN Users u ON u.Id=m.DoctorId
                  WHERE m.PatientId=@id ORDER BY m.RecordDate DESC;",
                r => new
                {
                    Id = Convert.ToInt32(r["Id"]),
                    Dt = r["Dt"]?.ToString() ?? "",
                    Doctor = r["Doctor"]?.ToString() ?? "",
                    Diagnosis = r["Diagnosis"]?.ToString() ?? "",
                    Treatment = r["Treatment"]?.ToString() ?? "",
                    Vitals = $"BP {Val(r["BP"])}  Pulse {Val(r["Pulse"])}  Temp {Val(r["Tmp"])}  Wt {Val(r["Weight"])}"
                },
                new Dictionary<string, object?> { ["id"] = patientId });

            var dental = _db.Query(
                @"SELECT d.RecordDate AS Dt, u.FirstName||' '||u.LastName AS Dentist, d.GumHealth, d.XrayNotes, d.Notes
                  FROM DentalRecords d JOIN Users u ON u.Id=d.DoctorId
                  WHERE d.PatientId=@id ORDER BY d.RecordDate DESC;",
                r => new
                {
                    Dt = r["Dt"]?.ToString() ?? "",
                    Dentist = r["Dentist"]?.ToString() ?? "",
                    Gum = r["GumHealth"]?.ToString() ?? "",
                    Notes = $"{r["XrayNotes"]} {r["Notes"]}".Trim()
                },
                new Dictionary<string, object?> { ["id"] = patientId });

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Medi Help J'Pura").FontSize(16).Bold().FontColor(Maroon);
                        col.Item().Text("Electronic Health Record").FontSize(11).Bold().FontColor(DeepRed);
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(DeepRed);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Spacing(3);
                        col.Item().Text(t => { t.Span("Patient: ").SemiBold(); t.Span(p["Name"]?.ToString()); });
                        col.Item().Text(t =>
                        {
                            t.Span("Uni ID: ").SemiBold(); t.Span($"{p["Reg"] ?? "—"}    ");
                            t.Span("Age/Sex: ").SemiBold(); t.Span($"{age} / {p["Gender"]}    ");
                            t.Span("Type: ").SemiBold(); t.Span(p["PatientType"]?.ToString());
                        });
                        if (!string.IsNullOrWhiteSpace(p["Faculty"]?.ToString()))
                            col.Item().Text(t => { t.Span("Faculty: ").SemiBold(); t.Span($"{p["Faculty"]} — {p["Program"]} (Yr {p["YearOfStudy"]})"); });
                        col.Item().Text(t => { t.Span("Generated: ").SemiBold(); t.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")); });

                        col.Item().PaddingTop(10).Text($"Consultations ({records.Count})").FontSize(12).Bold().FontColor(Maroon);
                        if (records.Count == 0) col.Item().Text("No consultations recorded.").FontColor(Colors.Grey.Medium);
                        foreach (var r in records)
                        {
                            col.Item().PaddingTop(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Column(c =>
                            {
                                c.Item().Text(t => { t.Span($"{r.Dt}  ").SemiBold(); t.Span($"· Dr. {r.Doctor}").FontColor(Colors.Grey.Darken1); });
                                c.Item().Text(t => { t.Span("Diagnosis: ").SemiBold(); t.Span(r.Diagnosis); });
                                if (!string.IsNullOrWhiteSpace(r.Treatment)) c.Item().Text($"Treatment: {r.Treatment}");
                                c.Item().Text(r.Vitals).FontSize(9).FontColor(Colors.Grey.Darken1);
                                var meds = _db.Query(
                                    "SELECT Medicine, Quantity, TimesPerDay, MealTiming FROM Prescriptions WHERE MedicalRecordId=@m ORDER BY Id;",
                                    x => $"• {x["Medicine"]} ×{x["Quantity"]} — {x["TimesPerDay"]}×/day, {x["MealTiming"]}",
                                    new Dictionary<string, object?> { ["m"] = r.Id });
                                foreach (var m in meds) c.Item().PaddingLeft(10).Text(m).FontSize(9);
                            });
                        }

                        if (dental.Count > 0)
                        {
                            col.Item().PaddingTop(12).Text($"Dental records ({dental.Count})").FontSize(12).Bold().FontColor(Maroon);
                            foreach (var d in dental)
                                col.Item().PaddingTop(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Column(c =>
                                {
                                    c.Item().Text(t => { t.Span($"{d.Dt}  ").SemiBold(); t.Span($"· Dr. {d.Dentist}").FontColor(Colors.Grey.Darken1); });
                                    c.Item().Text($"Gum: {d.Gum}");
                                    if (!string.IsNullOrWhiteSpace(d.Notes)) c.Item().Text(d.Notes).FontSize(9);
                                });
                        }
                    });

                    page.Footer().AlignRight().Text(t => { t.Span("Page "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
                });
            }).GeneratePdf(outPath);

            LogReport("PatientEHR", "PDF", outPath, by, $"patientId={patientId}");
            return outPath;
        }

        private static string Val(object? o) => o is null or DBNull ? "—" : o.ToString() ?? "—";

        // ── Expense / analytics reports (FR-INV-9) ──────────────────────

        /// <summary>Medication cost grouped by patient faculty.</summary>
        public string GenerateExpenseByFaculty(string format, string outPath, int? by = null)
        {
            var data = _db.ExecuteQuery(
                @"SELECT COALESCE(p.Faculty,'External / none') AS Faculty,
                         SUM(rx.Quantity) AS [Units],
                         CAST(SUM(rx.Quantity * i.UnitPrice) AS INTEGER) AS [Cost (Rs)]
                  FROM Prescriptions rx
                  JOIN MedicalRecords m ON m.Id=rx.MedicalRecordId
                  JOIN Patients p ON p.Id=m.PatientId
                  JOIN InventoryItems i ON i.Id=rx.InventoryItemId
                  GROUP BY p.Faculty ORDER BY [Cost (Rs)] DESC;");
            return Export("ExpenseByFaculty", "Medication Expenditure per Faculty", data, format, outPath, by, null);
        }

        /// <summary>Top-consumed drugs by units (from monthly consumption history).</summary>
        public string GenerateTopConsumed(string format, string outPath, int? by = null)
        {
            var data = _db.ExecuteQuery(
                @"SELECT i.ItemName AS Medicine, i.Stream,
                         SUM(c.QuantityConsumed) AS [Units consumed],
                         CAST(SUM(c.QuantityConsumed) * i.UnitPrice AS INTEGER) AS [Value (Rs)]
                  FROM ConsumptionRecords c JOIN InventoryItems i ON i.Id=c.InventoryItemId
                  GROUP BY i.Id ORDER BY [Units consumed] DESC LIMIT 15;");
            return Export("TopConsumed", "Top Consumed Drugs", data, format, outPath, by, null);
        }

        /// <summary>Total medication expenditure per financial quarter.</summary>
        public string GenerateQuarterlyExpense(string format, string outPath, int? by = null)
        {
            var data = _db.ExecuteQuery(
                @"SELECT (c.Year || ' Q' || ((c.Month-1)/3+1)) AS Quarter,
                         CAST(SUM(c.QuantityConsumed * i.UnitPrice) AS INTEGER) AS [Cost (Rs)]
                  FROM ConsumptionRecords c JOIN InventoryItems i ON i.Id=c.InventoryItemId
                  GROUP BY Quarter ORDER BY Quarter;");
            return Export("QuarterlyExpense", "Quarterly Medication Expenditure", data, format, outPath, by, null);
        }

        /// <summary>Public entry point to export any prepared DataTable (e.g. a purchase order).</summary>
        public string ExportDataTable(string reportType, string title, DataTable data, string format, string outPath, int? by = null)
            => Export(reportType, title, data, format, outPath, by, null);

        // ── Helpers ────────────────────────────────────────────────────

        private string Export(string type, string title, DataTable data, string format, string outPath, int? by, string? prms)
        {
            string path = format.Trim().ToUpperInvariant() switch
            {
                "PDF" => ExportToPdf(title, data, outPath),
                "EXCEL" or "XLSX" => ExportToExcel(type, data, outPath),
                "CSV" => ExportToCsv(data, outPath),
                _ => throw new ArgumentException($"Unsupported format '{format}'. Use PDF, Excel or CSV.")
            };
            LogReport(type, format.ToUpperInvariant(), path, by, prms);
            return path;
        }

        private void LogReport(string type, string format, string filePath, int? by, string? prms) =>
            _db.ExecuteNonQuery(
                @"INSERT INTO Reports (ReportType, Format, GeneratedBy, GeneratedDate, FilePath, Parameters)
                  VALUES (@t,@f,@b,@d,@p,@pr);",
                new Dictionary<string, object?>
                {
                    ["t"] = type, ["f"] = format, ["b"] = by,
                    ["d"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ["p"] = filePath, ["pr"] = prms
                });

        private static void EnsureDirectory(string outPath)
        {
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }

        private static string CsvEscape(string? value)
        {
            value ??= string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static string Sanitize(string s)
        {
            foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' }) s = s.Replace(c, '_');
            return s.Length > 31 ? s[..31] : s;
        }
    }
}
