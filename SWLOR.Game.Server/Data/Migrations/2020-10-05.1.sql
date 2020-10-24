Update Perk Set IsActive = 1 Where ID = 175;
Update Perk Set ExecutionTypeID = 7 Where ID = 175;
Update PerkLevel Set Description = 'The caster gains 10 Temporary Hit Points Per Tick' Where ID = 2281;
Update PerkLevel Set Description = 'The caster gains 20 Temporary Hit Points Per Tick' Where ID = 2282;
Update PerkLevel Set Description = 'The caster gains 30 Temporary Hit Points and 5% Concealment Per Tick' Where ID = 2283;
Update PerkFeat Set ConcentrationTickInterval = 6 Where PerkID = 175;
Update PerkFeat Set ConcentrationFPCost = 3 Where ID = 97;
Update PerkFeat Set ConcentrationFPCost = 4 Where ID = 98;
Update PerkFeat Set ConcentrationFPCost = 6 Where ID = 99;