-- Clean up partial migration
DROP TABLE IF EXISTS mood_entry_event_labels CASCADE;

-- Remove the migration entry if it exists
DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251012175542_ConvertToJunctionTableForMoodEntryEventLabels';

-- Clean up test data to avoid foreign key issues
DELETE FROM mood_entries;
DELETE FROM event_labels;