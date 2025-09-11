-- =============================================
-- Script: 02_CreatePostsTable.sql
-- Description: Creates the Posts table for storing LinkedIn post data
-- Author: Postmate API
-- Created: 2024
-- =============================================

USE [PostmateDB];
GO

-- Create the Posts table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Posts](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Topic] [nvarchar](500) NOT NULL,
        [Draft] [nvarchar](max) NULL,
        [Status] [nvarchar](50) NOT NULL,
        [ScheduledAt] [datetime2](7) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    PRINT 'Table Posts created successfully.';
END
ELSE
BEGIN
    PRINT 'Table Posts already exists.';
END
GO

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND name = N'IX_Posts_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Posts_Status] ON [dbo].[Posts] ([Status] ASC);
    PRINT 'Index IX_Posts_Status created successfully.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND name = N'IX_Posts_CreatedAt')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Posts_CreatedAt] ON [dbo].[Posts] ([CreatedAt] DESC);
    PRINT 'Index IX_Posts_CreatedAt created successfully.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND name = N'IX_Posts_ScheduledAt')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Posts_ScheduledAt] ON [dbo].[Posts] ([ScheduledAt] ASC);
    PRINT 'Index IX_Posts_ScheduledAt created successfully.';
END
GO

-- Add check constraint for Status values
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Posts_Status]') AND parent_object_id = OBJECT_ID(N'[dbo].[Posts]'))
BEGIN
    ALTER TABLE [dbo].[Posts] ADD CONSTRAINT [CK_Posts_Status] CHECK ([Status] IN ('Draft', 'Pending', 'Approved', 'Posted', 'Rejected'));
    PRINT 'Check constraint CK_Posts_Status added successfully.';
END
GO

-- Set default value for CreatedAt
IF NOT EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[DF_Posts_CreatedAt]') AND parent_object_id = OBJECT_ID(N'[dbo].[Posts]'))
BEGIN
    ALTER TABLE [dbo].[Posts] ADD CONSTRAINT [DF_Posts_CreatedAt] DEFAULT (GETUTCDATE()) FOR [CreatedAt];
    PRINT 'Default constraint DF_Posts_CreatedAt added successfully.';
END
GO

PRINT 'Posts table setup completed successfully.';
