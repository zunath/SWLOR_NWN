-- Correct the resrefs of three carpets (and clarify the names of two of them).  
update dbo.BaseStructure set PlaceableResref='x0_roundrugorien' where ID=54;
update dbo.BaseStructure set Name='Carpet, Fancy', PlaceableResref='x0_rugoriental' where ID=93;
update dbo.BaseStructure set Name='Carpet, Fancy, Smaller', PlaceableResref='x0_rugoriental2' where ID=96;