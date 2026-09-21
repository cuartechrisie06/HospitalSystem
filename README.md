# Hospital System (Clean Rebuild)

A clean, simplified Hospital Management System built with **C# Windows Forms** (.NET Framework 4.8).

## Features

- **Login** with demo accounts
- **Dashboard** with live statistics
- **Patients** – Register, search, update
- **Appointments** – Schedule, confirm, cancel (with basic double-booking protection)
- **Admissions** – Admit patient, assign bed, discharge, live bed board

## Demo Accounts

| Username | Password | Role            |
|----------|----------|-----------------|
| admin    | admin    | Administrator   |
| nurse    | nurse    | Nurse           |

## How to Run

1. Open `HospitalSystemClean.sln` in **Visual Studio 2019 / 2022**.
2. Make sure the **.NET desktop development** workload is installed.
3. Press **F5**.

## Technical Notes

- **Data**: In-memory only (resets when the app closes).
- **UI**: Built with standard Windows Forms controls so the forms are easier to understand and can be opened in the designer more reliably than the previous custom-drawn version.
- **Structure**:
  - `Models/` – Simple data classes
  - `Data/HospitalData.cs` – In-memory store + seed data
  - `Forms/` – LoginForm + DashboardForm
  - `Views/` – PatientsView, AppointmentsView, AdmissionsView

## Future Improvement Ideas

- Replace `HospitalData` with a real database (SQLite / SQL Server)
- Add interfaces for better SOLID compliance
- Add more validation and reports
