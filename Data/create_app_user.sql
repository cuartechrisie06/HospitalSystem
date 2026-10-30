-- A MySQL account just for the app, so it doesn't run as root.
-- Run once as root after importing schema.sql:
--   "C:\xampp\mysql\bin\mysql.exe" -u root < Data\create_app_user.sql
-- Then put the same user and password in App.config (connection string "HospitalSystem").
--
-- CHANGE THE PASSWORD below before running this.

CREATE USER IF NOT EXISTS 'hospital_app'@'localhost' IDENTIFIED BY 'change-this-password';

-- Only the hospital_system database. ALTER, CREATE and INDEX are needed because the app upgrades
-- its own tables on startup (SchemaMigrator); there is no access to any other database.
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES
    ON hospital_system.* TO 'hospital_app'@'localhost';

FLUSH PRIVILEGES;
