IF DB_ID('WattWatchDB') IS NULL CREATE DATABASE WattWatchDB;
GO
USE WattWatchDB;
GO
CREATE TABLE Roles (
    RoleId INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(30) NOT NULL UNIQUE);
CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    PasswordSalt NVARCHAR(200) NOT NULL,
    RoleId INT NOT NULL REFERENCES Roles(RoleId),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE Buildings (
    BuildingId INT IDENTITY PRIMARY KEY,
    BuildingName NVARCHAR(100) NOT NULL,
    Location NVARCHAR(200) NULL);
CREATE TABLE Devices (
    DeviceId INT IDENTITY PRIMARY KEY,
    DeviceName NVARCHAR(100) NOT NULL,
    BuildingId INT NOT NULL REFERENCES Buildings(BuildingId),
    DeviceCode NVARCHAR(50) NOT NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE EnergyReadings (
    ReadingId BIGINT IDENTITY PRIMARY KEY,
    DeviceId INT NOT NULL REFERENCES Devices(DeviceId),
    PowerKW DECIMAL(10,3) NOT NULL CHECK (PowerKW >= 0),
    EnergyKWh DECIMAL(12,3) NOT NULL CHECK (EnergyKWh >= 0),
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE INDEX IX_Readings_Device_Time ON EnergyReadings(DeviceId, RecordedAt);
CREATE TABLE Thresholds (
    ThresholdId INT IDENTITY PRIMARY KEY,
    DeviceId INT NOT NULL UNIQUE REFERENCES Devices(DeviceId),
    MaxPowerKW DECIMAL(10,3) NOT NULL CHECK (MaxPowerKW > 0),
    IsEnabled BIT NOT NULL DEFAULT 1,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE Alerts (
    AlertId BIGINT IDENTITY PRIMARY KEY,
    DeviceId INT NOT NULL REFERENCES Devices(DeviceId),
    ReadingId BIGINT NOT NULL REFERENCES EnergyReadings(ReadingId),
    AlertType NVARCHAR(50) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active'
        CHECK (Status IN ('Active','Acknowledged','Resolved')),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    AcknowledgedBy INT NULL REFERENCES Users(UserId),
    AcknowledgedAt DATETIME2 NULL);
CREATE TABLE AuditLogs (
    AuditId BIGINT IDENTITY PRIMARY KEY,
    UserId INT NULL REFERENCES Users(UserId),
    Action NVARCHAR(100) NOT NULL,
    Details NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
INSERT INTO Roles(RoleName) VALUES ('Admin'),('User');
-- Demo data
INSERT INTO Buildings(BuildingName, Location) VALUES ('Main Block','Campus');
INSERT INTO Devices(DeviceName, BuildingId, DeviceCode) VALUES ('Computer Lab Meter',1,'LAB-01');
INSERT INTO Thresholds(DeviceId, MaxPowerKW) VALUES (1, 5.000);
GO
