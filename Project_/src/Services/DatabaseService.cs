using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Project_.src.Services
{
    /// <summary>
    /// Central SQLite data-access helper for the whole application.
    /// All database access goes through this class — repositories call these
    /// helpers with parameterized SQL (never string-concatenated SQL).
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        /// <summary>
        /// Creates the helper. By default the database lives in <c>data/medihelp.db</c>
        /// under the application base directory.
        /// </summary>
        public DatabaseService(string? dbPath = null)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
                Directory.CreateDirectory(dataDir);
                dbPath = Path.Combine(dataDir, "medihelp.db");
            }

            _dbPath = dbPath;
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        /// <summary>Full path to the SQLite database file (used by Backup/Restore).</summary>
        public string DatabasePath => _dbPath;

        /// <summary>Opens a new connection with foreign keys enabled.</summary>
        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }
            return connection;
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

        // ──────────────────────────────────────────────────────────────
        //  Core query helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>Runs INSERT/UPDATE/DELETE; returns the number of rows affected.</summary>
        public int ExecuteNonQuery(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            BindParameters(command, parameters);
            return command.ExecuteNonQuery();
        }

        /// <summary>Runs a query that returns a single scalar value (e.g. COUNT, an id).</summary>
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

        /// <summary>Inserts a row and returns the auto-generated row id.</summary>
        public long ExecuteInsertReturnId(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql + "; SELECT last_insert_rowid();";
            BindParameters(command, parameters);
            var result = command.ExecuteScalar();
            return result is null || result is DBNull ? 0 : Convert.ToInt64(result);
        }

        public DataTable ExecuteQuery(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            BindParameters(command, parameters);
            using var reader = command.ExecuteReader();
            var table = new DataTable();
            
            // Add columns without constraints to avoid ADO.NET ConstraintExceptions 
            // (e.g. SQLite allows multiple NULLs in UNIQUE columns, but DataTable does not)
            for (int i = 0; i < reader.FieldCount; i++)
            {
                table.Columns.Add(reader.GetName(i), reader.GetFieldType(i) ?? typeof(object));
            }
            
            while (reader.Read())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.GetValue(i);
                }
                table.Rows.Add(row);
            }
            
            return table;
        }

        /// <summary>Runs a SELECT and maps each row to a typed object via <paramref name="map"/>.</summary>
        public List<T> Query<T>(string sql, Func<IDataReader, T> map, IDictionary<string, object?>? parameters = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            BindParameters(command, parameters);
            using var reader = command.ExecuteReader();
            var list = new List<T>();
            while (reader.Read())
            {
                list.Add(map(reader));
            }
            return list;
        }

        /// <summary>
        /// Runs several statements inside a single transaction. The connection and
        /// transaction are passed to <paramref name="work"/>; commits on success,
        /// rolls back on any exception.
        /// </summary>
        public void ExecuteInTransaction(Action<SqliteConnection, SqliteTransaction> work)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                work(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Schema creation + seed
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates all tables if they do not exist and seeds a default admin user.
        /// Safe to call on every startup (idempotent).
        /// </summary>
        public void InitializeDatabase()
        {
            ExecuteNonQuery(SchemaSql);
            SeedDefaultAdmin();
            MigrateSchema();
        }

        private void MigrateSchema()
        {
            try
            {
                // Add Status column to Prescriptions if it doesn't exist
                ExecuteNonQuery("ALTER TABLE Prescriptions ADD COLUMN Status TEXT NOT NULL DEFAULT 'Pending';");
            }
            catch
            {
                // Column likely already exists
            }
        }

        private void SeedDefaultAdmin()
        {
            var count = ExecuteScalar<long>("SELECT COUNT(*) FROM Users;");
            if (count > 0) return;

            var hash = BCrypt.Net.BCrypt.HashPassword("admin123");
            ExecuteNonQuery(
                @"INSERT INTO Users (FirstName, LastName, Username, PasswordHash, Role, IsActive, CreatedAt)
                  VALUES (@FirstName, @LastName, @Username, @PasswordHash, @Role, 1, @CreatedAt);",
                new Dictionary<string, object?>
                {
                    ["FirstName"] = "System",
                    ["LastName"] = "Administrator",
                    ["Username"] = "admin",
                    ["PasswordHash"] = hash,
                    ["Role"] = "Admin",
                    ["CreatedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
        }

        // ──────────────────────────────────────────────────────────────
        //  Backup / restore (SQLite is a single file)
        // ──────────────────────────────────────────────────────────────

        /// <summary>Copies the database file to <paramref name="destinationPath"/>.</summary>
        public void Backup(string destinationPath)
        {
            SqliteConnection.ClearAllPools();
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.Copy(_dbPath, destinationPath, overwrite: true);
        }

        /// <summary>Replaces the live database with the file at <paramref name="sourcePath"/>.</summary>
        public void Restore(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Backup file not found.", sourcePath);

            SqliteConnection.ClearAllPools();
            File.Copy(sourcePath, _dbPath, overwrite: true);
        }

        // ──────────────────────────────────────────────────────────────
        //  Schema definition
        // ──────────────────────────────────────────────────────────────

        private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS Users (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName      TEXT    NOT NULL,
    LastName       TEXT    NOT NULL,
    Username       TEXT    NOT NULL UNIQUE,
    PasswordHash   TEXT    NOT NULL,
    Role           TEXT    NOT NULL,
    PhoneNumber    TEXT,
    Email          TEXT,
    Specialization TEXT,
    Department     TEXT,
    AdminLevel     TEXT,
    JobTitle       TEXT,
    Shift          TEXT,
    IsActive       INTEGER NOT NULL DEFAULT 1,
    CreatedAt      TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Patients (
    Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName          TEXT    NOT NULL,
    LastName           TEXT    NOT NULL,
    DateOfBirth        TEXT,
    Gender             TEXT,
    PhoneNumber        TEXT,
    Email              TEXT,
    Address            TEXT,
    PatientType        TEXT    NOT NULL DEFAULT 'External',
    RegistrationNumber TEXT    UNIQUE,
    Faculty            TEXT,
    Program            TEXT,
    YearOfStudy        INTEGER,
    IsArchived         INTEGER NOT NULL DEFAULT 0,
    CreatedAt          TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Appointments (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId       INTEGER NOT NULL,
    DoctorId        INTEGER NOT NULL,
    AppointmentDate TEXT    NOT NULL,
    DurationMinutes INTEGER NOT NULL DEFAULT 30,
    Status          TEXT    NOT NULL DEFAULT 'Scheduled',
    Notes           TEXT,
    CreatedAt       TEXT    NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    FOREIGN KEY (DoctorId)  REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS MedicalRecords (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId     INTEGER NOT NULL,
    DoctorId      INTEGER NOT NULL,
    AppointmentId INTEGER,
    Symptoms      TEXT,
    Diagnosis     TEXT,
    Treatment     TEXT,
    BloodPressure TEXT,
    Pulse         INTEGER,
    Temperature   REAL,
    Weight        REAL,
    Notes         TEXT,
    RecordDate    TEXT    NOT NULL,
    FOREIGN KEY (PatientId)     REFERENCES Patients(Id),
    FOREIGN KEY (DoctorId)      REFERENCES Users(Id),
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id)
);

CREATE TABLE IF NOT EXISTS Inventory (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemName     TEXT    NOT NULL,
    Category     TEXT,
    Quantity     INTEGER NOT NULL DEFAULT 0,
    Unit         TEXT,
    UnitPrice    REAL    NOT NULL DEFAULT 0,
    ReorderLevel INTEGER NOT NULL DEFAULT 0,
    ExpiryDate   TEXT,
    Supplier     TEXT,
    UpdatedAt    TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Prescriptions (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    MedicalRecordId INTEGER NOT NULL,
    InventoryItemId INTEGER,
    Medicine        TEXT    NOT NULL,
    Dosage          TEXT,
    Instructions    TEXT,
    Quantity        INTEGER NOT NULL DEFAULT 1,
    Status          TEXT    NOT NULL DEFAULT 'Pending',
    CreatedAt       TEXT    NOT NULL,
    FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id),
    FOREIGN KEY (InventoryItemId) REFERENCES Inventory(Id)
);

CREATE TABLE IF NOT EXISTS Reports (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    ReportType    TEXT    NOT NULL,
    Format        TEXT    NOT NULL,
    GeneratedBy   INTEGER,
    GeneratedDate TEXT    NOT NULL,
    FilePath      TEXT,
    Parameters    TEXT,
    FOREIGN KEY (GeneratedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS AuditLog (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId    INTEGER,
    Action    TEXT    NOT NULL,
    Entity    TEXT,
    Timestamp TEXT    NOT NULL,
    Detail    TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IF NOT EXISTS IX_Appointments_DoctorId   ON Appointments(DoctorId);
CREATE INDEX IF NOT EXISTS IX_Appointments_PatientId  ON Appointments(PatientId);
CREATE INDEX IF NOT EXISTS IX_Appointments_Date        ON Appointments(AppointmentDate);
CREATE INDEX IF NOT EXISTS IX_MedicalRecords_PatientId ON MedicalRecords(PatientId);
CREATE INDEX IF NOT EXISTS IX_Prescriptions_RecordId   ON Prescriptions(MedicalRecordId);
";
    }
}
