using System.Data;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Project_.src.Services
{
    /// <summary>
    /// Generates and exports reports as PDF (QuestPDF), Excel (ClosedXML), or CSV.
    /// High-level report methods pull data via <see cref="DatabaseService"/>, render
    /// the file, log the export in the Reports table, and return the saved path.
    /// </summary>
    public class ReportService
    {
        private readonly DatabaseService _db;

        // Brand colours (matching the Login screen palette).
        private const string DeepRed = "#8A0007";
        private const string Maroon = "#4E0205";

        static ReportService()
        {
            // QuestPDF Community licence (free for this use).
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ReportService(DatabaseService db)
        {
            _db = db;
        }

        // ──────────────────────────────────────────────────────────────
        //  Generic exporters — reused by every report
        // ──────────────────────────────────────────────────────────────

        /// <summary>Renders a DataTable as a simple tabular PDF and saves it.</summary>
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
                        col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < data.Columns.Count; i++)
                                columns.RelativeColumn();
                        });

                        // Header row
                        foreach (DataColumn column in data.Columns)
                        {
                            table.Cell().Background(DeepRed).Padding(4)
                                .Text(column.ColumnName).FontColor(Colors.White).Bold();
                        }

                        // Data rows
                        foreach (DataRow row in data.Rows)
                        {
                            foreach (var cell in row.ItemArray)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4).Text(cell?.ToString() ?? string.Empty);
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf(outPath);

            return outPath;
        }

        /// <summary>Renders a DataTable as an .xlsx worksheet and saves it.</summary>
        public string ExportToExcel(string sheetName, DataTable data, string outPath)
        {
            EnsureDirectory(outPath);

            using var workbook = new XLWorkbook();
            // ClosedXML can insert a DataTable directly, including headers.
            data.TableName = string.IsNullOrWhiteSpace(sheetName) ? "Report" : Sanitize(sheetName);
            var sheet = workbook.Worksheets.Add(data, data.TableName);
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml(DeepRed);
            sheet.Row(1).Style.Font.FontColor = XLColor.White;
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(outPath);

            return outPath;
        }

        /// <summary>Writes a DataTable as a CSV file (RFC-4180 quoting) and saves it.</summary>
        public string ExportToCsv(DataTable data, string outPath)
        {
            EnsureDirectory(outPath);

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", data.Columns.Cast<DataColumn>().Select(c => CsvEscape(c.ColumnName))));
            foreach (DataRow row in data.Rows)
            {
                sb.AppendLine(string.Join(",", row.ItemArray.Select(v => CsvEscape(v?.ToString()))));
            }

            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            return outPath;
        }

        // ──────────────────────────────────────────────────────────────
        //  Specific reports
        // ──────────────────────────────────────────────────────────────

        /// <summary>Patient demographics + medical history as a PDF.</summary>
        public string GeneratePatientSummaryPdf(int patientId, string outPath, int? generatedBy = null)
        {
            var data = _db.ExecuteQuery(
                @"SELECT m.RecordDate, u.FirstName || ' ' || u.LastName AS Doctor,
                         m.Diagnosis, m.Treatment, m.BloodPressure, m.Pulse, m.Temperature, m.Weight
                  FROM MedicalRecords m
                  JOIN Users u ON u.Id = m.DoctorId
                  WHERE m.PatientId = @pid
                  ORDER BY m.RecordDate DESC;",
                new Dictionary<string, object?> { ["pid"] = patientId });

            var path = ExportToPdf($"Patient Medical Summary (#{patientId})", data, outPath);
            LogReport("PatientSummary", "PDF", path, generatedBy, $"patientId={patientId}");
            return path;
        }

        /// <summary>Appointments within a date range (optionally one doctor) in the chosen format.</summary>
        public string GenerateAppointmentsReport(DateTime from, DateTime to, int? doctorId, string format, string outPath, int? generatedBy = null)
        {
            var sql = new StringBuilder(
                @"SELECT a.AppointmentDate, p.FirstName || ' ' || p.LastName AS Patient,
                         u.FirstName || ' ' || u.LastName AS Doctor, a.Status, a.Notes
                  FROM Appointments a
                  JOIN Patients p ON p.Id = a.PatientId
                  JOIN Users u    ON u.Id = a.DoctorId
                  WHERE a.AppointmentDate BETWEEN @from AND @to");
            var p = new Dictionary<string, object?>
            {
                ["from"] = from.ToString("yyyy-MM-dd 00:00:00"),
                ["to"] = to.ToString("yyyy-MM-dd 23:59:59")
            };
            if (doctorId.HasValue)
            {
                sql.Append(" AND a.DoctorId = @doc");
                p["doc"] = doctorId.Value;
            }
            sql.Append(" ORDER BY a.AppointmentDate;");

            var data = _db.ExecuteQuery(sql.ToString(), p);
            return Export("Appointments", "Appointments Report", data, format, outPath, generatedBy,
                $"from={from:yyyy-MM-dd};to={to:yyyy-MM-dd};doctorId={doctorId}");
        }

        /// <summary>Current inventory stock (optionally only items at/below reorder level).</summary>
        public string GenerateInventoryStockReport(bool lowStockOnly, string format, string outPath, int? generatedBy = null)
        {
            var sql = @"SELECT ItemName, Category, Quantity, Unit, UnitPrice, ReorderLevel, ExpiryDate, Supplier
                        FROM Inventory";
            if (lowStockOnly) sql += " WHERE Quantity <= ReorderLevel";
            sql += " ORDER BY ItemName;";

            var data = _db.ExecuteQuery(sql);
            var title = lowStockOnly ? "Low-Stock Report" : "Inventory Stock Report";
            return Export("Inventory", title, data, format, outPath, generatedBy, $"lowStockOnly={lowStockOnly}");
        }

        // ──────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>Dispatches to the right exporter based on <paramref name="format"/> and logs it.</summary>
        private string Export(string reportType, string title, DataTable data, string format, string outPath, int? generatedBy, string? parameters)
        {
            string path = format.Trim().ToUpperInvariant() switch
            {
                "PDF" => ExportToPdf(title, data, outPath),
                "EXCEL" or "XLSX" => ExportToExcel(reportType, data, outPath),
                "CSV" => ExportToCsv(data, outPath),
                _ => throw new ArgumentException($"Unsupported format '{format}'. Use PDF, Excel, or CSV.")
            };
            LogReport(reportType, format.ToUpperInvariant(), path, generatedBy, parameters);
            return path;
        }

        private void LogReport(string reportType, string format, string filePath, int? generatedBy, string? parameters)
        {
            _db.ExecuteNonQuery(
                @"INSERT INTO Reports (ReportType, Format, GeneratedBy, GeneratedDate, FilePath, Parameters)
                  VALUES (@type, @fmt, @by, @date, @path, @params);",
                new Dictionary<string, object?>
                {
                    ["type"] = reportType,
                    ["fmt"] = format,
                    ["by"] = generatedBy,
                    ["date"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["path"] = filePath,
                    ["params"] = parameters
                });
        }

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

        private static string Sanitize(string sheetName)
        {
            foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' })
                sheetName = sheetName.Replace(c, '_');
            return sheetName.Length > 31 ? sheetName[..31] : sheetName;
        }
    }
}
