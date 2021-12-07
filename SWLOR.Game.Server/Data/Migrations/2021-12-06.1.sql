-- Make the Starship Auxiliary Light Cannon craftable by reducing the required components numbers.
UPDATE CraftBlueprint 
SET MainMinimum = '2', SecondaryMinimum = '2'
WHERE ID = 667;
