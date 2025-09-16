-- =============================================
-- Script: 03_AddPostTypeColumn.sql
-- Description: Adds PostType column to the Posts table
-- Author: Postmate API
-- Created: 2024
-- =============================================

-- Add PostType column to Posts table
ALTER TABLE posts 
ADD COLUMN post_type VARCHAR(50) NOT NULL DEFAULT 'educational';

-- Add check constraint for PostType values
ALTER TABLE posts 
ADD CONSTRAINT ck_posts_post_type 
CHECK (post_type IN ('educational', 'listicle', 'storytelling', 'thought-leadership'));

-- Create index for better performance
CREATE INDEX IF NOT EXISTS ix_posts_post_type ON posts (post_type);

-- Update existing records to have the default post type
UPDATE posts SET post_type = 'educational' WHERE post_type IS NULL;

PRINT 'PostType column added successfully to Posts table.';
