<div align="center">

# Medi Help J'Pura

### University Medical Centre Management System (UMCMS)

A Windows desktop application for running a university medical centre end to end —
patient care, appointments, pharmacy, inventory, and reporting in a single role-aware workspace.

[![Platform](https://img.shields.io/badge/platform-Windows-0078D6)](#requirements)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-WPF-2C3E50)](#tech-stack)
[![Database](https://img.shields.io/badge/database-SQLite-003B57)](#tech-stack)

</div>

---

## Overview

**Medi Help J'Pura** is a clinical management system for the University of Sri
Jayewardenepura medical centre. It brings the full patient journey — from
reception check-in to consultation, prescription, and dispensing — into one
desktop application with **role-based access**, a **shared LAN database**, and
**FEFO-correct pharmacy stock control**.

Built as a layered .NET 8 / WPF solution, the system is designed to run on
several reception, consultation, and pharmacy PCs against a **single shared
database**, with live screens that refresh automatically as records change.

---

## Key Features

| Area | What it does |
|------|--------------|
| **Patients** | Register, search, and manage patient and guardian records |
| **Appointments** | Booking with collision checks, availability schedules, and a **waitlist** with auto-fill |
| **Consultations** | General and dental consultation records tied to each patient's history |
| **Pharmacy** | Prescription entry, a live **pharmacist dispensing queue**, and **FEFO** (first-expiry-first-out) batch dispensing |
| **Inventory** | Batch-level stock tracking, expiry awareness, and **reorder forecasting** |
| **Reporting** | Exportable reports and analytics in **PDF** (QuestPDF) and **Excel** (ClosedXML) |
| **Dashboard** | At-a-glance summary of the centre's daily activity |
| **Security** | BCrypt-hashed credentials, role-based navigation, password changes, and a full **audit log** |
| **Emergency** | Mobile-number lookup for a patient and their guardian, logged to the audit trail |
| **Administration** | User management, settings, and database **backup / restore** |

---

## Roles & Demo Accounts

The application is role-aware: each user sees only the screens relevant to their job.

| Role | Username | Password |
|------|----------|----------|
| Administrator     | `admin`      | `admin123`  |
| General Physician | `gdoctor`    | `doc123`    |
| Dentist           | `dentist`    | `dent123`   |
| Receptionist      | `reception`  | `staff123`  |
| Pharmacist        | `pharmacist` | `pharm123`  |

> Additional demo users `ndoctor`, `adentist`, and `reception2` all use `demo123`.

---

## Getting Started

### Requirements

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (v17.8+) — optional, for IDE workflow

### Run in Visual Studio

1. Open **`UniversityMedicalCenter.sln`**.
2. Set **`UMCMS.App`** as the startup project *(right-click → Set as Startup Project)*.
3. Press **F5**.

The SQLite database is **created and seeded automatically** on first launch at
`src\UMCMS.App\bin\…\data\umcms.db`. NuGet packages restore automatically — versions
are pinned centrally in `Directory.Packages.props`.

### Run from the CLI

```bash
# Build the whole solution
dotnet build UniversityMedicalCenter.sln

# Launch the application
dotnet run --project src/UMCMS.App/UMCMS.App.csproj

# Run the test suite
dotnet test tests/UMCMS.Tests/UMCMS.Tests.csproj
```

---

## Multi-User / LAN Setup

Run several PCs against **one shared database** — no copies, no sync conflicts.

1. **Host PC** (e.g. reception): share a folder and let the app create/seed the
   database there, or drop an existing `umcms.db` into it
   — e.g. `C:\MediHelp` shared as `\\RECEPTION-PC\MediHelp`.
2. **Each client PC**: open **Settings → Shared Database (LAN)** and point it at
   the shared file (e.g. `\\RECEPTION-PC\MediHelp\umcms.db`), then restart.
3. All clients now read and write the same database. Concurrent writes are
   serialised with a busy-timeout and automatic retry, and live screens
   (Dispensary, Appointments) **auto-refresh** — so a prescription a doctor sends
   appears in the pharmacist's queue within seconds.

Leave the path blank to use this PC's local `data\umcms.db`.

---

## Architecture

The solution follows a clean, one-way dependency flow:

```
View  →  App services (App.*)  →  Service  →  Repository / DatabaseService  →  SQLite
```

| Project | Responsibility |
|---------|----------------|
| **UMCMS.Domain**   | POCO entities, navigation model, and forecasting math — no dependencies |
| **UMCMS.Data**     | `DatabaseService` (SQLite) and repositories using parameterised SQL |
| **UMCMS.Services** | Auth, Appointments, Inventory, Forecast, Reports, Notifications, Audit, Settings, Session, Navigation |
| **UMCMS.App**      | WPF views, helpers, and the application shell (composition root in `App.xaml.cs`) |

Database tables are created by `DatabaseService.SchemaSql`; all access is
transactional and parameterised.

### Project Structure

```text
UniversityMedicalCenter.sln          solution
Directory.Packages.props             central NuGet versions
├── UMCMS V1/                         Standalone Application
├── Diagrams/
├── src/
│   ├── UMCMS.Domain/                 entities, NavItem, forecasting
│   │   ├── Entities/                 Patient, Appointment, Prescription, InventoryItem …
│   │   └── Forecasting/              reorder forecast logic
│   ├── UMCMS.Data/                   DatabaseService + repositories
│   │   └── Repositories/             Patient, Appointment, Inventory, Waitlist …
│   ├── UMCMS.Services/               application services
│   └── UMCMS.App/                    WPF presentation layer
│       ├── Views/                    Dashboard, Appointments, Consultations,
│       │                             Dental, Inventory, Forecast, Reports,
│       │                             PharmacistQueue, Settings, AuditLog …
│       ├── Helpers/                  MVVM utilities
│       └── resources/                icons & assets
│                                     waitlist auto-fill, auth
```

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime    | .NET 8 |
| UI         | WPF (Windows Presentation Foundation) |
| Database   | SQLite via `Microsoft.Data.Sqlite` |
| Security   | `BCrypt.Net-Next` password hashing |
| PDF export | `QuestPDF` |
| Excel export | `ClosedXML` |

---

## Testing

The test suite covers the system's most safety-critical logic:

- **Forecasting** — reorder-point calculations
- **Inventory dispensing** — FEFO batch selection
- **Waitlist** — auto-fill behaviour when slots free up
- **Authentication** — credential verification and roles

```bash
dotnet test tests/UMCMS.Tests/UMCMS.Tests.csproj
```

---

## Notes

- The **emergency** feature is a mobile-number lookup — it surfaces the patient's
  and their guardian's contact numbers to call, and records the contact in the
  audit log.
- The local database (`*.db`), `umcms.config`, and the `bin/`, `obj/`, and `.vs/`
  directories are git-ignored.

---

<div align="center">

Developed as a group project for the University of Sri Jayewardenepura.

</div>
