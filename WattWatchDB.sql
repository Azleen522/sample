-- WattWatch Database Setup Script
-- Idempotent: safe to run multiple times
-- Creates database and all tables with data initialization

USE master;
GO

IF DB_ID(N'WattWatchDB') IS NULL
BEGIN
    CREATE DATABASE WattWatchDB;
END
GO

USE WattWatchDB;
GO

-- ============================================
-- TABLE: Roles
-- ============================================
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(30) NOT NULL UNIQUE
    );
    PRINT 'Created table: Roles';
END
ELSE
BEGIN
    PRINT 'Table Roles already exists.';
END
GO

-- ============================================
-- TABLE: Users
-- ============================================
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(500) NOT NULL,
        PasswordSalt NVARCHAR(200) NOT NULL,
        RoleId INT NOT NULL REFERENCES dbo.Roles(RoleId),
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created table: Users';
END
ELSE
BEGIN
    PRINT 'Table Users already exists.';
END
GO

-- ============================================
-- TABLE: Buildings
-- ============================================
IF OBJECT_ID(N'dbo.Buildings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Buildings
    (
        BuildingId INT IDENTITY(1,1) PRIMARY KEY,
        BuildingName NVARCHAR(100) NOT NULL,
        Location NVARCHAR(200) NULL
    );
    PRINT 'Created table: Buildings';
END
ELSE
BEGIN
    PRINT 'Table Buildings already exists.';
END
GO

-- ============================================
-- TABLE: Devices
-- ============================================
IF OBJECT_ID(N'dbo.Devices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Devices
    (
        DeviceId INT IDENTITY(1,1) PRIMARY KEY,
        DeviceName NVARCHAR(100) NOT NULL,
        BuildingId INT NOT NULL REFERENCES dbo.Buildings(BuildingId),
        DeviceCode NVARCHAR(50) NOT NULL UNIQUE,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created table: Devices';
END
ELSE
BEGIN
    PRINT 'Table Devices already exists.';
END
GO

-- ============================================
-- TABLE: EnergyReadings
-- ============================================
IF OBJECT_ID(N'dbo.EnergyReadings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnergyReadings
    (
        ReadingId BIGINT IDENTITY(1,1) PRIMARY KEY,
        DeviceId INT NOT NULL REFERENCES dbo.Devices(DeviceId),
        PowerKW DECIMAL(10,3) NOT NULL CHECK (PowerKW >= 0),
        EnergyKWh DECIMAL(12,3) NOT NULL CHECK (EnergyKWh >= 0),
        RecordedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_Readings_Device_Time ON dbo.EnergyReadings(DeviceId, RecordedAt);
    PRINT 'Created table: EnergyReadings';
END
ELSE
BEGIN
    PRINT 'Table EnergyReadings already exists.';
END
GO

-- ============================================
-- TABLE: Thresholds
-- ============================================
IF OBJECT_ID(N'dbo.Thresholds', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Thresholds
    (
        ThresholdId INT IDENTITY(1,1) PRIMARY KEY,
        DeviceId INT NOT NULL UNIQUE REFERENCES dbo.Devices(DeviceId),
        MaxPowerKW DECIMAL(10,3) NOT NULL CHECK (MaxPowerKW > 0),
        IsEnabled BIT NOT NULL DEFAULT 1,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created table: Thresholds';
END
ELSE
BEGIN
    PRINT 'Table Thresholds already exists.';
END
GO

-- ============================================
-- TABLE: Alerts
-- ============================================
IF OBJECT_ID(N'dbo.Alerts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alerts
    (
        AlertId BIGINT IDENTITY(1,1) PRIMARY KEY,
        DeviceId INT NOT NULL REFERENCES dbo.Devices(DeviceId),
        ReadingId BIGINT NOT NULL REFERENCES dbo.EnergyReadings(ReadingId),
        AlertType NVARCHAR(50) NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active'
            CHECK (Status IN ('Active', 'Acknowledged', 'Resolved')),
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        AcknowledgedBy INT NULL REFERENCES dbo.Users(UserId),
        AcknowledgedAt DATETIME2 NULL
    );
    PRINT 'Created table: Alerts';
END
ELSE
BEGIN
    PRINT 'Table Alerts already exists.';
END
GO

-- ============================================
-- TABLE: AuditLogs
-- ============================================
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL REFERENCES dbo.Users(UserId),
        Action NVARCHAR(100) NOT NULL,
        Details NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created table: AuditLogs';
END
ELSE
BEGIN
    PRINT 'Table AuditLogs already exists.';
END
GO

-- ============================================
-- DATA INITIALIZATION (Idempotent)
-- ============================================

-- Insert default roles
INSERT INTO dbo.Roles(RoleName)
SELECT 'Admin'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Admin');

INSERT INTO dbo.Roles(RoleName)
SELECT 'User'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'User');

PRINT 'Roles initialized.';
GO

-- Insert demo building
INSERT INTO dbo.Buildings(BuildingName, Location)
SELECT 'Main Block', 'Campus'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Buildings WHERE BuildingName = 'Main Block');

PRINT 'Buildings initialized.';
GO

-- Insert demo device
INSERT INTO dbo.Devices(DeviceName, BuildingId, DeviceCode)
SELECT 'Computer Lab Meter', 1, 'LAB-01'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Devices WHERE DeviceCode = 'LAB-01');

PRINT 'Devices initialized.';
GO

-- Insert demo threshold
INSERT INTO dbo.Thresholds(DeviceId, MaxPowerKW, IsEnabled)
SELECT d.DeviceId, 5.000, 1
FROM dbo.Devices d
WHERE d.DeviceCode = 'LAB-01'
AND NOT EXISTS (SELECT 1 FROM dbo.Thresholds WHERE DeviceId = d.DeviceId);

PRINT 'Thresholds initialized.';
GO

PRINT '=== WattWatch Database Setup Complete ===';
PRINT 'Tables created: Roles, Users, Buildings, Devices, EnergyReadings, Thresholds, Alerts, AuditLogs';
PRINT 'Demo data: 1 Building, 1 Device with 5kW threshold, 2 Roles (Admin, User)';
GO
