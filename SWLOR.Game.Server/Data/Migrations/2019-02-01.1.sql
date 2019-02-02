-- Add the Pilot Starship permission to the correct place.
ALTER TABLE dbo.PCBaseStructurePermission ADD CanFlyStarship bit NOT NULL DEFAULT(0);