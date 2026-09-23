-- Hospital System database schema + seed data
-- Run with: mysql -u root < schema.sql

CREATE DATABASE IF NOT EXISTS hospital_system CHARACTER SET utf8mb4;
USE hospital_system;

CREATE TABLE IF NOT EXISTS users (
    username     VARCHAR(50) PRIMARY KEY,
    password     VARCHAR(100) NOT NULL,
    display_name VARCHAR(100),
    role         VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS departments (
    id   INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS doctors (
    id              INT PRIMARY KEY AUTO_INCREMENT,
    full_name       VARCHAR(150) NOT NULL,
    department_id   INT,
    specialization  VARCHAR(150),
    contact         VARCHAR(50),
    is_on_duty      TINYINT(1) NOT NULL DEFAULT 0,
    status          VARCHAR(20) NOT NULL DEFAULT 'Active',
    FOREIGN KEY (department_id) REFERENCES departments(id)
);

CREATE TABLE IF NOT EXISTS patients (
    id             INT PRIMARY KEY AUTO_INCREMENT,
    full_name      VARCHAR(150) NOT NULL,
    age            INT NOT NULL,
    gender         VARCHAR(20),
    contact        VARCHAR(50),
    address        VARCHAR(255),
    blood_type     VARCHAR(10),
    status         VARCHAR(20) NOT NULL DEFAULT 'Active',
    registered_on  DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS beds (
    id           INT PRIMARY KEY AUTO_INCREMENT,
    room_no      VARCHAR(20),
    bed_no       VARCHAR(20),
    ward         VARCHAR(50),
    is_occupied  TINYINT(1) NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS appointments (
    id             INT PRIMARY KEY AUTO_INCREMENT,
    patient_id     INT NOT NULL,
    doctor_id      INT NOT NULL,
    department_id  INT NOT NULL,
    scheduled_on   DATETIME NOT NULL,
    reason         VARCHAR(255),
    status         VARCHAR(20) NOT NULL DEFAULT 'Pending',
    FOREIGN KEY (patient_id) REFERENCES patients(id),
    FOREIGN KEY (doctor_id) REFERENCES doctors(id),
    FOREIGN KEY (department_id) REFERENCES departments(id)
);

CREATE TABLE IF NOT EXISTS admissions (
    id             INT PRIMARY KEY AUTO_INCREMENT,
    patient_id     INT NOT NULL,
    doctor_id      INT NOT NULL,
    bed_id         INT NOT NULL,
    admitted_on    DATETIME NOT NULL,
    discharged_on  DATETIME NULL,
    diagnosis      VARCHAR(255),
    notes          VARCHAR(500),
    status         VARCHAR(20) NOT NULL DEFAULT 'Active',
    FOREIGN KEY (patient_id) REFERENCES patients(id),
    FOREIGN KEY (doctor_id) REFERENCES doctors(id),
    FOREIGN KEY (bed_id) REFERENCES beds(id)
);

CREATE TABLE IF NOT EXISTS alerts (
    id          INT PRIMARY KEY AUTO_INCREMENT,
    title       VARCHAR(150),
    message     VARCHAR(500),
    severity    VARCHAR(20),
    status      VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_on  DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS activity_log (
    id          INT PRIMARY KEY AUTO_INCREMENT,
    module      VARCHAR(50) NOT NULL,
    action      VARCHAR(50) NOT NULL,
    description VARCHAR(255) NOT NULL,
    icon        VARCHAR(10) NOT NULL DEFAULT '',
    created_at  DATETIME NOT NULL
);

-- Seed data (mirrors the previous in-memory HospitalData.Seed()) --

INSERT INTO users (username, password, display_name, role) VALUES
    ('admin', 'admin', 'System Administrator', 'Administrator'),
    ('nurse', 'nurse', 'Ward Nurse', 'Nurse')
ON DUPLICATE KEY UPDATE username = username;

INSERT INTO departments (id, name) VALUES
    (1, 'General Medicine'),
    (2, 'Pediatrics'),
    (3, 'Surgery'),
    (4, 'Cardiology')
ON DUPLICATE KEY UPDATE name = VALUES(name);

INSERT INTO doctors (id, full_name, department_id, specialization, is_on_duty) VALUES
    (1, 'Ana Reyes', 1, 'Internal Medicine', 1),
    (2, 'Mark Villanueva', 1, 'Family Medicine', 1),
    (3, 'Liza Tan', 2, 'Pediatrics', 0),
    (4, 'Jose Cruz', 3, 'General Surgery', 1),
    (5, 'Grace Lim', 4, 'Cardiology', 0)
ON DUPLICATE KEY UPDATE full_name = VALUES(full_name);

-- 12 beds across 6 rooms (101-106), alternating ward, mirrors the C# seed loop
INSERT INTO beds (id, room_no, bed_no, ward, is_occupied) VALUES
    (1,  '101', '1', 'General Ward', 0),
    (2,  '101', '2', 'General Ward', 0),
    (3,  '102', '1', 'Private',      0),
    (4,  '102', '2', 'Private',      0),
    (5,  '103', '1', 'ICU',          0),
    (6,  '103', '2', 'ICU',          0),
    (7,  '104', '1', 'General Ward', 0),
    (8,  '104', '2', 'General Ward', 0),
    (9,  '105', '1', 'Private',      0),
    (10, '105', '2', 'Private',      0),
    (11, '106', '1', 'ICU',          0),
    (12, '106', '2', 'ICU',          0)
ON DUPLICATE KEY UPDATE room_no = VALUES(room_no);

INSERT INTO patients (id, full_name, age, gender, contact, address, blood_type, registered_on) VALUES
    (1, 'Juan Dela Cruz', 45, 'Male',   '09171234567', 'Quezon City', 'O+', DATE_SUB(CURDATE(), INTERVAL 10 DAY)),
    (2, 'Maria Santos',   32, 'Female', '09189876543', 'Makati City', 'A+', DATE_SUB(CURDATE(), INTERVAL 5 DAY)),
    (3, 'Pedro Ramirez',  28, 'Male',   '09221234567', 'Pasig City',  'B+', DATE_SUB(CURDATE(), INTERVAL 2 DAY))
ON DUPLICATE KEY UPDATE full_name = VALUES(full_name);

INSERT INTO alerts (id, title, message, severity, created_on) VALUES
    (1, 'ICU Bed Critical',        'Only 1 ICU bed remaining',                       'High',   DATE_SUB(NOW(), INTERVAL 2 HOUR)),
    (2, 'Staff Shortage',          'Pediatrics has no doctor on duty',               'Medium', DATE_SUB(NOW(), INTERVAL 5 HOUR)),
    (3, 'Equipment Maintenance',   'X-Ray machine scheduled for maintenance tomorrow', 'Low',  DATE_SUB(NOW(), INTERVAL 1 DAY))
ON DUPLICATE KEY UPDATE title = VALUES(title);
