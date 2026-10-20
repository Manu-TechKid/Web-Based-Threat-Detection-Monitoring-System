-- RouterDetector Insert Data Script
-- Simple script to insert data into lookup tables

-- =============================================
-- RULE TYPES DATA
-- =============================================
SET IDENTITY_INSERT RuleTypes ON;
INSERT INTO RuleTypes (Id, Name) VALUES
(1, 'Signature-based'),
(2, 'Behavioral'),
(3, 'Anomaly-based'),
(4, 'Brute Force Attack'),
(5, 'DDoS Attack'),
(6, 'Port Scan'),
(7, 'SQL Injection'),
(8, 'XSS Attack'),
(9, 'Phishing Attempt'),
(10, 'Malware Detection'),
(11, 'Unauthorized Access'),
(12, 'Data Exfiltration'),
(13, 'Network Anomaly'),
(14, 'DNS Attack'),
(15, 'HTTP Flood'),
(16, 'SYN Flood'),
(17, 'ICMP Flood'),
(18, 'UDP Flood'),
(19, 'File Access Rule'),
(20, 'Login Attempt Rule'),
(21, 'Policy Violation Rule'),
(22, 'Traffic Analysis'),
(23, 'Protocol Analysis'),
(24, 'Content Inspection');
SET IDENTITY_INSERT RuleTypes OFF;

-- =============================================
-- EVENT TYPES DATA
-- =============================================
SET IDENTITY_INSERT EventTypes ON;
INSERT INTO EventTypes (Id, Name) VALUES
(1, 'File Access'),
(2, 'Login Attempt'),
(3, 'Policy Violation'),
(4, 'Security Alert'),
(5, 'System Warning'),
(6, 'Critical Error'),
(7, 'Information'),
(8, 'Debug'),
(9, 'Threat Detected'),
(10, 'Attack Blocked'),
(11, 'Suspicious Activity'),
(12, 'Configuration Change'),
(13, 'User Login'),
(14, 'User Logout'),
(15, 'Data Access'),
(16, 'Network Traffic'),
(17, 'System Maintenance'),
(18, 'Backup Operation'),
(19, 'Port Scan Event'),
(20, 'Malware Event'),
(21, 'Brute Force Event'),
(22, 'DDoS Event'),
(23, 'SQL Injection Event'),
(24, 'XSS Event');
SET IDENTITY_INSERT EventTypes OFF;

-- =============================================
-- SEVERITIES DATA
-- =============================================
SET IDENTITY_INSERT Severities ON;
INSERT INTO Severities (Id, Name) VALUES
(1, 'Critical'),
(2, 'High'),
(3, 'Medium'),
(4, 'Low'),
(5, 'Info'),
(6, 'Debug');
SET IDENTITY_INSERT Severities OFF;

-- =============================================
-- ACTION TAKENS DATA
-- =============================================
SET IDENTITY_INSERT ActionTakens ON;
INSERT INTO ActionTakens (Id, Name) VALUES
(1, 'Blocked'),
(2, 'Alerted'),
(3, 'Quarantined'),
(4, 'Allowed'),
(5, 'Logged'),
(6, 'Investigated'),
(7, 'Resolved'),
(8, 'Escalated'),
(9, 'Monitored'),
(10, 'Ignored'),
(11, 'Manual Review'),
(12, 'Auto Response'),
(13, 'User Notified'),
(14, 'System Restart'),
(15, 'Configuration Updated'),
(16, 'Backup Created'),
(17, 'File Quarantined'),
(18, 'Connection Terminated'),
(19, 'IP Blocked'),
(20, 'Account Locked');
SET IDENTITY_INSERT ActionTakens OFF;

-- =============================================
-- MESSAGE TYPES DATA
-- =============================================
SET IDENTITY_INSERT MessageTypes ON;
INSERT INTO MessageTypes (Id, Name) VALUES
(1, 'Suspicious activity detected'),
(2, 'Port scan detected'),
(3, 'Malware blocked'),
(4, 'Alert'),
(5, 'Warning'),
(6, 'Error'),
(7, 'Info'),
(8, 'Success'),
(9, 'Debug'),
(10, 'Notification'),
(11, 'Report'),
(12, 'Status'),
(13, 'Update'),
(14, 'Reminder'),
(15, 'Confirmation'),
(16, 'Request'),
(17, 'Response'),
(18, 'Broadcast'),
(19, 'Security Alert'),
(20, 'System Message'),
(21, 'User Notification'),
(22, 'Admin Alert'),
(23, 'Threat Notification'),
(24, 'Block Notification');
SET IDENTITY_INSERT MessageTypes OFF; 