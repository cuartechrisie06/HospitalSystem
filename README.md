# Hospital System (Clean Rebuild)

A Hospital Management System built with **C# Windows Forms** (.NET Framework 4.8), backed by a **MySQL** database.

## Features

- **Login** with accounts stored in the database
- **Dashboard** – appointments (today and next 7 days), bed occupancy (overall and per ward), pending admissions (the waiting list, with how long each patient has waited), admitted patients, doctors on duty, and alerts.
- **Alerts** – raised and cleared automatically by the other modules; there is no manual "new alert". Staff can *acknowledge* an alert, which hides it until the condition clears (it comes back if the situation gets worse). Current rules:
  - Admissions: high bed occupancy (80%+) / no beds left; ICU down to 1 or 0 beds; patients waiting for a bed (high priority after 4 hours, or as soon as a bed is free)
  - Doctors: a department with no doctor on duty
  - Appointments: an off-duty or deactivated doctor with patients still booked today; past appointments not marked completed or cancelled
  - Billing: discharged patients with an unpaid balance; HMO coverage uncollected after 30 days
- **Patients** – Register, search, update
- **Doctors** – Doctor records (license number, credentials, specialization, contact) and duty status, with search; deactivate a doctor with a required reason (kept on the record) and reactivate later. "Show inactive" reveals deactivated doctors; inactive doctors are hidden from appointment/admission scheduling.
- **Appointments** – Schedule from a searchable, click-to-select table of active patients (captures patient, department, doctor, reason, date & time), confirm, reschedule, mark completed, cancel (with basic double-booking protection). Deactivated patients and doctors cannot be scheduled — they are excluded from the pickers and re-checked at schedule time.
- **Admissions** – Admit an active, not-currently-admitted patient (doctor and diagnosis required), assign an available bed, discharge, cancel (for mistaken entries). Live bed board shows every room/bed and ward type with a per-ward availability summary (e.g. `ICU 0/2`), colour-coded occupancy, and a warning when occupancy is high or no bed is free (when the hospital is full, patients go on a **waiting list** as Pending admissions and are given a bed with **Assign Bed to Selected** once one frees up; their bill opens at that point). Admission status changes feed billing: admitting opens a linked bill from the pre-set admission charge schedule (admit → billing trigger), discharging finalizes all per-day charges to the actual length of stay, and cancelling a mistaken admission voids its unpaid bill.
- **Billing** – Bills per patient (optionally linked to an admission or appointment; an admission's bill is created automatically on admit), line items by category, partial/full payments, auto-calculated balance and status (Unpaid / PartiallyPaid / Paid / Cancelled)
  - **Payments** – Cash (amount tendered and change), Card (card type, last 4 digits only, approval code), HMO (provider, LOA no.), plus e-wallet and bank transfer. Patient payments reduce the patient balance; HMO payments settle the HMO's approved coverage. A bill is Paid once both are settled.
  - **Balances** – each bill shows the patient balance, HMO outstanding and total outstanding, plus what the patient owes across all their bills. The bills list shows totals outstanding from patients and from HMOs, and a **Show: Outstanding** filter.
  - **Billing breakdown** – itemized charges grouped by category (room & board, professional fees, procedures, medicines, laboratory & diagnostics, other), then discounts, VAT and HMO coverage down to the amount due and balance. Adjusted per bill on the **Discounts / Tax / HMO** tab:
    - General discount (percent or peso amount, reason required)
    - Senior citizen / PWD discount: 20%, VAT-exempt, OSCA/PWD ID required. Not combined with the general discount; the higher one applies. Patients aged 60+ are flagged.
    - VAT (12% by default on new bills; existing bills keep 0% so their totals don't change)
    - HMO coverage (provider, LOA / approval no., approved amount), deducted from what the patient pays
    - **Print Statement...** produces a statement of account with the full breakdown and payments.
  - **Admission charge schedule** – the pre-set services billed on admission (admission fee, room rate per ward, nursing care, ICU monitoring, lab panel…), each one-time or per day and for all wards or one ward. Managed from **Billing → Admission Charge Schedule...**; price changes apply to bills generated afterwards.
- **Activity Log** (administrators only) – every action in every module is recorded with the user who did it (sign-ins and failed sign-ins, patients, doctors, appointments, admissions, billing, printed statements, alerts raised/cleared by "System"). Filter by date, module, user or text and export to CSV.

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

### 1. Clone the repository

```
git clone https://github.com/cuartechrisie06/HospitalSystem.git
```

### 2. Start MySQL

Open the **XAMPP Control Panel** and click **Start** next to **MySQL**.

> If you also have the `MySQL80` Windows service installed, leave it stopped — both use port 3306 and will conflict.

### 3. Create the database

Import `Data/schema.sql`. It creates the `hospital_system` database, all 12 tables, and the seed data
(2 users, 4 departments, 5 doctors, 12 beds, 3 patients).

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
  - `Models/` – Data classes (Patient, Doctor, Appointment, Admission, Bed, Bill, BillItem, Payment, Alert, ActivityItem)
  - `Data/Db.cs` – Connection factory
  - `Data/HospitalData.cs` – All SQL queries live here
  - `Data/schema.sql` – Database schema + seed data
  - `Forms/` – LoginForm + DashboardForm
  - `Views/` – PatientsView, DoctorsView, AppointmentsView, AdmissionsView, BillingView

## Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| `Unable to connect to any of the specified MySQL hosts` | MySQL is not running | Start MySQL in XAMPP |
| `Unknown database 'hospital_system'` | Schema not imported | Run step 3 above |
| `The type or namespace name 'MySql' could not be found` | NuGet packages not restored | Right-click solution → Restore NuGet Packages, then rebuild |
| Port 3306 already in use | Two MySQL servers running | Stop the `MySQL80` service, keep only XAMPP's |

## Future Improvement Ideas

- Move the connection string out of source and into `App.config`
- Hash the stored passwords instead of keeping them in plain text
- Add interfaces for better SOLID compliance
- Add more validation and printable reports
