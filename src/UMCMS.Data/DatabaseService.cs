using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UMCMS.Data
{
    /// <summary>
    /// Central SQLite data-access helper. All database access goes through these
    /// parameterized helpers (no string-concatenated SQL). Repositories depend on it.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseService(string? dbPath = null)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
                Directory.CreateDirectory(dataDir);
                dbPath = Path.Combine(dataDir, "umcms.db");
            }

            _dbPath = dbPath;
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        public string DatabasePath => _dbPath;

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var pragma = connection.CreateCommand();
            // foreign_keys: integrity; busy_timeout: wait (don't fail) when another LAN
            // client holds a write lock — lets multiple machines share one database file.
            pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 8000;";
            pragma.ExecuteNonQuery();
            return connection;
        }

        /// <summary>Retries an operation a few times if the database is momentarily locked by another client.</summary>
        private static T WithRetry<T>(Func<T> op)
        {
            const int maxAttempts = 5;
            for (int attempt = 1; ; attempt++)
            {
                try { return op(); }
                catch (SqliteException ex) when (attempt < maxAttempts &&
                       (ex.SqliteErrorCode == 5 /* BUSY */ || ex.SqliteErrorCode == 6 /* LOCKED */))
                {
                    Thread.Sleep(120 * attempt);
                }
            }
        }

        private static void BindParameters(SqliteCommand command, IDictionary<string, object?>? parameters)
        {
            if (parameters is null) return;
            foreach (var kvp in parameters)
            {
                var name = kvp.Key.StartsWith('@') ? kvp.Key : "@" + kvp.Key;
                command.Parameters.AddWithValue(name, kvp.Value ?? DBNull.Value);
            }
        }

        // ── Core helpers ───────────────────────────────────────────────

        public int ExecuteNonQuery(string sql, IDictionary<string, object?>? parameters = null) => WithRetry(() =>
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            BindParameters(command, parameters);
            return command.ExecuteNonQuery();
        });

        public T? ExecuteScalar<T>(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            BindParameters(command, parameters);
            var result = command.ExecuteScalar();
            if (result is null || result is DBNull) return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public long ExecuteInsertReturnId(string sql, IDictionary<string, object?>? parameters = null) => WithRetry(() =>
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql + "; SELECT last_insert_rowid();";
            BindParameters(command, parameters);
            var result = command.ExecuteScalar();
            return result is null || result is DBNull ? 0L : Convert.ToInt64(result);
        });

        public DataTable ExecuteQuery(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            BindParameters(command, parameters);
            using var reader = command.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        public List<T> Query<T>(string sql, Func<IDataReader, T> map, IDictionary<string, object?>? parameters = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            BindParameters(command, parameters);
            using var reader = command.ExecuteReader();
            var list = new List<T>();
            while (reader.Read()) list.Add(map(reader));
            return list;
        }

        public void ExecuteInTransaction(Action<SqliteConnection, SqliteTransaction> work)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            try { work(connection, transaction); transaction.Commit(); }
            catch { transaction.Rollback(); throw; }
        }

        // ── Backup / restore ───────────────────────────────────────────

        public void Backup(string destinationPath)
        {
            SqliteConnection.ClearAllPools();
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.Copy(_dbPath, destinationPath, overwrite: true);
        }

        public void Restore(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Backup file not found.", sourcePath);
            SqliteConnection.ClearAllPools();
            File.Copy(sourcePath, _dbPath, overwrite: true);
        }

        // ── Schema + seed ──────────────────────────────────────────────

        public void InitializeDatabase(bool seedDemoData = true)
        {
            ExecuteNonQuery(SchemaSql);
            SeedSettings();
            if (seedDemoData) SeedInitialData();
        }

        /// <summary>Seeds default app settings if absent (idempotent, always runs).</summary>
        private void SeedSettings()
        {
            ExecuteNonQuery(
                @"INSERT OR IGNORE INTO Settings (Key, Value) VALUES
                  ('forecast.alpha','0.5'), ('forecast.z','1.5'), ('expiry.windowDays','30'),
                  ('session.idleMinutes','10');");
        }

        public string? GetSetting(string key) =>
            ExecuteScalar<string>("SELECT Value FROM Settings WHERE Key=@k;",
                new Dictionary<string, object?> { ["k"] = key });

        public void SetSetting(string key, string value) =>
            ExecuteNonQuery(
                "INSERT INTO Settings (Key,Value) VALUES (@k,@v) ON CONFLICT(Key) DO UPDATE SET Value=@v;",
                new Dictionary<string, object?> { ["k"] = key, ["v"] = value });

        private void SeedInitialData()
        {
            if (ExecuteScalar<long>("SELECT COUNT(*) FROM Users;") > 0) return;

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            void AddUser(string f, string l, string u, string p, string role, string? spec = null) =>
                ExecuteNonQuery(
                    @"INSERT INTO Users (FirstName, LastName, Username, PasswordHash, Role, Specialization, IsActive, CreatedAt)
                      VALUES (@f,@l,@u,@p,@r,@s,1,@c);",
                    new Dictionary<string, object?>
                    {
                        ["f"] = f, ["l"] = l, ["u"] = u,
                        ["p"] = BCrypt.Net.BCrypt.HashPassword(p),
                        ["r"] = role, ["s"] = spec, ["c"] = now
                    });

            // ── Users (id order: admin=1, general doctors=2,3, dentists=4,5, receptionists=6,7) ──
            AddUser("Sunil", "Karunaratne", "admin", "admin123", "Admin");
            AddUser("Nimal", "Perera", "gdoctor", "doc123", "Doctor", "General");
            AddUser("Dinesh", "Rajapaksa", "ndoctor", "demo123", "Doctor", "General");
            AddUser("Aisha", "Fernando", "dentist", "dent123", "Doctor", "Dental");
            AddUser("Hashini", "Madushani", "adentist", "demo123", "Doctor", "Dental");
            AddUser("Kamala", "Silva", "reception", "staff123", "Receptionist");
            AddUser("Ishara", "Senanayake", "reception2", "demo123", "Receptionist");
            AddUser("Ranil", "Gunasekara", "pharmacist", "pharm123", "Pharmacist");
            int gd1 = 2, gd2 = 3, dn1 = 4, dn2 = 5;

            // ── Inventory helpers ──
            long AddItem(string name, string stream, string cat, string unit, double price, int reorder) =>
                ExecuteInsertReturnId(
                    @"INSERT INTO InventoryItems (ItemName, Stream, Category, Unit, UnitPrice, ReorderLevel, Quantity, UpdatedAt)
                      VALUES (@n,@s,@cat,@u,@pr,@rl,0,@up);",
                    new Dictionary<string, object?>
                    { ["n"] = name, ["s"] = stream, ["cat"] = cat, ["u"] = unit, ["pr"] = price, ["rl"] = reorder, ["up"] = now });

            void AddBatch(long itemId, int qty, double price, string supplier, DateTime expiry)
            {
                ExecuteNonQuery(
                    @"INSERT INTO InventoryBatches (InventoryItemId, Quantity, PurchasePrice, Supplier, ReceivedDate, ExpiryDate)
                      VALUES (@i,@q,@p,@s,@rd,@ed);",
                    new Dictionary<string, object?>
                    { ["i"] = itemId, ["q"] = qty, ["p"] = price, ["s"] = supplier, ["rd"] = now, ["ed"] = expiry.ToString("yyyy-MM-dd") });
                ExecuteNonQuery("UPDATE InventoryItems SET Quantity = Quantity + @q WHERE Id=@i;",
                    new Dictionary<string, object?> { ["q"] = qty, ["i"] = itemId });
            }

            void AddConsumption(long itemId, int[] last6)
            {
                var d = DateTime.Now;
                for (int i = 0; i < last6.Length; i++)
                {
                    var m = d.AddMonths(-(last6.Length - i));
                    ExecuteNonQuery(
                        @"INSERT INTO ConsumptionRecords (InventoryItemId, Year, Month, QuantityConsumed) VALUES (@i,@y,@m,@q);",
                        new Dictionary<string, object?> { ["i"] = itemId, ["y"] = m.Year, ["m"] = m.Month, ["q"] = last6[i] });
                }
            }

            void AddPatient(string f, string l, string dob, string g, string type, string? phone, string? email,
                            string? addr, string? reg, string? fac, string? prog, int? yr,
                            string? ecName = null, string? ecPhone = null) =>
                ExecuteNonQuery(
                    @"INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PatientType, PhoneNumber, Email, Address, RegistrationNumber, Faculty, Program, YearOfStudy, EmergencyContactName, EmergencyContactPhone, CreatedAt)
                      VALUES (@f,@l,@d,@g,@t,@ph,@e,@a,@r,@fac,@prog,@yr,@ecn,@ecp,@c);",
                    new Dictionary<string, object?>
                    { ["f"] = f, ["l"] = l, ["d"] = dob, ["g"] = g, ["t"] = type, ["ph"] = phone, ["e"] = email,
                      ["a"] = addr, ["r"] = reg, ["fac"] = fac, ["prog"] = prog, ["yr"] = yr,
                      ["ecn"] = ecName, ["ecp"] = ecPhone, ["c"] = now });

            void AddAppointment(int pid, int did, DateTime when, string status, string stream, string notes = "") =>
                ExecuteNonQuery(
                    @"INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, DurationMinutes, Status, Stream, Notes, CreatedAt)
                      VALUES (@p,@d,@dt,30,@st,@str,@n,@c);",
                    new Dictionary<string, object?>
                    { ["p"] = pid, ["d"] = did, ["dt"] = when.ToString("yyyy-MM-dd HH:mm:ss"), ["st"] = status, ["str"] = stream, ["n"] = notes, ["c"] = now });

            long AddRecord(int pid, int did, string sym, string dg, string tr, string bp, int pulse, double temp, double weight, string notes) =>
                ExecuteInsertReturnId(
                    @"INSERT INTO MedicalRecords (PatientId, DoctorId, Symptoms, Diagnosis, Treatment, BloodPressure, Pulse, Temperature, Weight, Notes, RecordDate)
                      VALUES (@p,@d,@sy,@dg,@tr,@bp,@pu,@te,@we,@no,@rd);",
                    new Dictionary<string, object?>
                    { ["p"] = pid, ["d"] = did, ["sy"] = sym, ["dg"] = dg, ["tr"] = tr, ["bp"] = bp, ["pu"] = pulse,
                      ["te"] = temp, ["we"] = weight, ["no"] = notes, ["rd"] = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss") });

            void AddRx(long recId, string med, string dos, string ins, int qty, long? itemId, int times = 3, string timing = "After meals") =>
                ExecuteNonQuery(
                    @"INSERT INTO Prescriptions (MedicalRecordId, InventoryItemId, Medicine, Dosage, Instructions, Quantity, TimesPerDay, MealTiming, Dispensed, CreatedAt)
                      VALUES (@m,@i,@med,@dos,@ins,@q,@tpd,@mt,0,@c);",
                    new Dictionary<string, object?>
                    { ["m"] = recId, ["i"] = itemId, ["med"] = med, ["dos"] = dos, ["ins"] = ins, ["q"] = qty,
                      ["tpd"] = times, ["mt"] = timing, ["c"] = now });

            void AddDental(int pid, int did, string chart, string gum, string xray, string notes) =>
                ExecuteNonQuery(
                    @"INSERT INTO DentalRecords (PatientId, DoctorId, ToothChartJson, GumHealth, XrayNotes, Notes, RecordDate)
                      VALUES (@p,@d,@tc,@gh,@xn,@no,@rd);",
                    new Dictionary<string, object?>
                    { ["p"] = pid, ["d"] = did, ["tc"] = chart, ["gh"] = gum, ["xn"] = xray, ["no"] = notes, ["rd"] = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss") });

            void AddWaitlist(int pid, string stream, int prio) =>
                ExecuteNonQuery(
                    @"INSERT INTO WaitlistEntries (PatientId, Stream, Priority, Status, CreatedAt) VALUES (@p,@s,@pr,'Waiting',@c);",
                    new Dictionary<string, object?> { ["p"] = pid, ["s"] = stream, ["pr"] = prio, ["c"] = now });

            void AddSub(int did, int pid) =>
                ExecuteNonQuery(
                    @"INSERT INTO AvailabilitySubscriptions (DoctorId, PatientId, Notified, CreatedAt) VALUES (@d,@p,0,@c);",
                    new Dictionary<string, object?> { ["d"] = did, ["p"] = pid, ["c"] = now });

            // ── Inventory: General medication (some deliberately low / expiring for alerts) ──
            long para = AddItem("Paracetamol 500mg", "General", "Medication", "tablets", 2.50, 50);
            AddBatch(para, 120, 2.40, "State Pharmaceuticals Corp", DateTime.Now.AddMonths(8));
            AddBatch(para, 120, 2.55, "State Pharmaceuticals Corp", DateTime.Now.AddMonths(14));
            AddConsumption(para, new[] { 80, 95, 70, 110, 90, 100 });

            long amox = AddItem("Amoxicillin 250mg", "General", "Medication", "capsules", 8.00, 40);
            AddBatch(amox, 30, 7.80, "Hemas Pharmaceuticals", DateTime.Now.AddMonths(5));   // LOW
            AddConsumption(amox, new[] { 40, 35, 50, 45, 60, 55 });

            long cet = AddItem("Cetirizine 10mg", "General", "Medication", "tablets", 1.75, 30);
            AddBatch(cet, 120, 1.70, "State Pharmaceuticals Corp", DateTime.Now.AddMonths(10));
            AddConsumption(cet, new[] { 20, 25, 30, 22, 28, 26 });

            long met = AddItem("Metformin 500mg", "General", "Medication", "tablets", 3.20, 40);
            AddBatch(met, 90, 3.10, "Astron Limited", DateTime.Now.AddMonths(9));
            AddConsumption(met, new[] { 30, 28, 35, 40, 38, 42 });

            long ome = AddItem("Omeprazole 20mg", "General", "Medication", "capsules", 5.50, 30);
            AddBatch(ome, 80, 5.30, "Hemas Pharmaceuticals", DateTime.Now.AddMonths(7));
            AddConsumption(ome, new[] { 18, 22, 20, 25, 19, 24 });

            long ibu = AddItem("Ibuprofen 400mg", "General", "Medication", "tablets", 3.00, 40);
            AddBatch(ibu, 100, 2.90, "Astron Limited", DateTime.Now.AddMonths(11));
            AddConsumption(ibu, new[] { 25, 30, 28, 35, 32, 30 });

            long ors = AddItem("ORS Sachets", "General", "Consumable", "sachets", 15.00, 25);
            AddBatch(ors, 60, 14.00, "State Pharmaceuticals Corp", DateTime.Now.AddMonths(18));
            AddConsumption(ors, new[] { 12, 15, 10, 20, 18, 16 });

            long vitc = AddItem("Vitamin C 100mg", "General", "Medication", "tablets", 1.20, 30);
            AddBatch(vitc, 200, 1.10, "Hemas Pharmaceuticals", DateTime.Now.AddMonths(12));
            AddConsumption(vitc, new[] { 15, 12, 18, 20, 16, 14 });

            long salb = AddItem("Salbutamol Inhaler", "General", "Medication", "inhalers", 480.00, 10);
            AddBatch(salb, 6, 470.00, "Astron Limited", DateTime.Now.AddMonths(6));          // LOW
            AddConsumption(salb, new[] { 3, 4, 2, 5, 4, 3 });

            long dom = AddItem("Domperidone 10mg", "General", "Medication", "tablets", 2.10, 30);
            AddBatch(dom, 90, 2.00, "Astron Limited", DateTime.Now.AddMonths(9));
            AddConsumption(dom, new[] { 10, 12, 14, 11, 13, 15 });

            // ── Inventory: Dental supplies ──
            long anes = AddItem("Dental Anaesthetic Cartridge", "Dental", "Consumable", "cartridges", 45.00, 20);
            AddBatch(anes, 60, 44.00, "DSI Dental Lanka", DateTime.Now.AddMonths(12));
            AddConsumption(anes, new[] { 15, 18, 12, 20, 16, 19 });

            long resin = AddItem("Composite Filling Resin", "Dental", "Consumable", "syringes", 320.00, 10);
            AddBatch(resin, 15, 315.00, "DSI Dental Lanka", DateTime.Now.AddMonths(6));
            AddConsumption(resin, new[] { 4, 6, 5, 7, 5, 6 });

            long floss = AddItem("Dental Floss", "Dental", "Consumable", "packs", 60.00, 20);
            AddBatch(floss, 50, 55.00, "Smile Dental Supplies", DateTime.Now.AddMonths(24));
            AddConsumption(floss, new[] { 8, 10, 9, 12, 11, 10 });

            long fvar = AddItem("Fluoride Varnish", "Dental", "Consumable", "tubes", 250.00, 10);
            AddBatch(fvar, 8, 240.00, "Smile Dental Supplies", DateTime.Now.AddDays(20));      // LOW + EXPIRING SOON
            AddConsumption(fvar, new[] { 3, 4, 3, 5, 4, 4 });

            long mirror = AddItem("Disposable Mouth Mirror", "Dental", "Consumable", "pieces", 35.00, 30);
            AddBatch(mirror, 100, 32.00, "DSI Dental Lanka", DateTime.Now.AddMonths(30));
            AddConsumption(mirror, new[] { 20, 25, 22, 28, 24, 26 });

            long forceps = AddItem("Extraction Forceps", "Dental", "Equipment", "pieces", 1500.00, 3);
            AddBatch(forceps, 5, 1450.00, "DSI Dental Lanka", DateTime.Now.AddMonths(60));
            AddConsumption(forceps, new[] { 1, 1, 0, 1, 2, 1 });

            // ── Patients: internal students (ids 1-16) then external visitors (ids 17-24) ──
            var internals = new (string f, string l, string g, string dob, string reg, string fac, string prog, int yr)[]
            {
                ("Saman","Jayasinghe","Male","2003-04-12","AS2021123","Applied Sciences","BSc Computer Science",3),
                ("Dilani","Wickramasinghe","Female","2002-09-30","MS2020045","Management Studies & Commerce","BBA",4),
                ("Kasun","Bandara","Male","2001-01-15","EN2019210","Engineering","BEng Electronics",5),
                ("Nadeesha","Rathnayake","Female","2003-07-22","HS2021333","Humanities & Social Sciences","BA Sociology",3),
                ("Tharindu","Fernando","Male","2004-03-05","TE2022150","Technology","BICT",2),
                ("Sanduni","Senanayake","Female","2002-11-11","MD2020012","Medical Sciences","MBBS",4),
                ("Ruwan","Silva","Male","2000-06-18","AH2018077","Allied Health Sciences","BSc Nursing",5),
                ("Ishara","Madushani","Female","2003-02-27","AS2021200","Applied Sciences","BSc Physics",3),
                ("Chamara","Wijesekara","Male","2002-08-09","MS2020099","Management Studies & Commerce","B.Com",4),
                ("Hashini","Liyanage","Female","2004-05-14","HS2022018","Humanities & Social Sciences","BA English",2),
                ("Pradeep","Dissanayake","Male","2001-12-01","EN2019155","Engineering","BEng Civil",5),
                ("Amaya","Gunawardena","Female","2003-10-20","TE2021045","Technology","BICT",3),
                ("Lahiru","Karunaratne","Male","2002-04-03","AS2020310","Applied Sciences","BSc Chemistry",4),
                ("Nethmi","Jayawardena","Female","2004-01-25","MD2022005","Medical Sciences","MBBS",2),
                ("Gayan","Abeywardena","Male","2001-09-12","MS2019260","Management Studies & Commerce","BBA Finance",5),
                ("Tharushi","Peris","Female","2003-06-30","AH2021090","Allied Health Sciences","BSc Physiotherapy",3),
            };
            int phoneSeq = 1234560;
            int ecSeq = 7700000;
            foreach (var s in internals)
                AddPatient(s.f, s.l, s.dob, s.g, "Internal",
                    $"07{(phoneSeq % 9) + 1}{phoneSeq++:0000000}".Substring(0, 10),
                    $"{s.f.ToLower()}.{s.l.ToLower()}@stu.sjp.ac.lk", null, s.reg, s.fac, s.prog, s.yr,
                    $"{s.l} (Parent/Guardian)", $"07{(ecSeq % 9) + 1}{ecSeq++:0000000}".Substring(0, 10));

            var externals = new (string f, string l, string g, string dob, string addr)[]
            {
                ("Sunil","Abeysekara","Male","1975-03-19","No. 24, Temple Rd, Maharagama"),
                ("Kamala","Wickramasinghe","Female","1982-07-08","12/3, Lake View, Boralesgamuwa"),
                ("Ranjan","Peris","Male","1990-11-23","45 Galle Rd, Dehiwala"),
                ("Manel","Fonseka","Female","1968-05-30","8, Cotta Rd, Kotte"),
                ("Ajith","Kumara","Male","1988-02-14","No. 102, High Level Rd, Pannipitiya"),
                ("Shirani","De Silva","Female","1979-09-17","33, Station Rd, Moratuwa"),
                ("Bandula","Herath","Male","1972-12-05","56, Athurugiriya Rd, Homagama"),
                ("Priya","Nayanananda","Female","1995-08-21","19, Welivita Rd, Kaduwela"),
            };
            foreach (var e in externals)
                AddPatient(e.f, e.l, e.dob, e.g, "External",
                    $"07{(phoneSeq % 9) + 1}{phoneSeq++:0000000}".Substring(0, 10),
                    $"{e.f.ToLower()}.{e.l.ToLower()}@gmail.com", e.addr, null, null, null, null,
                    "Next of kin", $"07{(ecSeq % 9) + 1}{ecSeq++:0000000}".Substring(0, 10));

            // ── Appointments (today, recent past, near future) ──
            var today = DateTime.Today;
            AddAppointment(1, gd1, today.AddHours(9), "Scheduled", "General", "Routine check-up");
            AddAppointment(2, gd1, today.AddHours(9).AddMinutes(30), "CheckedIn", "General", "Fever");
            AddAppointment(3, gd2, today.AddHours(10), "Scheduled", "General", "Cough & cold");
            AddAppointment(4, dn1, today.AddHours(10).AddMinutes(30), "Scheduled", "Dental", "Toothache");
            AddAppointment(5, dn2, today.AddHours(11), "CheckedIn", "Dental", "Scaling");
            AddAppointment(6, gd1, today.AddHours(11).AddMinutes(30), "Scheduled", "General", "Follow-up");
            AddAppointment(7, gd1, today.AddDays(-1).AddHours(9), "Completed", "General", "Viral fever");
            AddAppointment(8, gd2, today.AddDays(-1).AddHours(10), "Completed", "General", "Allergic rhinitis");
            AddAppointment(9, dn1, today.AddDays(-2).AddHours(9).AddMinutes(30), "Completed", "Dental", "Scaling & polishing");
            AddAppointment(10, gd1, today.AddDays(-2).AddHours(11), "NoShow", "General", "");
            AddAppointment(11, gd2, today.AddDays(-3).AddHours(10), "Cancelled", "General", "");
            AddAppointment(12, dn2, today.AddDays(-3).AddHours(14), "Completed", "Dental", "Composite filling");
            AddAppointment(13, gd1, today.AddDays(1).AddHours(9), "Scheduled", "General", "Blood pressure review");
            AddAppointment(14, dn1, today.AddDays(1).AddHours(10), "Scheduled", "Dental", "Extraction");
            AddAppointment(15, gd2, today.AddDays(2).AddHours(9).AddMinutes(30), "Scheduled", "General", "");
            AddAppointment(16, dn2, today.AddDays(3).AddHours(11), "Scheduled", "Dental", "Check-up");
            AddAppointment(1, dn1, today.AddDays(2).AddHours(15), "Scheduled", "Dental", "Wisdom tooth review");
            AddAppointment(2, gd2, today.AddDays(4).AddHours(9), "Scheduled", "General", "");

            // ── Medical records + prescriptions (general medicine) ──
            long r1 = AddRecord(7, gd1, "Fever, body ache, headache for 2 days", "Viral fever", "Rest, hydration, antipyretics",
                "120/80", 88, 38.5, 68, "Advised to return if fever persists beyond 3 days.");
            AddRx(r1, "Paracetamol 500mg", "1 tablet TDS", "After meals for 3 days", 9, para);
            AddRx(r1, "ORS Sachets", "1 sachet", "Dissolve in 200ml water, after loose stools", 5, ors);

            long r2 = AddRecord(8, gd2, "Sneezing, runny nose, itchy eyes", "Allergic rhinitis", "Antihistamine",
                "118/76", 76, 36.8, 72, "Avoid dust exposure.");
            AddRx(r2, "Cetirizine 10mg", "1 tablet ON", "At night for 5 days", 5, cet);

            long r3 = AddRecord(1, gd1, "Burning epigastric pain", "Gastritis", "Proton pump inhibitor",
                "122/80", 80, 37.0, 65, "Avoid spicy food and NSAIDs.");
            AddRx(r3, "Omeprazole 20mg", "1 capsule OD", "Before breakfast for 14 days", 14, ome);

            long r4 = AddRecord(9, gd2, "Routine diabetic review", "Type 2 Diabetes Mellitus", "Continue oral hypoglycaemic",
                "130/84", 78, 36.7, 81, "HbA1c review in 3 months.");
            AddRx(r4, "Metformin 500mg", "1 tablet BD", "After meals", 30, met);

            // ── Dental records ──
            AddDental(9, dn1, "{\"36\":\"Cavity\",\"46\":\"Filled\",\"11\":\"Healthy\"}", "Mild gingivitis",
                "Bitewing shows early caries on 36", "Scaling and polishing done; advised fluoride application.");
            AddDental(12, dn2, "{\"26\":\"Filled\",\"27\":\"Cavity\"}", "Healthy",
                "No periapical pathology", "Composite filling placed on 26.");

            // ── Waitlist (waiting for a slot) ──
            AddWaitlist(17, "General", 1);
            AddWaitlist(18, "General", 3);
            AddWaitlist(19, "Dental", 2);

            // ── Availability subscriptions (notify on doctor return) ──
            AddSub(gd1, 20);
            AddSub(dn1, 21);
        }

        private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL, LastName TEXT NOT NULL,
    Username TEXT NOT NULL UNIQUE, PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL, Specialization TEXT,
    PhoneNumber TEXT, Email TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1, CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Patients (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL, LastName TEXT NOT NULL,
    DateOfBirth TEXT, Gender TEXT, PhoneNumber TEXT, Email TEXT, Address TEXT,
    PatientType TEXT NOT NULL DEFAULT 'External',
    RegistrationNumber TEXT UNIQUE, Faculty TEXT, Program TEXT, YearOfStudy INTEGER,
    EmergencyContactName TEXT, EmergencyContactPhone TEXT,
    IsArchived INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Appointments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL, DoctorId INTEGER NOT NULL,
    AppointmentDate TEXT NOT NULL, DurationMinutes INTEGER NOT NULL DEFAULT 30,
    Status TEXT NOT NULL DEFAULT 'Scheduled', Stream TEXT NOT NULL DEFAULT 'General',
    Notes TEXT, CreatedAt TEXT NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    FOREIGN KEY (DoctorId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS MedicalRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL, DoctorId INTEGER NOT NULL, AppointmentId INTEGER,
    Symptoms TEXT, Diagnosis TEXT, Treatment TEXT,
    BloodPressure TEXT, Pulse INTEGER, Temperature REAL, Weight REAL,
    Notes TEXT, RecordDate TEXT NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    FOREIGN KEY (DoctorId) REFERENCES Users(Id),
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id)
);

CREATE TABLE IF NOT EXISTS DentalRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL, DoctorId INTEGER NOT NULL, AppointmentId INTEGER,
    ToothChartJson TEXT, GumHealth TEXT, XrayNotes TEXT, Notes TEXT, RecordDate TEXT NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    FOREIGN KEY (DoctorId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS InventoryItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemName TEXT NOT NULL, Stream TEXT NOT NULL DEFAULT 'General', Category TEXT,
    Unit TEXT, UnitPrice REAL NOT NULL DEFAULT 0, ReorderLevel INTEGER NOT NULL DEFAULT 0,
    Quantity INTEGER NOT NULL DEFAULT 0, UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS InventoryBatches (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InventoryItemId INTEGER NOT NULL, Quantity INTEGER NOT NULL,
    PurchasePrice REAL NOT NULL DEFAULT 0, Supplier TEXT,
    ReceivedDate TEXT NOT NULL, ExpiryDate TEXT,
    FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id)
);

CREATE TABLE IF NOT EXISTS ConsumptionRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InventoryItemId INTEGER NOT NULL, Year INTEGER NOT NULL, Month INTEGER NOT NULL,
    QuantityConsumed INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id)
);

CREATE TABLE IF NOT EXISTS Prescriptions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MedicalRecordId INTEGER NOT NULL, InventoryItemId INTEGER,
    Medicine TEXT NOT NULL, Dosage TEXT, Instructions TEXT,
    Quantity INTEGER NOT NULL DEFAULT 1,
    TimesPerDay INTEGER NOT NULL DEFAULT 1, MealTiming TEXT,
    Dispensed INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL,
    FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id),
    FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id)
);

CREATE TABLE IF NOT EXISTS WaitlistEntries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL, Stream TEXT NOT NULL DEFAULT 'General',
    Priority INTEGER NOT NULL DEFAULT 5, Status TEXT NOT NULL DEFAULT 'Waiting',
    OfferedAt TEXT, CreatedAt TEXT NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS DoctorAvailability (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DoctorId INTEGER NOT NULL UNIQUE, Status TEXT NOT NULL DEFAULT 'OnSite',
    ShiftStart TEXT, ShiftEnd TEXT, LastChanged TEXT NOT NULL,
    FOREIGN KEY (DoctorId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS AvailabilitySubscriptions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DoctorId INTEGER NOT NULL, PatientId INTEGER NOT NULL,
    Notified INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL,
    FOREIGN KEY (DoctorId) REFERENCES Users(Id),
    FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

CREATE TABLE IF NOT EXISTS NotificationLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Channel TEXT NOT NULL, Recipient TEXT, Body TEXT,
    Status TEXT NOT NULL DEFAULT 'Sent', CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Reports (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReportType TEXT NOT NULL, Format TEXT NOT NULL, GeneratedBy INTEGER,
    GeneratedDate TEXT NOT NULL, FilePath TEXT, Parameters TEXT,
    FOREIGN KEY (GeneratedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS AuditLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER, Action TEXT NOT NULL, Entity TEXT, Timestamp TEXT NOT NULL, Detail TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IF NOT EXISTS IX_Appointments_DoctorId ON Appointments(DoctorId);
CREATE INDEX IF NOT EXISTS IX_Appointments_Date ON Appointments(AppointmentDate);
CREATE INDEX IF NOT EXISTS IX_Batches_ItemId ON InventoryBatches(InventoryItemId);
CREATE INDEX IF NOT EXISTS IX_Consumption_ItemId ON ConsumptionRecords(InventoryItemId);
CREATE INDEX IF NOT EXISTS IX_MedicalRecords_PatientId ON MedicalRecords(PatientId);
";
    }
}
