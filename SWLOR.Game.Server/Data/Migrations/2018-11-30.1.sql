
-- Players were initially set to version 2 on creation.
-- This was previously an unused field, but we're using it now.
-- To alleviate confusion and weird code, we're setting everyone back to
-- version 1 and letting the migration process run to bump them back to 2.
UPDATE dbo.Player
SET VersionNumber = 1
WHERE VersionNumber = 2