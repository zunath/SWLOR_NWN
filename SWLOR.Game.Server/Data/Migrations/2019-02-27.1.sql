
-- Move everyone's spawn points to Veles medical bay because we're disabling instance respawn points
UPDATE dbo.Player
SET RespawnAreaResref = 'velesinterior',
	RespawnLocationX = 146.146759033203,
	RespawnLocationY = 62.8685264587402,
	RespawnLocationZ = 0,
	RespawnLocationOrientation = 337
WHERE RespawnAreaResref IN ('house_int_1', 'starship1_int', 'starship2_int', 'house_int_9', 'apartment_002', 'apartment_2', 'apartment_3', 'house_int_2',
							'house_int_5', 'house_int_8', 'house_int_3', 'house_int_11')