# Hospital System (Clean Rebuild)

A Hospital Management System built with **C# Windows Forms** (.NET Framework 4.8), backed by a **MySQL** database.

## Features

- **Login** with accounts stored in the database. Passwords are stored as salted PBKDF2 hashes (plain-text passwords from
  an older database are converted on first run), and 5 failed sign-ins lock the account for 5 minutes on every workstation.
- **Dashboard** – appointments (today and next 7 days), bed occupancy (overall and per ward), pending admissions (the waiting list, with how long each patient has waited), admitted patients, doctors on duty, and alerts.
- **Alerts** – raised and cleared automatically by the other modules; there is no manual "new alert". Staff can *acknowledge* an alert, which hides it until the condition clears (it comes back if the situation gets worse). Current rules:
  - Admissions: high bed occupancy (80%+) / no beds left; ICU down to 1 or 0 beds; patients waiting for a bed (high priority after 4 hours, or as soon as a bed is free)
  - Doctors: a department with no doctor on duty
  - Appointments: an off-duty or deactivated doctor with patients still booked today; past appointments not marked completed or cancelled
  - Billing: discharged patients with an unpaid balance; HMO coverage uncollected after 30 days
- **Patients** – Register, search, update. The birthdate is stored (so ages stay correct), and deactivating a record
  asks for an optional reason; who deactivated or reactivated it, and when, is kept and shown in Patient Details.
- **Doctors** – Doctor records (license number, credentials, specialization, contact) and duty status, with search; deactivate a doctor with a required reason (kept on the record) and reactivate later. "Show inactive" reveals deactivated doctors; inactive doctors are hidden from appointment/admission scheduling.
- **Appointments** – Schedule from a searchable, click-to-select table of active patients (captures patient, department, doctor, reason, date & time), confirm, reschedule, mark completed, cancel (with basic double-booking protection). Deactivated patients and doctors cannot be scheduled — they are excluded from the pickers and re-checked at schedule time.
- **Admissions** – Admit an active, not-currently-admitted patient (doctor and diagnosis required), assign an available bed, discharge. Admissions can't be cancelled; a patient still on the waiting list can be removed from it. Live bed board shows every room/bed and ward type with a per-ward availability summary (e.g. `ICU 0/2`), colour-coded occupancy, and a warning when occupancy is high or no bed is free (when the hospital is full, patients go on a **waiting list** as Pending admissions and are given a bed with **Assign Bed to Selected** once one frees up; their bill opens at that point). Admission status changes feed billing: admitting opens a linked bill from the pre-set admission charge schedule (admit → billing trigger), discharging finalizes all per-day charges to the actual length of stay.
- **Billing** – Bills per patient (optionally linked to an admission or appointment; an admission's bill is created automatically on admit), line items by category, partial/full payments, auto-calculated balance and status (Unpaid / PartiallyPaid / Paid). Bills can't be cancelled; bills cancelled before this change still show as Cancelled
  - **Payments** – Cash (amount tendered and change), Card (card type, last 4 digits only, approval code), HMO (provider, LOA no.), plus e-wallet and bank transfer. Patient payments reduce the patient balance; HMO payments settle the HMO's approved coverage. A bill is Paid once both are settled.
  - **Balances** – each bill shows the patient balance, HMO outstanding and total outstanding, plus what the patient owes across all their bills. The bills list shows totals outstanding from patients and from HMOs, and a **Show: Outstanding** filter.
  - **Billing breakdown** – itemized charges grouped by category (room & board, professional fees, procedures, medicines, laboratory & diagnostics, other), then discounts, VAT and HMO coverage down to the amount due and balance. Adjusted per bill on the **Discounts / Tax / HMO** tab:
    - General discount (percent or peso amount, reason required)
    - Senior citizen / PWD discount: 20%, VAT-exempt, OSCA/PWD ID required. Not combined with the general discount; the higher one applies. Patients aged 60+ are flagged.
    - VAT (12% by default on new bills; existing bills keep 0% so their totals don't change)
    - HMO coverage (provider, LOA / approval no., approved amount), deducted from what the patient pays
    - **Print Statement...** produces a statement of account with the full breakdown and payments.
  - **Admission charge schedule** – the pre-set services billed on admission (admission fee, room rate per ward, nursing care, ICU monitoring, lab panel…), each one-time or per day and for all wards or one ward. Managed from **Billing → Admission Charge Schedule...**; price changes apply to bills generated afterwards.
- **Reports** – printable **daily census** (admissions, discharges, patients in house, occupancy, waiting list,
  appointments per day, plus current occupancy by ward) and, for administrators, **revenue by day** (bills issued,
  amount billed, payments collected by Cash / Card / HMO / other, and current outstanding balances).
- **Backup & Restore** (administrators only) – saves the whole database to one `.sql` file and loads it back.
  A restore replaces all data in a single transaction: if anything goes wrong, nothing changes.
- **Activity Log** (administrators only) – every action in every module is recorded with the user who did it (sign-ins and failed sign-ins, patients, doctors, appointments, admissions, billing, printed statements, alerts raised/cleared by "System"). Filter by date, module, user or text and export to CSV.

## Demo Accounts

| Username | Password | Role            |
|----------|----------|-----------------|
| admin    | admin    | Administrator   |
| nurse    | nurse    | Nurse           |

### What each role can do

The signed-in user is shown at the top of the sidebar (amber badge for administrators, teal for nurses),
and the dashboard opens with a banner naming the view.

| | Administrator | Nurse |
|---|---|---|
| Dashboard | everything, plus an **Outstanding** balances card and billing alerts | clinical cards and alerts only |
| Patients | register, edit, deactivate / reactivate | register, edit |
| Doctors | add, edit, change duty, deactivate / reactivate | view only |
| Appointments, Admissions (incl. waiting list) | full | full |
| Billing | create bills, add / remove charges, discounts / VAT / HMO, record payments, charge schedule | view bills and breakdown, print statements, add charges |
| Reports | daily census and revenue | daily census |
| Activity Log, Backup & Restore | yes | hidden |

Permissions are defined once in `Data/Permissions.cs`. The screens hide what a role can't do, and the services
refuse it as well (through `IPermissionGuard`); every refused attempt is written to the activity log as "Access Denied".

## Requirements

- **Visual Studio 2019 / 2022** with the **.NET desktop development** workload
- **MySQL or MariaDB** running on `localhost:3306`
  (XAMPP works out of the box — its default `root` user has no password, which matches the connection string)

## Setup

### 1. Clone the repository

```
git clone https://github.com/cuartechrisie06/HospitalSystem.git
```

### 2. Start MySQL

Open the **XAMPP Control Panel** and click **Start** next to **MySQL**.

> If you also have the `MySQL80` Windows service installed, leave it stopped — both use port 3306 and will conflict.

### 3. Create the database

Import `Data/schema.sql`. It creates the `hospital_system` database, all 12 tables, and the seed data
(2 users, 4 departments, 5 doctors, 16 beds, 3 patients).

Rooms: 2 private rooms (1 bed each), 2 semi-private rooms (2 beds), 2 wards (3 beds) and 2 ICU rooms (2 beds).
A database created before this layout is moved onto it automatically when the app starts with no patient in a bed
("General Ward" becomes "Ward", and a semi-private room charge is added to the charge schedule).

Using the command line:

```
"C:\xampp\mysql\bin\mysql.exe" -u root < Data\schema.sql
```

Or in **phpMyAdmin** (`http://localhost/phpmyadmin`): Import → choose `Data/schema.sql` → Go.

> Already imported the database before billing was added? No need to re-import:
> the app creates the `bills`, `bill_items`, `payments` and `charge_schedules` tables on startup if they are missing
> (seeding the default admission charges), and adds any new columns to existing tables.

### 4. Run the app

1. Open `HospitalSystemClean.sln` in Visual Studio.
2. Build once — Visual Studio restores the NuGet packages automatically into a new `packages/` folder
   (that folder is gitignored, so it won't exist right after cloning — that's expected, not a missing-files problem).
   If it doesn't restore on its own:
   - Right-click the solution → **Restore NuGet Packages**, or
   - Make sure **Tools → Options → NuGet Package Manager → Allow NuGet to download missing packages** is checked, or
   - Run `nuget restore packages.config -PackagesDirectory packages` from a command prompt in the project folder
     (grab `nuget.exe` from [nuget.org/downloads](https://www.nuget.org/downloads) if you don't have it).
3. Press **F5**.

MySQL must be running *before* you launch the app; otherwise it shows a "Database unavailable" message and closes.

## Editing screens in the Designer

Every form and view opens in the Visual Studio Designer with all of its controls:
`LoginForm`, `DashboardForm` (sidebar), `PatientsView`, `DoctorsView`, `AppointmentsView`, `AdmissionsView`,
`BillingView`, `ReportsView`, `BackupView`, `ActivityLogView` and `ChargeScheduleForm`.
`BillingView` is split into partial files: `BillingView.Designer.cs` (the layout, edited by the Designer),
`BillingView.cs` (bills, items, adjustments) and `BillingView.Payments.cs` (recording payments).

Each screen has two constructors: a parameterless one that only calls `InitializeComponent()` (the Designer's),
and one that takes the services the screen uses and loads its data (the app's). The Designer never runs a class's
constructor or helper methods. It only reads `InitializeComponent()`, so to keep a control visible there:

- declare it as a field and create it inside `InitializeComponent()` with plain statements
  (`this.btn = new System.Windows.Forms.Button();`, `this.btn.Text = "...";`, `this.panel.Controls.Add(this.btn);`)
- wire events to named methods: `this.btn.Click += new System.EventHandler(this.Btn_Click);`
- don't use local variables, lambdas, `new X { ... }` initializers or calls to your own helper methods in there

Only data-driven content is built at runtime: grid rows, combo box items that come from enums or the database,
the dashboard's alert rows, the ward bars and charts, and what each role is allowed to see.

## Connection String

Set in `App.config` (connection string `HospitalSystem`); the default matches a fresh XAMPP install:

```
Server=localhost;Port=3306;Database=hospital_system;Uid=root;Pwd=;
```

Change it there if your MySQL uses a different port, user or password. After building, the same setting is in
`HospitalSystem.exe.config` next to the program, so it can be changed on each workstation without rebuilding.

### Running without root (recommended outside a dev machine)

`Data/create_app_user.sql` creates a `hospital_app` MySQL account that can only use the `hospital_system` database.
Edit the password in the script, run it once as root, then put that user and password in `App.config`:

```
"C:\xampp\mysql\bin\mysql.exe" -u root < Data\create_app_user.sql
```

## Several workstations

The app keeps a copy of the data in memory and re-reads the database every time a screen is opened from the sidebar,
so changes made on other PCs show up as soon as you switch screens. The checks that matter are made in the database
itself, so two PCs can't put two patients in the same bed, admit the same patient twice, or book a doctor twice
within 30 minutes, even if one of them hasn't refreshed yet.

## Tests

`HospitalSystem.Tests` (MSTest) checks the business rules: password hashing and lockout, permissions, patient
validation and deactivation, appointment double-booking, admissions and bed handling, billing totals, discounts and
VAT, reports and alerts. The tests run the real services, wired the same way as the app, over in-memory fake
repositories, so they need no database. Run them from **Test → Run All Tests** in Visual Studio.

## Technical Notes

- **Data**: MySQL, accessed through `MySql.Data` (NuGet). Data persists between runs.
- **UI**: Standard Windows Forms controls, laid out in code.
- **Structure** (screens → services → repositories → MySQL; each layer only talks to the interfaces of the one below):
  - `Models/` – Data classes (Patient, Doctor, Appointment, Admission, Bed, Bill, BillItem, Payment, Alert, ActivityItem)
  - `Data/Db.cs` – `IDbConnectionFactory` and the MySQL connection factory
  - `Data/SchemaMigrator.cs` – Upgrades an older database in place at startup
  - `Data/Repositories/` – All SQL lives here, one repository per table group (`IPatientRepository`, `IBillRepository`, …);
    `IRepositorySet` bundles them so the app can be built over MySQL or over test fakes
  - `Data/schema.sql` – Database schema + seed data
  - `Services/` – Business rules, activity logging and permission checks, one service per module
    (`IPatientService`, `IAdmissionService`, `IBillingService`, …), plus `AlertMonitor` for the automatic alerts
  - `Services/AppServices.cs` – Composition root: the only place that creates the concrete classes and wires them up
  - `Forms/` – LoginForm + DashboardForm
  - `Views/` – PatientsView, DoctorsView, AppointmentsView, AdmissionsView, BillingView, ReportsView, BackupView, ActivityLogView
  - `Models/Statuses.cs` – the status values (`AppointmentStatus.Confirmed`, `AdmissionStatus.Active`, …) used everywhere
  - `HospitalSystem.Tests/` – unit tests

### How it follows SOLID

- **Single responsibility**: SQL is in the repositories, rules and logging in the services, layout and input in
  the screens, schema upgrades in `SchemaMigrator`, and the role policy in `Permissions`.
- **Open/closed**: modules react to each other through events instead of calling each other. Discharging raises
  `IAdmissionService.Discharged` (billing updates the room days); every logged action raises
  `IActivityLogger.Logged` (`AlertMonitor` re-checks the alerts). New reactions subscribe in `AppServices`
  without editing the module that raises the event.
- **Liskov substitution**: services depend only on repository interfaces, so any implementation (another database,
  an in-memory one for tests) can be swapped in `AppServices` and the services behave the same.
- **Interface segregation**: screens receive only the services they use, and read and write sides are split where
  only one place writes: `IBedService` / `IBedOccupancy`, `IAlertService` / `IAlertWriter`,
  `IActivityLogger` / `IActivityLogReader`, and `IAuthService` / `IUserSession` / `IUserDirectory`.
- **Dependency inversion**: forms, views and services get their dependencies through their constructors as
  interfaces. Nothing reaches for a global; `Program.cs` builds everything once with `AppServices.Create(...)`.

To add a feature: put the SQL in a repository, the rule in a service, and give the screen the service through
its constructor (passing it from `DashboardForm`, which hands each module its share of `AppServices`).

## Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| `Unable to connect to any of the specified MySQL hosts` | MySQL is not running | Start MySQL in XAMPP |
| `Unknown database 'hospital_system'` | Schema not imported | Run step 3 above |
| `The type or namespace name 'MySql' could not be found` | NuGet packages not restored | Right-click solution → Restore NuGet Packages, then rebuild |
| Port 3306 already in use | Two MySQL servers running | Stop the `MySQL80` service, keep only XAMPP's |
| "This account is locked" | 5 failed sign-ins | Wait 5 minutes, or run `UPDATE users SET locked_until=NULL WHERE username='...'` |

## Future Improvement Ideas

- More reports (e.g. doctor workload, length of stay)
