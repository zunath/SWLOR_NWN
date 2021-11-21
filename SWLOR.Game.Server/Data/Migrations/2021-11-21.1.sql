-- Fix the description on Shield Oath, Perk 132
UPDATE Perk
SET Description = 'Increases Damage Immunity and enmity generation but reduces damage while active. Only one stance may be active at a time.'
WHERE ID = 132;