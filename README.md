# Medical Center Management System

A standalone desktop application designed and developed for managing medical center operations. This project is built using C# and .NET 8.0 with WPF (Windows Presentation Foundation) following the MVVM architectural pattern.

## Introduction
The Medical Center Management System is a university group project designed to simplify day-to-day administrative tasks in a clinic. It provides an interface to register patients, manage doctor schedules, book appointments, and generate reports.

## Technologies Used
*   **Frontend**: WPF (Windows Presentation Foundation)
*   **Language**: C# (.NET 8.0)
*   **Database**: SQLite or SQL Server LocalDB
*   **Libraries**: QuestPDF (for PDFs), EPPlus (for Excel sheets), LiveCharts (for dashboard charts)

## Key Features
*   **User Login & Role Authentication**: Login access for Admins, Doctors, and Receptionists.
*   **Patient Management**: Register, search, update, and delete patient records.
*   **Doctor Management**: Manage doctor profiles and availability schedules.
*   **Appointment Booking**: Book appointments and prevent time collisions.
*   **Report Generation**: Export reports into PDF, Excel, and CSV formats.
*   **Dashboard**: View summary statistics and graphical charts.
*   **Data Backup & Restore**: Backup database files and restore them when needed.
*   **Data Import & Export**: Import/export patient data using Excel/CSV files.

## Project Folder Structure
Below is the detailed directory structure layout showing all files created for the project:

```text
📂 Project Root
 ┣ 📂 .vs/                       # Visual Studio environment configurations
 ┣ 📂 Project_                   # Main Application Solution Directory
 ┃ ┣ 📂 bin/                     # Compiled binaries (output files)
 ┃ ┣ 📂 data/                    # Local data storage files (e.g., databases/XMLs)
 ┃ ┣ 📂 obj/                     # Build artifacts and temporary objects
 ┃ ┣ 📂 resources(image,icons)/  # Graphical assets, application icons, and media
 ┃ ┃
 ┃ ┣ 📂 src/                     # Core Business Logic Layer
 ┃ ┃ ┣ 📂 Helpers/               # UI Utilities & Data Binding support
 ┃ ┃ ┃ ┣ 📜 ObservableObject.cs  # Base INotifyPropertyChanged implementation
 ┃ ┃ ┃ ┗ 📜 RelayCommand.cs      # ICommand implementation for MVVM bindings
 ┃ ┃ ┣ 📂 Models/                # Application Data Entities
 ┃ ┃ ┃ ┣ 📜 Appointment.cs       # Appointment business object
 ┃ ┃ ┃ ┣ 📜 Doctor.cs            # Doctor business object
 ┃ ┃ ┃ ┣ 📜 Patient.cs           # Patient business object
 ┃ ┃ ┃ ┗ 📜 User.cs              # Authentication and credentials entity
 ┃ ┃ ┣ 📂 Repositories/          # Data Access Layer & Abstractions
 ┃ ┃ ┃ ┣ 📜 IAppointmentRepository.cs
 ┃ ┃ ┃ ┣ 📜 AppointmentRepository.cs
 ┃ ┃ ┃ ┣ 📜 IDoctorRepository.cs
 ┃ ┃ ┃ ┣ 📜 DoctorRepository.cs
 ┃ ┃ ┃ ┣ 📜 IPatientRepository.cs
 ┃ ┃ ┃ ┣ 📜 PatientRepository.cs
 ┃ ┃ ┃ ┣ 📜 IUserRepository.cs
 ┃ ┃ ┃ ┗ 📜 UserRepository.cs
 ┃ ┃ ┣ 📂 Services/              # Core Application Services
 ┃ ┃ ┃ ┣ 📜 AuthService.cs       # Handles user login and authorization
 ┃ ┃ ┃ ┣ 📜 DatabaseService.cs   # General database connection & queries helper
 ┃ ┃ ┃ ┗ 📜 ReportService.cs     # Logic for generating clinical analytical reports
 ┃ ┃ ┗ 📂 ViewModels/            # UI Logic Layer (Intermediary)
 ┃ ┃   ┣ 📜 AppointmentsViewModel.cs
 ┃ ┃   ┣ 📜 DashboardViewModel.cs
 ┃ ┃   ┣ 📜 DoctorsViewModel.cs
 ┃ ┃   ┣ 📜 LoginViewModel.cs
 ┃ ┃   ┣ 📜 MainViewModel.cs
 ┃ ┃   ┣ 📜 PatientsViewModel.cs
 ┃ ┃   ┗ 📜 ReportsViewModel.cs
 ┃ ┃
 ┃ ┣ 📂 ui files(view)/          # XAML Presentation Layer (Views)
 ┃ ┃ ┣ 📜 AppointmentsView.xaml (.cs)
 ┃ ┃ ┣ 📜 DashboardView.xaml (.cs)
 ┃ ┃ ┣ 📜 DoctorsView.xaml (.cs)
 ┃ ┃ ┣ 📜 LoginView.xaml (.cs)
 ┃ ┃ ┣ 📜 PatientsView.xaml (.cs)
 ┃ ┃ ┗ 📜 ReportsView.xaml (.cs)
 ┃ ┃
 ┃ ┣ 📜 App.xaml (.cs)           # Application entry point and startup configurations
 ┃ ┣ 📜 AssemblyInfo.cs          # General assembly metadata attributes
 ┃ ┣ 📜 MainWindow.xaml (.cs)    # Shell window container hosting active views
 ┃ ┣ 📜 Project_.csproj          # MSBuild project file outlining dependencies
 ┃ ┗ 📜 Project_.csproj.user     # User-specific local IDE settings
 ┃
 ┣ 📜 .gitignore                 # Specifies intentionally untracked files to ignore
 ┣ 📜 Project_.sln               # Visual Studio Solution wrapper file
 ┗ 📜 README.md                  # Project overview and documentation