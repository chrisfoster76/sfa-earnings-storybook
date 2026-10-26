-- Wipe script for SfaEventHub (EventHub's local event capture)
-- Only events captured locally: events captured from other environments (e.g. demo) are kept

DELETE FROM [dbo].[ReceivedEvents] WHERE [Environment] = 'local';
