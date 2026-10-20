-- RouterDetector Delete Records Script
-- This script deletes all records from lookup tables
-- Execute this script before inserting new data to avoid conflicts

-- =============================================
-- DELETE ALL RECORDS FROM LOOKUP TABLES
-- =============================================

-- Delete from RuleTypes
DELETE FROM RuleTypes;
PRINT 'Deleted all records from RuleTypes';

-- Delete from EventTypes
DELETE FROM EventTypes;
PRINT 'Deleted all records from EventTypes';

-- Delete from Severities
DELETE FROM Severities;
PRINT 'Deleted all records from Severities';

-- Delete from ActionTakens
DELETE FROM ActionTakens;
PRINT 'Deleted all records from ActionTakens';

-- Delete from MessageTypes
DELETE FROM MessageTypes;
PRINT 'Deleted all records from MessageTypes';

-- =============================================
-- VERIFICATION - CHECK IF TABLES ARE EMPTY
-- =============================================

SELECT 'RuleTypes' as TableName, COUNT(*) as RecordCount FROM RuleTypes
UNION ALL
SELECT 'EventTypes' as TableName, COUNT(*) as RecordCount FROM EventTypes
UNION ALL
SELECT 'Severities' as TableName, COUNT(*) as RecordCount FROM Severities
UNION ALL
SELECT 'ActionTakens' as TableName, COUNT(*) as RecordCount FROM ActionTakens
UNION ALL
SELECT 'MessageTypes' as TableName, COUNT(*) as RecordCount FROM MessageTypes;

-- =============================================
-- RESET IDENTITY COLUMNS (OPTIONAL)
-- =============================================
-- Uncomment the following lines if you want to reset the ID columns to start from 1

/*
DBCC CHECKIDENT ('RuleTypes', RESEED, 0);
DBCC CHECKIDENT ('EventTypes', RESEED, 0);
DBCC CHECKIDENT ('Severities', RESEED, 0);
DBCC CHECKIDENT ('ActionTakens', RESEED, 0);
DBCC CHECKIDENT ('MessageTypes', RESEED, 0);
PRINT 'Identity columns reset to start from 1';
*/ 