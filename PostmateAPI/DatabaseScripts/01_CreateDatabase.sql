-- =============================================
-- Script: 01_CreateDatabase.sql
-- Description: Creates the Postmate database
-- Author: Postmate API
-- Created: 2024
-- =============================================

-- Create the Postmate database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PostmateDB')
BEGIN
    CREATE DATABASE [PostmateDB]
    COLLATE SQL_Latin1_General_CP1_CI_AS;
    
    PRINT 'Database PostmateDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database PostmateDB already exists.';
END
GO

-- Use the Postmate database
USE [PostmateDB];
GO

-- Set database options
ALTER DATABASE [PostmateDB] SET RECOVERY SIMPLE;
ALTER DATABASE [PostmateDB] SET AUTO_SHRINK OFF;
ALTER DATABASE [PostmateDB] SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE [PostmateDB] SET AUTO_UPDATE_STATISTICS ON;
GO

PRINT 'Database PostmateDB configured successfully.';
