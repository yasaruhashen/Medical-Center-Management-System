# Medi Help J'Pura — University Medical Centre Management System (UMCMS)

A Windows desktop application (WPF, .NET 8) for managing a university medical centre:
patients, appointments, consultations, dental records, inventory with FEFO dispensing,
a pharmacy dispensary, reorder forecasting, reporting, and role-based access.

## Open in Visual Studio
1. Open **`UniversityMedicalCenter.sln`** in Visual Studio 2022 (v17.8+).
2. Set **`UMCMS.App`** as the startup project (right-click → *Set as Startup Project*).
3. Press **F5** to build and run. The SQLite database is created and seeded automatically
   on first launch under `src\UMCMS.App\bin\…\data\umcms.db`.

NuGet packages restore automatically (versions are pinned centrally in
`Directory.Packages.props`).

## Build / run / test from the CLI
```bash
dotnet build UniversityMedicalCenter.sln
dotnet run   --project src/UMCMS.App/UMCMS.App.csproj
dotnet test  tests/UMCMS.Tests/UMCMS.Tests.csproj
```

## Demo accounts
| Role | Username | Password |
|------|----------|----------|
| Administrator    | `admin`      | `admin123`  |
| General Physician| `gdoctor`    | `doc123`    |
| Dentist          | `dentist`    | `dent123`   |
| Receptionist     | `reception`  | `staff123`  |
| Pharmacist       | `pharmacist` | `pharm123`  |

(Additional demo users `ndoctor`, `adentist`, `reception2` use `demo123`.)

## Repository structure
```
UniversityMedicalCenter.sln          solution
Directory.Packages.props             central NuGet versions
README.md
├── src/
│   ├── UMCMS.Domain    — POCO entities, NavItem, forecasting math (no dependencies)
│   ├── UMCMS.Data      — DatabaseService (SQLite) + repositories
│   ├── UMCMS.Services  — Auth, Appointments, Inventory, Forecast, Reports,
│   │                     Notifications, Audit, Settings, Session, NavigationCatalog
│   └── UMCMS.App (WPF) — Views, Helpers, App shell (composition root in App.xaml.cs)
├── tests/
│   └── UMCMS.Tests     — xUnit tests (forecasting, FEFO dispensing, waitlist, auth)
└── docs/               — spec.md, plan.md
```

Layering: **View → App services (`App.*`) → Service → Repository / DatabaseService → SQLite.**
Tables are created by `DatabaseService.SchemaSql`; transactional access uses parameterised SQL.

## Tech stack
WPF · .NET 8 · SQLite (`Microsoft.Data.Sqlite`) · BCrypt.Net-Next · QuestPDF (PDF) ·
ClosedXML (Excel) · xUnit.

## Multi-user / LAN setup
Run several PCs against **one shared database** (no copies, no sync conflicts):
1. On the **host** PC (e.g. reception), share a folder and let the app create/seed the DB there
   (or copy an existing `umcms.db` into it), e.g. `C:\MediHelp` shared as `\\RECEPTION-PC\MediHelp`.
2. On **each** PC, open the app → **Settings → Shared Database (LAN)** and set the path to the
   shared file, e.g. `\\RECEPTION-PC\MediHelp\umcms.db`, then restart. (This writes a
   `umcms.config` file next to the app; you can also create that file manually.)
3. All clients now read/write the same database. Concurrent writes are serialised with a
   busy-timeout + automatic retry, and the live screens (Dispensary, Appointments) **auto-refresh**,
   so a prescription a doctor sends appears in the pharmacist's queue within a few seconds.

Leave the path blank to use this PC's local `data\umcms.db`.

## Notes
- The emergency feature is a **mobile-number lookup** (shows the patient's and parent/guardian's
  numbers to call) and records the contact in the audit log.
- The local database (`*.db`), `umcms.config`, and `bin/`, `obj/`, `.vs/` are git-ignored.
