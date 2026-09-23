# Hospital System (Clean Rebuild)

A Hospital Management System built with **C# Windows Forms** (.NET Framework 4.8), backed by a **MySQL** database.

## Features

- **Login** with accounts stored in the database
- **Dashboard** with live statistics pulled from MySQL
- **Patients** – Register, search, update
- **Doctors** – Doctor list and duty status
- **Appointments** – Schedule, confirm, cancel (with basic double-booking protection)
- **Admissions** – Admit patient, assign bed, discharge, live bed board
- **Activity log** – Every action is recorded in the `activity_log` table

## Demo Accounts

| Username | Password | Role            |
|----------|----------|-----------------|
| admin    | admin    | Administrator   |
| nurse    | nurse    | Nurse           |

## Requirements

- **Visual Studio 2019 / 2022** with the **.NET desktop development** workload
- **MySQL or MariaDB** running on `localhost:3306`
  (XAMPP works out of the box — its default `root` user has no password, which matches the connection string)

## Setup

### 1. Start MySQL

Open the **XAMPP Control Panel** and click **Start** next to **MySQL**.

> If you also have the `MySQL80` Windows service installed, leave it stopped — both use port 3306 and will conflict.

### 2. Create the database

Import `Data/schema.sql`. It creates the `hospital_system` database, all 9 tables, and the seed data
(2 users, 4 departments, 5 doctors, 12 beds, 3 patients).

Using the command line:

```
"C:\xampp\mysql\bin\mysql.exe" -u root < Data\schema.sql
```

Or in **phpMyAdmin** (`http://localhost/phpmyadmin`): Import → choose `Data/schema.sql` → Go.

### 3. Run the app

1. Open `HospitalSystemClean.sln` in Visual Studio.
2. Build once — Visual Studio restores the NuGet packages automatically.
   (If it doesn't: right-click the solution → **Restore NuGet Packages**.)
3. Press **F5**.

MySQL must be running *before* you launch the app, otherwise it will fail when it loads data.

## Connection String

Set in `Data/Db.cs`:

```
Server=localhost;Port=3306;Database=hospital_system;Uid=root;Pwd=;
```

Change it there if your MySQL uses a different port, user or password.

## Technical Notes

- **Data**: MySQL, accessed through `MySql.Data` (NuGet). Data persists between runs.
- **UI**: Standard Windows Forms controls, laid out in code.
- **Structure**:
  - `Models/` – Data classes (Patient, Doctor, Appointment, Admission, Bed, Alert, ActivityItem)
  - `Data/Db.cs` – Connection factory
  - `Data/HospitalData.cs` – All SQL queries live here
  - `Data/schema.sql` – Database schema + seed data
  - `Forms/` – LoginForm + DashboardForm
  - `Views/` – PatientsView, DoctorsView, AppointmentsView, AdmissionsView

## Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| `Unable to connect to any of the specified MySQL hosts` | MySQL is not running | Start MySQL in XAMPP |
| `Unknown database 'hospital_system'` | Schema not imported | Run step 2 above |
| `The type or namespace name 'MySql' could not be found` | NuGet packages not restored | Right-click solution → Restore NuGet Packages, then rebuild |
| Port 3306 already in use | Two MySQL servers running | Stop the `MySQL80` service, keep only XAMPP's |

## Future Improvement Ideas

- Move the connection string out of source and into `App.config`
- Hash the stored passwords instead of keeping them in plain text
- Add interfaces for better SOLID compliance
- Add more validation and printable reports
