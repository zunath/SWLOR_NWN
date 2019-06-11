--  Why: To avoid divide by zero error in AbilityService.ProcessConcentrationEffects:
--                 // Are we ready to continue processing this concentration effect?
--                if (tick % perkFeat.ConcentrationTickInterval != 0) return;
--  What: Set all concentration based perks that have ConcentrationTickInterval = 0 
--        to ConcentrationTickInterval = 1 to avoid divide by zero error.
update PerkFeat
    set ConcentrationTickInterval = 1
    where PerkID in (select p.id
                        from Perk p
                        inner join PerkFeat pf on pf.perkid = p.id
                        where exists (select 1 from PerkCategory pc
                                        where upper(pc.name) like '%FORCE%'
                                        and pc.IsActive = 1
                                        and pc.id = p.PerkCategoryID)
                        and pf.ConcentrationTickInterval = 0
                        and p.ExecutionTypeID = (select pet.id
                                                    from PerkExecutionType pet
                                                    where upper(pet.Name) like '%CONCENTRATION%'));
                                                    
