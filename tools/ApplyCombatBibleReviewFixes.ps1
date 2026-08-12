[CmdletBinding()]
param(
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx",
    [switch]$EspionageStealthOnly,
    [string[]]$OnlyPerkName = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workbookFullPath = if ([IO.Path]::IsPathRooted($WorkbookPath)) {
    $WorkbookPath
}
else {
    Join-Path $repoRoot $WorkbookPath
}

# Keep these changes declarative so every review correction is reproducible and auditable. The
# writer below changes only the named cells inside the workbook ZIP; it does not recalculate or
# rewrite formula-backed sheets through a spreadsheet library.
$perkChanges = @(
    @{
        Sheet = "Lightsaber"
        PerkName = "Force Sheath II"
        Values = @{
            Description = "On your next hit, deal + 17 Force DMG."
        }
    },
    @{
        Sheet = "Lightsaber"
        PerkName = "Force Sheath III"
        Values = @{
            Description = "On your next hit, deal + 23 Force DMG."
        }
    },
    @{
        Sheet = "Lightsaber"
        PerkName = "Force Sheath IV"
        Values = @{
            Description = "On your next hit, deal + 30 Force DMG."
        }
    },
    @{
        Sheet = "Katar"
        PerkName = "Guard Counter I"
        Values = @{
            Description = "Queue your next auto attack to deal weapon DMG + 8. If you guarded an attack within the last 30 seconds, it deals weapon DMG + 16 instead."
            "Casting Time" = "Queued"
        }
    },
    @{
        Sheet = "Katar"
        PerkName = "Guard Counter II"
        Values = @{
            Description = "Queue your next auto attack to deal weapon DMG + 18. If you guarded an attack within the last 30 seconds, it deals weapon DMG + 30 instead."
            "Casting Time" = "Queued"
        }
    },
    @{
        Sheet = "Katar"
        PerkName = "Guard Counter III"
        Values = @{
            Description = "Queue your next auto attack to deal weapon DMG + 28. If you guarded an attack within the last 30 seconds, it deals weapon DMG + 45 instead and inflicts Dazed for 15 seconds."
            "Casting Time" = "Queued"
        }
    },
    @{
        Sheet = "Katar"
        PerkName = "Redirecting Counter"
        Values = @{
            Description = "When you guard an attack, your next attack within 30 seconds gains +10% critical chance and deals +10 DMG."
        }
    },
    @{
        Sheet = "Spear"
        PerkName = "Vigor Stance"
        Values = @{
            Description = "While active, all outgoing damage is increased by 10%. Using any hostile combat ability costs 2 additional STM and grants +8% Evasion for 30 seconds."
        }
    },
    @{
        Sheet = "Spear"
        PerkName = "Disruption Field I"
        Values = @{
            Description = "Deals weapon DMG + 16 to enemies within 5m of you and inflicts Force Disruption for 30 seconds."
        }
    },
    @{
        Sheet = "Spear"
        PerkName = "Disruption Field II"
        Values = @{
            Description = "Deals weapon DMG + 30 to enemies within 5m of you and inflicts Force Disruption for 30 seconds."
        }
    },
    @{
        Sheet = "Lightsaber"
        PerkName = "Epicenter"
        Values = @{
            Description = "Inflicts Knockdown on enemies within 6m of you for 6 seconds, dealing 25 Force DMG and inflicting Sunder. Enemies that already had Sunder when struck take an additional 15 Force DMG."
        }
    },
    @{
        Sheet = "Spear"
        PerkName = "Forcebane"
        Values = @{
            Description = "Deals weapon DMG + 25 to enemies within 5m of you. Enemies hit suffer Foggy Mind, Force Disruption, and -20% Ability Accuracy for 45 seconds."
        }
    },
    @{
        Sheet = "Katar"
        PerkName = "Scrapheap Lockdown"
        Values = @{
            Description = "Deals weapon DMG + 25 to enemies within 5m of you. Enemies hit suffer Dazed and Hamstring for 30 seconds."
        }
    },
    @{
        Sheet = "Throwing"
        PerkName = "Concussive Toss I"
        Values = @{
            Description = "Deals weapon DMG + 16 to enemies within 5m of you and inflicts Dazed for 15 seconds."
        }
    },
    @{
        Sheet = "Throwing"
        PerkName = "Concussive Toss II"
        Values = @{
            Description = "Deals weapon DMG + 30 to enemies within 5m of you and inflicts Dazed for 15 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Remote Charge I"
        Values = @{
            Description = "Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 30 fire DMG plus PER scaling."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Remote Charge II"
        Values = @{
            Description = "Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 42 fire DMG plus PER scaling and inflicts Knockdown for 6 seconds."
        }
    },
    @{
        Sheet = "Vibroknife"
        PerkName = "Venatic Recovery"
        Values = @{
            Description = "Your first attack in combat restores 15 STM. This can trigger once every 60 seconds."
        }
    },
    @{
        Sheet = "Armor"
        PerkName = "Provoke II"
        Values = @{
            Description = "Goads the selected target and all other enemies within 8m of it into attacking you.`nEnmity generated increases by 1% per VIT."
        }
    },
    @{
        Sheet = "Lightsaber"
        PerkName = "Guardian's Challenge I"
        Values = @{
            Description = "Deals weapon DMG + 12 to enemies in an 8m x 3m line. For each struck enemy that damaged you within the last 30 seconds, gain +20% Enmity toward it for 30 seconds."
        }
    },
    @{
        Sheet = "Lightsaber"
        PerkName = "Guardian's Challenge II"
        Values = @{
            Description = "Deals weapon DMG + 24 to enemies in an 8m x 3m line. For each struck enemy that damaged you within the last 30 seconds, gain +30% Enmity toward it for 30 seconds."
        }
    },
    @{
        Sheet = "Lightsaber"
        PerkName = "Overpower I"
        Values = @{
            Description = "Spending at least 5 FP on a hostile combat ability increases your Force Attack by 3% for 30 seconds, stacking up to 9%."
        }
    },
    @{
        Sheet = "Spear"
        PerkName = "Hampering Barrage I"
        Values = @{
            Description = "Deals weapon DMG + 16 in a 5m x 5m cone and inflicts Hamstring for 30 seconds."
        }
    },
    @{
        Sheet = "Spear"
        PerkName = "Hampering Barrage II"
        Values = @{
            Description = "Deals weapon DMG + 30 in a 5m x 5m cone and inflicts Hamstring for 30 seconds."
        }
    },
    @{
        Sheet = "Saberstaff"
        PerkName = "Maelstrom Arc I"
        Values = @{
            Description = "Deals weapon DMG + 16 in a 5m x 5m cone. If your Ranged Deflection negated a ranged weapon auto-attack in the last 30 seconds, restore 4 FP."
        }
    },
    @{
        Sheet = "Saberstaff"
        PerkName = "Maelstrom Arc II"
        Values = @{
            Description = "Deals weapon DMG + 30 in a 5m x 5m cone. If your Ranged Deflection negated a ranged weapon auto-attack in the last 30 seconds, restore 8 FP."
        }
    },
    @{
        Sheet = "Staff"
        PerkName = "Worldbreaker"
        Values = @{
            Description = "Deals weapon DMG + 35 to enemies within 5m of you. Enemies affected by control effects take +40 DMG and are Dazed for 30 seconds."
        }
    },
    @{
        Sheet = "Staff"
        PerkName = "Line Breaker I"
        Values = @{
            Description = "Deals weapon DMG + 8 in an 8m x 2.5m line and inflicts Disoriented for 30 seconds."
        }
    },
    @{
        Sheet = "Staff"
        PerkName = "Line Breaker II"
        Values = @{
            Description = "Deals weapon DMG + 18 in an 8m x 3m line and inflicts Disoriented for 30 seconds."
        }
    },
    @{
        Sheet = "Staff"
        PerkName = "Line Breaker III"
        Values = @{
            Description = "Deals weapon DMG + 28 in an 8m x 3m line and inflicts Disoriented for 30 seconds."
        }
    },
    @{
        Sheet = "Staff"
        PerkName = "Line Breaker IV"
        Values = @{
            Description = "Deals weapon DMG + 38 in an 8m x 3m line and inflicts Disoriented for 30 seconds."
        }
    },
    @{
        Sheet = "Rifle"
        PerkName = "Suppressive Line I"
        Values = @{
            Description = "Deals weapon DMG + 16 in a 20m x 3m line. Targets hit by multiple Suppression stacks are Disoriented for 30 seconds."
        }
    },
    @{
        Sheet = "Rifle"
        PerkName = "Suppressive Line II"
        Values = @{
            Description = "Deals weapon DMG + 30 in a 20m x 3m line. Targets hit by multiple Suppression stacks are Disoriented for 30 seconds."
        }
    },
    @{
        Sheet = "Rifle"
        PerkName = "Piercing Round I"
        Values = @{
            Description = "Deals weapon DMG + 12 in a 20m x 3m line and ignores 10% Defense."
        }
    },
    @{
        Sheet = "Rifle"
        PerkName = "Piercing Round II"
        Values = @{
            Description = "Deals weapon DMG + 24 in a 20m x 3m line and ignores 15% Defense."
        }
    },
    @{
        Sheet = "Rifle"
        PerkName = "Piercing Round III"
        Values = @{
            Description = "Deals weapon DMG + 36 in a 20m x 3m line and ignores 20% Defense."
        }
    },
    @{
        Sheet = "Throwing"
        PerkName = "Explosive Toss I"
        Values = @{
            Description = "Deals weapon DMG + 7 to enemies in a 5m-radius area at the target location and inflicts Burn for 30 seconds."
        }
    },
    @{
        Sheet = "Throwing"
        PerkName = "Explosive Toss II"
        Values = @{
            Description = "Deals weapon DMG + 15 to enemies in a 5m-radius area at the target location and inflicts Burn for 30 seconds."
        }
    },
    @{
        Sheet = "Throwing"
        PerkName = "Explosive Toss III"
        Values = @{
            Description = "Deals weapon DMG + 24 to enemies in a 5m-radius area at the target location and inflicts Burn for 30 seconds."
        }
    },
    @{
        Sheet = "Throwing"
        PerkName = "Explosive Toss IV"
        Values = @{
            Description = "Deals weapon DMG + 32 to enemies in a 5m-radius area at the target location and inflicts Burn for 30 seconds."
        }
    },
    @{
        Sheet = "Throwing"
        PerkName = "Rain of Steel"
        Values = @{
            Description = "Deals weapon DMG + 20 to enemies within 5m of you. For 45 seconds, thrown area abilities leave fragmentation zones that deal 8 physical DMG every 6 seconds."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Force Push I"
        Values = @{
            Description = "Deals 8 force DMG to one target in a 5m x 5m cone, knocks it down for 6 seconds, and slows its movement for 12 seconds."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Force Push II"
        Values = @{
            Description = "Deals 12 force DMG to up to 2 targets in an 8m x 5m cone, knocks them down for 6 seconds, and slows their movement for 12 seconds."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Force Push III"
        Values = @{
            Description = "Deals 18 force DMG to up to 3 targets in a 10m x 5m cone, knocks them down for 6 seconds, and slows their movement for 12 seconds."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Throw Lightsaber I"
        Values = @{
            Description = "Hurls your equipped weapon with the Force through a 15m x 2.5m line, dealing weapon DMG + 10 physical DMG plus WIL/PER scaling to one target in the line."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Throw Lightsaber II"
        Values = @{
            Description = "Hurls your equipped weapon with the Force through a 15m x 2.5m line, dealing weapon DMG + 20 physical DMG plus WIL/PER scaling to the selected target and one additional enemy in the line."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Throw Lightsaber III"
        Values = @{
            Description = "Hurls your equipped weapon with the Force through a 15m x 2.5m line, dealing weapon DMG + 30 physical DMG plus WIL/PER scaling to the selected target and up to two additional enemies in the line."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Radiant Lance I"
        Values = @{
            Description = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 12 force DMG plus WIL scaling to hostile targets in the line."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Radiant Lance II"
        Values = @{
            Description = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 22 force DMG plus WIL scaling to hostile targets in the line."
        }
    },
    @{
        Sheet = "Force"
        PerkName = "Radiant Lance III"
        Values = @{
            Description = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 32 force DMG plus WIL scaling to hostile targets in the line."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Frag Grenade II"
        Values = @{
            Description = "Deals 32 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed for 12 seconds. Consumes explosives."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Frag Grenade III"
        Values = @{
            Description = "Deals 48 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed for 12 seconds. Consumes explosives."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Cluster Grenade"
        Values = @{
            Description = "Throws three adjacent grenades within 3m of the target point. Each grenade deals 18 fire DMG plus PER scaling in a 2m blast, and overlapping blasts can hit the same enemy. Consumes explosives."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Ion Grenade II"
        Values = @{
            Description = "Deals 34 electrical DMG plus PER scaling in a 3m blast. Deals 60% bonus damage to droids and inflicts Shock for 12 seconds. Consumes explosives."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Incendiary Field I"
        Values = @{
            Description = "Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 8 fire DMG plus PER scaling every 3 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Incendiary Field II"
        Values = @{
            Description = "Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 12 fire DMG plus PER scaling every 3 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Incendiary Field III"
        Values = @{
            Description = "Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 16 fire DMG plus PER scaling every 3 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Emergency Bunker"
        Values = @{
            Description = "Deploys a 4m-radius shield bunker at the target location for 45 seconds. Allies inside gain 60 temporary HP plus 8% of each target's maximum HP and take 15% less ranged physical damage."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Flamethrower I"
        Values = @{
            Description = "Deals 16 fire DMG plus PER scaling to hostile targets in a 6m x 5m cone."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Flamethrower II"
        Values = @{
            Description = "Deals 28 fire DMG plus PER scaling to hostile targets in a 6m x 5m cone and attempts to inflict Burn for 12 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Flamethrower III"
        Values = @{
            Description = "Deals 42 fire DMG plus PER scaling to hostile targets in a 6m x 5m cone and attempts to inflict Burn for 12 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Cryo Sprayer"
        Values = @{
            Description = "Deals 25 ice DMG plus PER scaling to hostile targets in a 6m x 5m cone and slows their movement for 30 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Ion Lance I"
        Values = @{
            Description = "Fires a focused ion beam from a wrist projector in an 8m x 2.5m line, dealing 12 electrical DMG plus PER scaling to hostile targets in the line."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Ion Lance II"
        Values = @{
            Description = "Fires a focused ion beam from a wrist projector in an 8m x 2.5m line, dealing 22 electrical DMG plus PER scaling to hostile targets in the line."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Ion Lance III"
        Values = @{
            Description = "Fires a focused ion beam from a wrist projector in an 8m x 2.5m line, dealing 32 electrical DMG plus PER scaling to hostile targets in the line."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Rail Dart I"
        Values = @{
            Description = "Fires a dart that deals 18 physical DMG plus PER scaling and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Rail Dart II"
        Values = @{
            Description = "Fires a dart that deals 34 physical DMG plus PER scaling and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Devices"
        PerkName = "Rail Dart III"
        Values = @{
            Description = "Fires a dart that deals 48 physical DMG plus PER scaling and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Rending Claw I"
        Values = @{
            Description = "The beast's next attack deals +10 physical DMG and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Rending Claw II"
        Values = @{
            Description = "The beast's next attack deals +18 physical DMG and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Rending Claw III"
        Values = @{
            Description = "The beast's next attack deals +28 physical DMG and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Claw I"
        Values = @{
            Description = "The beast's next attack deals +10 physical DMG and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Claw II"
        Values = @{
            Description = "The beast's next attack deals +18 physical DMG and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Claw III"
        Values = @{
            Description = "The beast's next attack deals +28 physical DMG and attempts to inflict Bleed for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Poison Breath I"
        Values = @{
            Description = "The beast breathes poison at hostile targets in a 6m x 5m cone, dealing 10 poison DMG plus MGT scaling and attempting to inflict Poison for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Poison Breath II"
        Values = @{
            Description = "The beast breathes poison at hostile targets in a 6m x 5m cone, dealing 14 poison DMG plus MGT scaling and attempting to inflict Poison for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Poison Breath III"
        Values = @{
            Description = "The beast breathes poison at hostile targets in a 6m x 5m cone, dealing 18 poison DMG plus MGT scaling and attempting to inflict Poison for 12 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Ice Breath I"
        Values = @{
            Description = "The beast breathes ice at hostile targets in a 6m x 5m cone, dealing 10 ice DMG plus MGT scaling and slowing affected enemies for 4 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Ice Breath II"
        Values = @{
            Description = "The beast breathes ice at hostile targets in a 6m x 5m cone, dealing 14 ice DMG plus MGT scaling and slowing affected enemies for 5 seconds."
        }
    },
    @{
        Sheet = "Beast Mastery"
        PerkName = "Ice Breath III"
        Values = @{
            Description = "The beast breathes ice at hostile targets in a 6m x 5m cone, dealing 18 ice DMG plus MGT scaling and immobilizing affected enemies for 10 seconds."
        }
    }
)

# Technique requirements follow player-facing encounter progression from the World NPCs tab.
# Same-level sources are ordered by their practical encounter difficulty: CZ220 Mynocks begin at
# rank 0, Probe Droids at rank 1, and later sources progress continuously through rank 40.
# Level-50 Tough, Elite, and Boss sources fill ranks 41-50. Additional/training rows are excluded,
# and every Mimicry rank from 0 through 50 has at least one technique.
$mimicryRequirements = @{
    "Crippling Talons" = 4
    "Frost Spit" = 3
    "Raking Claws" = 6
    "Rending Bite" = 7
    "Target Lock" = 18
    "Toxic Spit" = 28
    "Barbed Volley" = 47
    "Bonecrusher Bite" = 19
    "Brutal Bash" = 29
    "Capacitor Surge" = 2
    "Chitin Guard" = 20
    "Concussive Challenge" = 47
    "Force Rend" = 23
    "Glacial Slime" = 24
    "Hoarfrost Glob" = 32
    "Ion Burst" = 36
    "Iron Carapace" = 21
    "Mauling Bite" = 9
    "Mind Spike" = 25
    "Overload Shot" = 17
    "Piercing Quills" = 13
    "Pouncing Strike" = 8
    "Precision Shot" = 1
    "Rending Carve" = 43
    "Rime Pounce" = 34
    "Savage Roar" = 22
    "Serrated Slash" = 30
    "Sonic Shriek" = 0
    "Static Web" = 1
    "Suppressing Shot" = 1
    "Tactical Mark" = 5
    "Tail Sweep" = 10
    "Venom Spray" = 31
    "Arc Pulse" = 35
    "Blood Frenzy Flurry" = 43
    "Brace Breaker" = 41
    "Dark Shock" = 37
    "Disorienting Screech" = 0
    "Dread Wave" = 38
    "Essence Scar" = 42
    "Force Sunder" = 47
    "Goring Charge" = 14
    "Grenade Burst" = 12
    "Null Shock" = 48
    "Opening Cut" = 41
    "Pack Harrier" = 42
    "Permafrost Rupture" = 39
    "Rally Breaker" = 42
    "Rangefinder Shot" = 41
    "Seismic Slam" = 27
    "Shrapnel Burst" = 16
    "Signal Snare" = 41
    "Static Burst" = 43
    "Stim Canister" = 43
    "Toxic Cloud" = 33
    "Apex Collapse" = 50
    "Crossfire Drill" = 45
    "Cryo Bile" = 40
    "Final Eclipse" = 50
    "Final Line" = 49
    "Final Mandate" = 49
    "Final Suppression" = 48
    "Finishing Drive" = 48
    "Holdfast Slam" = 44
    "Inferno Blast" = 15
    "Inner Circle Bind" = 44
    "Inner Circle Pounce" = 46
    "Inner Circle Surge" = 45
    "Inner Circle Volley" = 45
    "Inner Ring Flurry" = 44
    "Inner Void" = 46
    "Last Bastion" = 47
    "Lockstep Crush" = 43
    "Merciless Angle" = 44
    "Pressure Lock" = 44
    "Rupturing Quake" = 26
    "Scorching Breath" = 50
    "Snap Rush" = 46
    "Sustain Burn" = 45
    "Terrifying Bellow" = 11
    "Warden Clamp" = 48
    "Warden Mark" = 49
    "Warden Maul" = 50
    "Warden Order" = 49
    "Warden Rend" = 49
    "Warden Sweep" = 48
    "Warden Wall" = 47
    "Will Fracture" = 46
}

$mimicryTraitNames = @(
    "Bonecrusher Bite", "Chitin Guard", "Crippling Talons", "Essence Scar", "Force Rend",
    "Force Sunder", "Glacial Slime", "Hoarfrost Glob", "Iron Carapace", "Mauling Bite",
    "Mind Spike", "Opening Cut", "Overload Shot", "Precision Shot", "Rangefinder Shot",
    "Rending Bite", "Rending Carve", "Rime Pounce", "Serrated Slash", "Tactical Mark",
    "Target Lock"
)

foreach ($entry in $mimicryRequirements.GetEnumerator()) {
    $notes = if ($mimicryTraitNames -contains $entry.Key) {
        "Requires Mimicry rank $($entry.Value). Passive trait applied while equipped; learned from creatures via the combat analyzer."
    }
    else {
        "Requires Mimicry rank $($entry.Value). Learned from creatures via the combat analyzer."
    }

    $perkChanges += @{
        Sheet = "Mimicry"
        PerkName = $entry.Key
        Values = @{
            "Skill Reqs." = if ($entry.Value -eq 0) { "-" } else { "Mimicry $($entry.Value)" }
            Notes = $notes
        }
    }
}

$mimicryAnalyzerChanges = @{
    "Combat Analyzer I" = @{
        Description = "Grants a combat analyzer capable of recording enemy creature techniques. Unlocks technique learning and the Techniques window. Provides 2 technique slots."
        Notes = "Unlocks the Techniques window (/techniques) and technique learning. Individual techniques require the Mimicry ranks listed on their rows."
    }
    "Combat Analyzer II" = @{
        Description = "Upgrades the combat analyzer, increasing equipped technique potency by 5%."
        Notes = "Cumulative equipped technique potency bonus: +5%. Requires Combat Analyzer I."
    }
    "Combat Analyzer III" = @{
        Description = "Further upgrades the combat analyzer, increasing equipped technique potency by 10% in total."
        Notes = "Cumulative equipped technique potency bonus: +10%. Requires Combat Analyzer II."
    }
    "Combat Analyzer IV" = @{
        Description = "Maximizes the combat analyzer, increasing equipped technique potency by 15% in total."
        Notes = "Cumulative equipped technique potency bonus: +15%. Requires Combat Analyzer III."
    }
}
foreach ($entry in $mimicryAnalyzerChanges.GetEnumerator()) {
    $perkChanges += @{
        Sheet = "Mimicry"
        PerkName = $entry.Key
        Values = $entry.Value
    }
}

$mimicryDescriptions = @{
    "Apex Collapse" = "While active, grants +25% Attack and +15% Critical Rate at the cost of -20% Physical Defense and -20% Force Defense."
    "Barbed Volley" = "Deals 18 physical DMG plus PER scaling in an 8m x 5m cone. Inflicts Bleed for 30 seconds."
    "Capacitor Surge" = "Inflicts Shock on enemies within 4m of you for 30 seconds. Deals no direct damage."
    "Concussive Challenge" = "Inflicts Dazed on enemies within 6m of you for 15 seconds. Deals no direct damage."
    "Ion Burst" = "Inflicts Disoriented on enemies in an 8m x 5m cone for 30 seconds. Deals no direct damage."
    "Piercing Quills" = "Launches quills in an 8m x 5m cone, inflicting Sunder and reducing struck enemies' Physical and Force Defense by 15% for 30 seconds. Deals no direct damage."
    "Savage Roar" = "Inflicts Weakened on enemies within 6m of you for 30 seconds. Deals no direct damage."
    "Sonic Shriek" = "Deals 18 sonic DMG plus WIL scaling in an 8m x 5m cone. Inflicts Disoriented for 30 seconds."
    "Static Web" = "Inflicts Shock on enemies within 4.5m of you for 30 seconds. Deals no direct damage."
    "Suppressing Shot" = "Inflicts Dazed on enemies in a 10m x 2.5m line for 15 seconds. Deals no direct damage."
    "Tail Sweep" = "Deals 18 physical DMG plus MGT scaling to enemies within 4.5m of you. Inflicts Dazed for 15 seconds."
    "Venom Spray" = "Deals 18 poison DMG plus PER scaling in an 8m x 5m cone. Inflicts Poison for 30 seconds."
    "Arc Pulse" = "Inflicts Shock on enemies within 4.5m of you for 30 seconds. Deals no direct damage."
    "Blood Frenzy Flurry" = "Deals 28 physical DMG plus AGI scaling in a 5m x 5m cone. Inflicts Bleed for 30 seconds."
    "Dark Shock" = "Inflicts Force Suppression on enemies within 4.5m of you for 30 seconds. Deals no direct damage."
    "Disorienting Screech" = "Inflicts Disoriented on enemies within 9m of you for 30 seconds and reduces their Accuracy by 10%. Deals no direct damage."
    "Goring Charge" = "Deals 28 physical DMG plus MGT scaling in an 8m x 2.5m line. Inflicts Bleed for 30 seconds."
    "Grenade Burst" = "Deals 28 fire DMG plus PER scaling to enemies within 4.5m of you. Inflicts Burn for 30 seconds."
    "Null Shock" = "Inflicts Force Suppression for 30 seconds and drains 10 FP and 10 STM from enemies in a 4.5m-radius area at the target location. Deals no direct damage."
    "Permafrost Rupture" = "Inflicts Freezing on enemies within 5.5m of you for 6 seconds. Deals no direct damage."
    "Seismic Slam" = "Deals 28 physical DMG plus MGT scaling to enemies within 6m of you and knocks them down for 6 seconds."
    "Shrapnel Burst" = "Deals 28 physical DMG plus PER scaling in an 8m x 5m cone. Inflicts Sunder for 30 seconds."
    "Dread Wave" = "Deals 28 sonic DMG plus WIL scaling to enemies within 6m of you. Inflicts Weakened for 30 seconds."
    "Static Burst" = "Deals 28 electrical DMG plus PER scaling to enemies within 4.5m of you and inflicts Shock for 30 seconds. It then arcs to up to 2 nearby enemies, dealing 10 electrical DMG plus PER scaling and inflicting Shock for 30 seconds on each."
    "Stim Canister" = "Deploys a stim canister that grants you and allies within 4m of you +10% Attack and +10% Haste for 30 seconds."
    "Toxic Cloud" = "Deals 28 poison DMG plus PER scaling to enemies in a 4.5m-radius area at the target location. Inflicts Toxin for 30 seconds."
    "Crossfire Drill" = "Inflicts Suppression on enemies in a 5m x 5m cone for 30 seconds. Deals no direct damage."
    "Cryo Bile" = "Inflicts Freezing and Immobilized on enemies in an 8m x 5m cone for 6 seconds and generates 100 additional Enmity per target. Deals no direct damage."
    "Final Eclipse" = "Deals 40 force DMG plus MGT scaling in an 8m x 2.5m line, increased by 40% against Weakened targets. Inflicts Force Disruption for 30 seconds and restores 5 FP to you per hit."
    "Final Line" = "Deals 40 physical DMG plus MGT scaling in an 8m x 2.5m line. Inflicts Exposed for 30 seconds and deals up to +35% DMG based on the target's missing health."
    "Final Mandate" = "Issues a command that grants you and allies within 8m of you +15% Attack and +10% Accuracy for 30 seconds."
    "Final Suppression" = "Inflicts Stunned on enemies in an 8m x 2.5m line for 6 seconds. Deals no direct damage."
    "Inferno Blast" = "Deals 40 fire DMG plus MGT scaling in a 10m x 7m cone, increased by 50% against targets already suffering Burn. Inflicts Burn for 30 seconds."
    "Inner Circle Surge" = "Deals 48 electrical DMG plus SOC scaling to a single target, increased by 50% if it is suffering Shock. Inflicts Exposed for 30 seconds, then arcs to up to 3 enemies within 6m of the target for 16 electrical DMG plus SOC scaling and Shock for 30 seconds each."
    "Inner Circle Volley" = "Deals 48 sonic DMG plus SOC scaling to a single target, increased by 50% if it is Dazed or Disoriented. Inflicts Disoriented for 30 seconds."
    "Inner Ring Flurry" = "Strikes a single target, inflicting Bleed for 30 seconds and restoring 4 STM to you. Deals no direct damage."
    "Last Bastion" = "Anchors a defensive line, granting allies within 8m of you a shield that absorbs 30 damage for 30 seconds; enemies within 8m generate +25% Enmity toward you for 30 seconds. Deals no direct damage."
    "Lockstep Crush" = "Deals 40 physical DMG plus AGI scaling in a 5m x 5m cone. Inflicts Knockdown for 6 seconds and Sunder for 30 seconds."
    "Merciless Angle" = "Deals 40 physical DMG plus SOC scaling in a 5m x 5m cone, increased by 50% against targets already suffering Bleed or Hemorrhage. Afflicted targets consume those effects to take another 40 physical DMG plus SOC scaling; other targets instead gain Hemorrhage for 30 seconds."
    "Pressure Lock" = "Inflicts Immobilized on enemies in a 5m x 5m cone for 15 seconds. Deals no direct damage."
    "Rupturing Quake" = "Deals 40 physical DMG plus MGT scaling to enemies within 9m of you. Inflicts Knockdown for 6 seconds and Sunder for 30 seconds."
    "Scorching Breath" = "Deals 40 fire DMG plus MGT scaling in an 8m x 5m cone. Inflicts Burn and Weakened for 30 seconds."
    "Snap Rush" = "A burst of speed that restores 6 STM and grants +15% Haste for 15 seconds."
    "Terrifying Bellow" = "Inflicts Dazed on enemies within 6m of you for 15 seconds and interrupts their current actions. Deals no direct damage."
    "Warden Clamp" = "Inflicts Dazed on enemies within 5.5m of you for 15 seconds and generates 75 additional Enmity per target. Deals no direct damage."
    "Warden Mark" = "Inflicts Marked on enemies within 5.5m of you for 30 seconds and generates 75 additional Enmity per target. Deals no direct damage."
    "Warden Maul" = "Knocks down enemies within 5.5m of you for 6 seconds, pulls them to you, and generates 100 additional Enmity per target. Deals no direct damage."
    "Warden Order" = "Sounds a restorative order, healing you and allies within 5.5m of you for 15% of maximum HP."
    "Warden Rend" = "Inflicts Weakened on enemies within 5.5m of you for 30 seconds, restores 4 FP to you per hit, and generates 75 additional Enmity per target. Deals no direct damage."
    "Will Fracture" = "Inflicts Foggy Mind on enemies in a 5m x 5m cone for 30 seconds and restores 4 FP to you per hit. Deals no direct damage."
}
foreach ($entry in $mimicryDescriptions.GetEnumerator()) {
    $perkChanges += @{
        Sheet = "Mimicry"
        PerkName = $entry.Key
        Values = @{ Description = $entry.Value }
    }
}

$mimicryDamageScaling = @{
    "Raking Claws" = "AGI"
    "Toxic Spit" = "PER"
    "Barbed Volley" = "PER"
    "Brutal Bash" = "MGT"
    "Pouncing Strike" = "MGT"
    "Sonic Shriek" = "WIL"
    "Tail Sweep" = "MGT"
    "Venom Spray" = "PER"
    "Blood Frenzy Flurry" = "AGI"
    "Dread Wave" = "WIL"
    "Goring Charge" = "MGT"
    "Grenade Burst" = "PER"
    "Seismic Slam" = "MGT"
    "Shrapnel Burst" = "PER"
    "Static Burst" = "PER"
    "Toxic Cloud" = "PER"
    "Final Eclipse" = "MGT"
    "Final Line" = "MGT"
    "Inferno Blast" = "MGT"
    "Inner Circle Pounce" = "SOC"
    "Inner Circle Surge" = "SOC"
    "Inner Circle Volley" = "SOC"
    "Inner Void" = "WIL"
    "Lockstep Crush" = "AGI"
    "Merciless Angle" = "SOC"
    "Rupturing Quake" = "MGT"
    "Scorching Breath" = "MGT"
}
foreach ($entry in $mimicryDamageScaling.GetEnumerator()) {
    $perkChanges += @{
        Sheet = "Mimicry"
        PerkName = $entry.Key
        Values = @{
            "Primary Stat" = $entry.Value
            "Scaling Source" = "Combat Formula"
        }
    }
}

$espionageDescriptions = @{
    "Stealth I" = "Enter stealth, increasing Stealth by 5 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat."
    "Stealth II" = "Enter stealth, increasing Stealth by 10 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat."
    "Stealth III" = "Enter stealth, increasing Stealth by 15 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat."
    "Stealth IV" = "Enter stealth, increasing Stealth by 20 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat."
    "Back Attack I" = "Melee weapon attacks from behind a target deal +3% damage."
    "Back Attack II" = "Melee weapon attacks from behind a target deal +5% damage and gain +3% Critical Rate."
    "Back Attack III" = "Melee weapon attacks from behind a target deal +8% damage and gain +5% Critical Rate."
    "Slicing I" = "Can open tier 1 lockboxes."
    "Slicing II" = "Can open tier 2 lockboxes."
    "Slicing III" = "Can open tier 3 lockboxes. Lockbox opening is 20% faster."
    "Slicing IV" = "Can open tier 4 lockboxes. Lockbox opening is 30% faster."
    "Slicing V" = "Can open tier 5 lockboxes. Lockbox opening is 40% faster."
    "Tactical Escape II" = "Reduces your enmity by 60%, removes negative movement-speed effects, and increases Evasion by 12% for 30 seconds."
    "Shadow Step II" = "Dash behind one hostile target within 5m, remove negative movement-speed effects, and increase Evasion by 15% for 30 seconds."
    "Silent Stride" = "While stealthed, increases Movement Speed by 30% and reduces STM drain by 20%, from 2 STM every 6 seconds to 2 STM every 7.5 seconds. Stealth still prevents running at full speed."
    "Ghost Protocol" = "Reduces your enmity by 80%, enters stealth for up to 30 seconds, and causes your next back attack within 30 seconds to critically hit and inflict Exposed, reducing Defense by 20% for 30 seconds."
    "Trapcraft III" = "Can craft, place, detect, and disarm tier 3 traps. Traps arm 20% faster, reducing their arming time from 3 seconds to 2.4 seconds."
    "Trapcraft IV" = "Can craft, place, detect, and disarm tier 4 traps. Traps arm 30% faster, reducing their arming time from 3 seconds to 2.1 seconds."
    "Venom Expertise I" = "Venom from weapon poisons you apply deals 10% more damage."
    "Venom Expertise II" = "Venom from weapon poisons you apply deals 20% more damage."
    "Razor Trap I" = "Places a visible trap that arms after 3 seconds. When triggered, enemies within its 3m blast take 14 physical DMG plus PER scaling and Bleed for 30 seconds."
    "Razor Trap II" = "Places a visible trap that arms after 3 seconds. When triggered, enemies within its 3m blast take 30 physical DMG plus PER scaling and Bleed for 30 seconds."
    "Shock Trap" = "Places a visible trap that arms after 3 seconds. When triggered, enemies within its 3m blast take 22 electrical DMG plus PER scaling and suffer Shock for 30 seconds."
    "Trap Management II" = "Increases maximum concurrent traps to 3 and trap detection range by 5m, from 6m to 11m."
    "Lasting Coatings" = "Weapon poison coatings you apply gain 50% more charges, increasing from 20 to 30 charges."
    "Master Saboteur" = "Can craft, place, detect, and disarm tier 5 traps. Your trap damage and weapon-poison Venom damage increase by 10%."
}

$alertnessDescriptions = @{
    "Alertness I" = "Increases Detection by 10, improving your chance to notice stealthed creatures."
    "Alertness II" = "Increases Detection by 15, improving your chance to notice stealthed creatures."
    "Alertness III" = "Increases Detection by 20, improving your chance to notice stealthed creatures."
}
foreach ($entry in $alertnessDescriptions.GetEnumerator()) {
    $perkChanges += @{
        Sheet = "Armor"
        PerkName = $entry.Key
        Values = @{ Description = $entry.Value }
    }
}
foreach ($entry in $espionageDescriptions.GetEnumerator()) {
    $values = @{ Description = $entry.Value }
    if ($entry.Key -match '^Stealth (I|II|III|IV)$') {
        $values['Type'] = 'Toggle'
        $values['Casting Time'] = '-'
        $values['Cooldown Time'] = '-'
    }
    $perkChanges += @{
        Sheet = "Espionage"
        PerkName = $entry.Key
        Values = $values
    }
}

$espionagePerks = @(
    "False Identities I", "Cover Story I", "False Identities II", "Cover Story II", "False Identities III",
    "Stealth I", "Back Attack I", "Slicing I", "Tactical Escape I", "Stealth II", "Back Attack II", "Slicing II",
    "Shadow Step I", "Stealth III", "Slicing III", "Silent Stride", "Tactical Escape II", "Back Attack III",
    "Stealth IV", "Slicing IV", "Shadow Step II", "Slicing V", "Ghost Protocol", "Poisoncraft I", "Trapcraft I",
    "Venom Expertise I", "Razor Trap I", "Poisoncraft II", "Trapcraft II", "Trap Management I", "Shock Trap",
    "Poisoncraft III", "Trapcraft III", "Lasting Coatings", "Venom Expertise II", "Razor Trap II", "Poisoncraft IV",
    "Trap Management II", "Trapcraft IV", "Poisoncraft V", "Master Saboteur"
)
foreach ($perkName in $espionagePerks) {
    $scalingSource = if ($perkName -in @("Razor Trap I", "Razor Trap II", "Shock Trap")) {
        "Combat Formula"
    }
    else {
        "None"
    }
    $perkChanges += @{
        Sheet = "Espionage"
        PerkName = $perkName
        Values = @{
            "Dev Status" = "Implemented"
            "Scaling Source" = $scalingSource
        }
    }
}

$sunderingSweepDescriptions = @{
    "Sundering Sweep I" = "Deals weapon DMG + 8 to enemies within 3m of you. If at least one struck enemy already has Sunder, spreads Sunder from that enemy to one other enemy in melee range for 30 seconds."
    "Sundering Sweep II" = "Deals weapon DMG + 12 to enemies within 3m of you. If at least one struck enemy already has Sunder, spreads Sunder from that enemy to one other enemy in melee range for 30 seconds."
    "Sundering Sweep III" = "Deals weapon DMG + 16 to enemies within 3m of you. If at least one struck enemy already has Sunder, spreads Sunder from that enemy to one other enemy in melee range for 30 seconds."
}
foreach ($entry in $sunderingSweepDescriptions.GetEnumerator()) {
    $perkChanges += @{
        Sheet = "Lightsaber"
        PerkName = $entry.Key
        Values = @{ Description = $entry.Value }
    }
}

$mimicryCooldowns = @{
    "Frost Spit" = 12
    "Raking Claws" = 12
    "Toxic Spit" = 12
    "Barbed Volley" = 18
    "Brutal Bash" = 15
    "Capacitor Surge" = 18
    "Concussive Challenge" = 18
    "Pouncing Strike" = 15
    "Savage Roar" = 18
    "Sonic Shriek" = 18
    "Static Web" = 18
    "Suppressing Shot" = 18
    "Arc Pulse" = 24
    "Blood Frenzy Flurry" = 24
    "Brace Breaker" = 18
    "Disorienting Screech" = 24
    "Goring Charge" = 24
    "Grenade Burst" = 24
    "Pack Harrier" = 18
    "Permafrost Rupture" = 24
    "Rally Breaker" = 18
    "Seismic Slam" = 24
    "Shrapnel Burst" = 24
    "Signal Snare" = 18
    "Static Burst" = 24
    "Crossfire Drill" = 30
    "Cryo Bile" = 30
    "Final Eclipse" = 30
    "Final Line" = 30
    "Final Mandate" = 30
    "Final Suppression" = 30
    "Finishing Drive" = 30
    "Holdfast Slam" = 24
    "Inferno Blast" = 30
    "Inner Circle Bind" = 24
    "Inner Circle Pounce" = 24
    "Inner Circle Surge" = 24
    "Inner Circle Volley" = 24
    "Inner Ring Flurry" = 24
    "Inner Void" = 24
    "Last Bastion" = 30
    "Lockstep Crush" = 30
    "Merciless Angle" = 30
    "Pressure Lock" = 30
    "Rupturing Quake" = 30
    "Scorching Breath" = 30
    "Snap Rush" = 30
    "Terrifying Bellow" = 30
    "Will Fracture" = 30
}
foreach ($entry in $mimicryCooldowns.GetEnumerator()) {
    $perkChanges += @{
        Sheet = "Mimicry"
        PerkName = $entry.Key
        Values = @{ "Cooldown Time" = "$($entry.Value) seconds" }
    }
}

$mimicryCastingTimes = @{
    "Final Mandate" = "1 second"
    "Finishing Drive" = "Instant"
    "Last Bastion" = "1 second"
    "Snap Rush" = "Instant"
    "Warden Order" = "1 second"
    "Warden Sweep" = "Instant"
}
foreach ($entry in $mimicryCastingTimes.GetEnumerator()) {
    $perkChanges += @{
        Sheet = "Mimicry"
        PerkName = $entry.Key
        Values = @{ "Casting Time" = $entry.Value }
    }
}

$characterStatChanges = @(
    @{ Cell = "J49"; Type = "Number"; Value = 100 },
    @{ Cell = "K49"; Type = "Text"; Value = "Default chance cap is 50%. Specific temporary or capstone effects can raise that cap, up to the 100% hard limit. Does not stack with Shield Deflection." },
    @{ Cell = "K50"; Type = "Text"; Value = "Guard chance is capped at 100%. A successful Guard reduces damage by 20% by default; bonuses can raise the reduction to a 40% hard limit." },
    @{ Cell = "I51"; Type = "Number"; Value = 5 },
    @{ Cell = "J51"; Type = "Number"; Value = 50 },
    @{ Cell = "K51"; Type = "Text"; Value = "Final critical-hit chance is clamped between 5% and 50% after all modifiers." },
    @{ Cell = "K52"; Type = "Text"; Value = "The combined Critical Damage percentage adjustment is capped at +200%." },
    @{ Cell = "K53"; Type = "Text"; Value = "The combined Enmity generation adjustment is clamped between -50% and +50%." },
    @{ Cell = "K54"; Type = "Text"; Value = "The combined attack-delay adjustment is capped at +50% Haste." },
    @{ Cell = "J55"; Type = "Number"; Value = 50 },
    @{ Cell = "K55"; Type = "Text"; Value = "The combined attack-delay adjustment is capped at -50%, so Slow cannot more than double the attack interval." },
    @{ Cell = "I57"; Type = "Number"; Value = 0 },
    @{ Cell = "K57"; Type = "Text"; Value = "Final movement-rate multiplier is clamped between 0 and 1.5. Uses NWNX Get/SetMovementRateFactor." },
    @{ Cell = "A66"; Type = "Text"; Value = "Damage-Derived Healing per Hit" },
    @{ Cell = "B66"; Type = "Text"; Value = "No" },
    @{ Cell = "C66"; Type = "Text"; Value = "Limits the combined HP restored by effects calculated from one hit's damage." },
    @{ Cell = "I66"; Type = "Number"; Value = 0 },
    @{ Cell = "J66"; Type = "Number"; Value = 50 },
    @{ Cell = "K66"; Type = "Text"; Value = "Hard cap applied after Combat Readiness and healing-received modifiers. Includes all percent-of-damage healing from the same hit. Excludes flat healing, maximum-HP healing, regeneration, healing over time, item healing, and other healing not calculated from hit damage." },
    @{ Cell = "A67"; Type = "Text"; Value = "Hit Rate" },
    @{ Cell = "B67"; Type = "Text"; Value = "No" },
    @{ Cell = "C67"; Type = "Text"; Value = "Chance for a weapon attack or hostile combat ability to hit after Accuracy, Evasion, and hit-rate modifiers." },
    @{ Cell = "I67"; Type = "Number"; Value = 20 },
    @{ Cell = "J67"; Type = "Number"; Value = 95 },
    @{ Cell = "K67"; Type = "Text"; Value = "Final hit rate is clamped between 20% and 95%." },
    @{ Cell = "A68"; Type = "Text"; Value = "Damage Bonus per Hit" },
    @{ Cell = "B68"; Type = "Text"; Value = "No" },
    @{ Cell = "C68"; Type = "Text"; Value = "Limits the combined increase from the normal damage-modifier stages of one hit." },
    @{ Cell = "I68"; Type = "Number"; Value = 0 },
    @{ Cell = "J68"; Type = "Number"; Value = 100 },
    @{ Cell = "K68"; Type = "Text"; Value = "Normal damage modifiers can add at most 100% of the damage entering those stages. This does not limit base damage or the earlier critical-damage calculation." },
    @{ Cell = "A69"; Type = "Text"; Value = "Single Damage Reduction Modifier" },
    @{ Cell = "B69"; Type = "Text"; Value = "No" },
    @{ Cell = "C69"; Type = "Text"; Value = "Limits how much one normal percentage damage modifier can reduce a positive hit." },
    @{ Cell = "I69"; Type = "Number"; Value = 0 },
    @{ Cell = "J69"; Type = "Number"; Value = 95 },
    @{ Cell = "K69"; Type = "Text"; Value = "A single normal damage-reduction modifier cannot reduce positive damage by more than 95%. Immunities and explicit damage-prevention effects are separate." },
    @{ Cell = "A70"; Type = "Text"; Value = "Combined Damage Reduction per Hit" },
    @{ Cell = "B70"; Type = "Text"; Value = "No" },
    @{ Cell = "C70"; Type = "Text"; Value = "Limits cumulative reduction during the target-status damage-modifier stage of one hit." },
    @{ Cell = "I70"; Type = "Number"; Value = 0 },
    @{ Cell = "J70"; Type = "Number"; Value = 85 },
    @{ Cell = "K70"; Type = "Text"; Value = "Damage leaving the target-status modifier stage cannot be reduced by more than 85% of the amount entering it. Immunities and explicit damage-prevention effects are separate." }
)

$auditSheetChanges = @(
    @{ Sheet = "Character Stats"; Cell = "K51"; Value = "All attacks and damaging abilities have a 5% base critical chance. Additive modifiers apply to that baseline, and the final chance is clamped between 5% and 50%." },
    @{ Sheet = "Character Stats"; Cell = "K60"; Value = "Opposed check: d20 + Stealth vs each observer's d20 + Detection, rolled once per 30 seconds per observer. Stealth = (AGI x 2) + equipment, perk, and status-effect bonuses." },
    @{ Sheet = "Character Stats"; Cell = "K61"; Value = "Counterpart to Stealth. Detection = PER + WIL plus equipment, perk, and status-effect bonuses; Detect mode grants an additional +5 while active." },
    @{ Sheet = "Status Effects"; Cell = "B208"; Value = "While active, all outgoing damage is increased by 10%. Using any hostile combat ability costs 2 additional STM and grants +8% Evasion for 30 seconds." },
    @{ Sheet = "Combat Balance Findings"; Cell = "F4"; Value = "All percent-of-damage healing produced by one hit is now pooled and capped at 50% of that hit's damage after Combat Readiness and healing-received modifiers." },
    @{ Sheet = "Combat Balance Findings"; Cell = "G4"; Value = "The multiplicative sustain ceiling is bounded; optimized Heavy Vibroblade and support combinations still need encounter retesting." },
    @{ Sheet = "Combat Balance Findings"; Cell = "H4"; Value = "Retest damage-plus-sustain archetypes with the shared per-hit healing cap; do not tune mobs around the former uncapped ceiling." },
    @{ Sheet = "Combat Balance Findings"; Cell = "J4"; Value = "Implemented - Retest" },
    @{ Sheet = "Combat Balance Findings"; Cell = "F5"; Value = "Spear Sweeping Flank and Vibroknife Backstab now deal useful baseline damage regardless of position; side/back positioning adds bonus damage or control. Cheap Shot also has status and stealth triggers." },
    @{ Sheet = "Combat Balance Findings"; Cell = "G5"; Value = "Low-positional uptime no longer removes the core ability payload; the positional ceiling still needs playtesting." },
    @{ Sheet = "Combat Balance Findings"; Cell = "H5"; Value = "Retest solo and party baselines, then validate that reliable positioning does not create the highest passive ceiling." },
    @{ Sheet = "Combat Balance Findings"; Cell = "J5"; Value = "Implemented - Retest" },
    @{ Sheet = "Combat Balance Findings"; Cell = "F6"; Value = "Spear Disabler now applies generic ability-cost pressure, FP and STM drains, control-based damage, interruption rewards, and Ability Accuracy reduction in addition to Force-oriented effects." },
    @{ Sheet = "Combat Balance Findings"; Cell = "G6"; Value = "The tree has meaningful effects against ordinary ability-using and stamina-using enemies; resource-less basic attackers remain a deliberate weak matchup." },
    @{ Sheet = "Combat Balance Findings"; Cell = "H6"; Value = "Retest against Force users, stamina users, and resource-less enemies to confirm the intended matchup spread." },
    @{ Sheet = "Combat Balance Findings"; Cell = "J6"; Value = "Implemented - Retest" },
    @{ Sheet = "Combat Balance Findings"; Cell = "F7"; Value = "Lightsaber Offense now has Guardian's Challenge at skill 12 and 32, an 8m x 3m hostile line ability, so its area cadence no longer waits for the capstone." },
    @{ Sheet = "Combat Balance Findings"; Cell = "G7"; Value = "The pre-capstone tree has a real hostile area surface; cadence and rider value still need playtesting." },
    @{ Sheet = "Combat Balance Findings"; Cell = "H7"; Value = "Retest Guardian's Challenge cadence and the Offense area interactions below skill 50." },
    @{ Sheet = "Combat Balance Findings"; Cell = "J7"; Value = "Implemented - Retest" },

    @{ Sheet = "Combat Fix Queue"; Cell = "E4"; Value = "Shared 50% per-hit damage-derived healing cap implemented; retest the optimized sustain ceiling." },
    @{ Sheet = "Combat Fix Queue"; Cell = "F4"; Value = "All percent-of-damage heals from one hit share a 50% cap after healing modifiers; high damage no longer produces uncapped multiplicative sustain." },
    @{ Sheet = "Combat Fix Queue"; Cell = "G4"; Value = "Implemented; flat, max-HP, regeneration, and healing-over-time effects intentionally remain outside this cap." },
    @{ Sheet = "Combat Fix Queue"; Cell = "E5"; Value = "Baseline payloads implemented; retest low- and high-positional uptime." },
    @{ Sheet = "Combat Fix Queue"; Cell = "F5"; Value = "Spear and Vibroknife core abilities remain useful without side/back access; positioning adds bonus damage or control." },
    @{ Sheet = "Combat Fix Queue"; Cell = "G5"; Value = "Implemented through Sweeping Flank, Backstab, and Cheap Shot's alternate status/stealth triggers." },
    @{ Sheet = "Combat Fix Queue"; Cell = "E6"; Value = "Generic STM, ability-cost, control, interruption, and Ability Accuracy hooks implemented; retest matchup spread." },
    @{ Sheet = "Combat Fix Queue"; Cell = "F6"; Value = "Spear Disabler contributes against common non-Force ability users while retaining stronger Force/resource pressure." },
    @{ Sheet = "Combat Fix Queue"; Cell = "G6"; Value = "Implemented; resource-less basic attackers remain an intentional weak matchup." },
    @{ Sheet = "Combat Fix Queue"; Cell = "E7"; Value = "Guardian's Challenge supplies a pre-capstone Offense area ability at skill 12 and 32; retest cadence." },
    @{ Sheet = "Combat Fix Queue"; Cell = "F7"; Value = "Offense area interactions have a real hostile trigger before the skill-50 capstone." },
    @{ Sheet = "Combat Fix Queue"; Cell = "G7"; Value = "Implemented; validate line targeting and rider value in play." },

    @{ Sheet = "Combat Archetypes"; Cell = "D4"; Value = "Staff-specific +2 MGT scaling, +5 Critical Rate, +10 Critical Damage, and 3 STM restored on a critical hit with a 6-second cooldown." },
    @{ Sheet = "Combat Archetypes"; Cell = "E4"; Value = "Staff Crusher remains attractive with a staff without becoming a mandatory passive package for every MGT weapon." },
    @{ Sheet = "Combat Archetypes"; Cell = "F4"; Value = "Requires staff use for its damage scaling; the smaller universal critical package retains cross-tree utility." },
    @{ Sheet = "Combat Archetypes"; Cell = "G4"; Value = "Resolved structurally; retest Staff and low-delay Staff builds." },
    @{ Sheet = "Combat Archetypes"; Cell = "D5"; Value = "Staff-specific Crusher scaling plus Heavy Vibroblade Life Siphon, Soul Ascension, Blood Weapon, and other percent-of-damage heals sharing a 50% per-hit cap." },
    @{ Sheet = "Combat Archetypes"; Cell = "E5"; Value = "High damage and sustain can still combine, but damage-derived healing can no longer scale without a shared ceiling." },
    @{ Sheet = "Combat Archetypes"; Cell = "F5"; Value = "Requires split Staff/Heavy Vibroblade investment, weapon switching, HP costs, and low-HP conditions." },
    @{ Sheet = "Combat Archetypes"; Cell = "G5"; Value = "Cap implemented; retest before mob tuning." },
    @{ Sheet = "Combat Archetypes"; Cell = "D6"; Value = "MGT weapon plus Leadership amplification; Staff Crusher damage scaling applies only while using a Staff." },
    @{ Sheet = "Combat Archetypes"; Cell = "E6"; Value = "Team support may still push optimized damage high, but the former cross-weapon Crusher multiplier is gone." },
    @{ Sheet = "Combat Archetypes"; Cell = "G6"; Value = "Retest party burst before mob tuning." },
    @{ Sheet = "Combat Archetypes"; Cell = "D10"; Value = "Foggy Mind cost pressure, FP and STM drain, interruption rewards, control-based damage, Force Disruption, and Ability Accuracy reduction." },
    @{ Sheet = "Combat Archetypes"; Cell = "E10"; Value = "Tree should contribute against ordinary ability and stamina users while remaining weaker against resource-less basic attackers." },
    @{ Sheet = "Combat Archetypes"; Cell = "G10"; Value = "Structural fix implemented; retest the matchup spread." },
    @{ Sheet = "Combat Archetypes"; Cell = "D11"; Value = "Guardian's Challenge supplies an 8m x 3m Offense line ability at skill 12 and 32, before the capstone." },
    @{ Sheet = "Combat Archetypes"; Cell = "E11"; Value = "Pre-capstone hostile area cadence and rider value." },
    @{ Sheet = "Combat Archetypes"; Cell = "G11"; Value = "Structural fix implemented; retest cadence and line targeting." },
    @{ Sheet = "Combat Archetypes"; Cell = "D12"; Value = "Sweeping Flank and Backstab retain baseline damage without position; Cheap Shot can trigger from incapacitating status, Stealth, or Invisibility." },
    @{ Sheet = "Combat Archetypes"; Cell = "E12"; Value = "Baseline remains functional when side/back uptime is low." },
    @{ Sheet = "Combat Archetypes"; Cell = "G12"; Value = "Structural fix implemented; protect the low-positional baseline in tuning." },
    @{ Sheet = "Combat Archetypes"; Cell = "D13"; Value = "Reliable side/back access adds Sweeping Flank bonus damage and Backstab bonus damage plus Knockdown." },
    @{ Sheet = "Combat Archetypes"; Cell = "E13"; Value = "Positional bonuses should reward setup without defining the baseline." },
    @{ Sheet = "Combat Archetypes"; Cell = "G13"; Value = "Retest the high-positional ceiling." }
)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Xml.Linq

function Read-ZipEntryText {
    param(
        [IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath
    )

    $entry = $Zip.GetEntry($EntryPath)
    if ($null -eq $entry) {
        throw "Workbook entry '$EntryPath' was not found."
    }

    $reader = [IO.StreamReader]::new($entry.Open())
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Get-WorkbookEntryPath {
    param([string]$Target)

    $normalized = $Target.Replace("\", "/")
    if ($normalized.StartsWith("/")) {
        return $normalized.TrimStart("/")
    }
    if ($normalized.StartsWith("xl/")) {
        return $normalized
    }
    return "xl/$normalized"
}

function Get-CellText {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [Collections.Generic.List[string]]$SharedStrings,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $typeAttribute = $Cell.Attribute("t")
    $type = if ($null -ne $typeAttribute) { $typeAttribute.Value } else { "" }
    if ($type -eq "inlineStr") {
        return (($Cell.Descendants($Namespace + "t") | ForEach-Object { $_.Value }) -join "")
    }

    $value = $Cell.Element($Namespace + "v")
    if ($null -eq $value) {
        return ""
    }
    if ($type -eq "s") {
        return $SharedStrings[[int]$value.Value]
    }
    return $value.Value
}

function Set-InlineCellText {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [string]$Value,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $Cell.SetAttributeValue("t", "inlineStr")
    $Cell.RemoveNodes()
    $text = [System.Xml.Linq.XElement]::new($Namespace + "t", $Value)
    if ($Value.Length -ne $Value.Trim().Length) {
        $text.SetAttributeValue([System.Xml.Linq.XNamespace]::Xml + "space", "preserve")
    }
    $inlineString = [System.Xml.Linq.XElement]::new($Namespace + "is")
    $inlineString.Add($text)
    $Cell.Add($inlineString)
}

function Set-NumericCellValue {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [double]$Value,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $Cell.SetAttributeValue("t", $null)
    $Cell.RemoveNodes()
    $Cell.Add([System.Xml.Linq.XElement]::new(
        $Namespace + "v",
        $Value.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture)))
}

function Get-OrCreateWorksheetCell {
    param(
        [System.Xml.Linq.XElement]$Row,
        [string]$CellReference,
        [System.Xml.Linq.XElement[]]$TemplateRows,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $cell = $Row.Elements($Namespace + "c") | Where-Object {
        $_.Attribute("r").Value -eq $CellReference
    } | Select-Object -First 1
    if ($null -ne $cell) {
        return $cell
    }

    $column = ([regex]::Match($CellReference, "^[A-Z]+")).Value
    $cell = [System.Xml.Linq.XElement]::new($Namespace + "c")
    $cell.SetAttributeValue("r", $CellReference)

    $templateCell = $TemplateRows.Elements($Namespace + "c") | Where-Object {
        $_.Attribute("r").Value -match "^$column\d+$"
    } | Select-Object -First 1
    if ($null -ne $templateCell -and $null -ne $templateCell.Attribute("s")) {
        $cell.SetAttributeValue("s", $templateCell.Attribute("s").Value)
    }

    $Row.Add($cell)
    return $cell
}

if ($EspionageStealthOnly) {
    $stealthPerkNames = @("Stealth I", "Stealth II", "Stealth III", "Stealth IV")
    $perkChanges = @($perkChanges | Where-Object {
        $_.Sheet -eq "Espionage" -and $_.PerkName -in $stealthPerkNames
    })
    $characterStatChanges = @()
    $auditSheetChanges = @()
}

if ($OnlyPerkName.Count -gt 0) {
    if ($EspionageStealthOnly) {
        throw "Use either -EspionageStealthOnly or -OnlyPerkName, not both."
    }

    $perkChanges = @($perkChanges | Where-Object { $_.PerkName -in $OnlyPerkName })
    $selectedPerkNames = @($perkChanges | ForEach-Object { $_.PerkName })
    $missingPerks = @($OnlyPerkName | Where-Object { $_ -notin $selectedPerkNames })
    if ($missingPerks.Count -gt 0) {
        throw "No declarative Bible correction exists for: $($missingPerks -join ', ')."
    }

    $characterStatChanges = @()
    $auditSheetChanges = @()
}

$tempWorkbookPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-combat-bible-{0}.xlsx" -f [guid]::NewGuid())
[IO.File]::Copy($workbookFullPath, $tempWorkbookPath, $false)
try {
    $zip = [IO.Compression.ZipFile]::Open($tempWorkbookPath, [IO.Compression.ZipArchiveMode]::Update)
    try {
        [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
        [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

    $relationships = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationships[$relationship.Id] = Get-WorkbookEntryPath $relationship.Target
    }

    $workbookNamespace = [Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    $sheetPaths = @{}
    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $sheetPaths[$sheet.GetAttribute("name")] = $relationships[$relationshipId]
    }

    $sharedStrings = [Collections.Generic.List[string]]::new()
    if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
        $sharedXml = [System.Xml.Linq.XDocument]::Parse((Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"))
        $sharedNamespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        foreach ($item in $sharedXml.Descendants($sharedNamespace + "si")) {
            $sharedStrings.Add((($item.Descendants($sharedNamespace + "t") | ForEach-Object { $_.Value }) -join ""))
        }
    }

        foreach ($sheetGroup in ($perkChanges | Group-Object { $_.Sheet })) {
        $sheetName = $sheetGroup.Name
        $worksheetName = if ($sheetName -eq "Armor") { "General" } else { $sheetName }
        if (-not $sheetPaths.ContainsKey($worksheetName)) {
            throw "Workbook sheet '$worksheetName' was not found."
        }

        $entryPath = $sheetPaths[$worksheetName]
        $worksheet = [System.Xml.Linq.XDocument]::Parse((Read-ZipEntryText -Zip $zip -EntryPath $entryPath), [System.Xml.Linq.LoadOptions]::PreserveWhitespace)
        $namespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        $rows = @($worksheet.Descendants($namespace + "row"))

        $headerRow = $null
        $headerColumns = @{}
        foreach ($row in $rows) {
            $candidate = @{}
            foreach ($cell in $row.Elements($namespace + "c")) {
                $reference = $cell.Attribute("r").Value
                $column = ([regex]::Match($reference, "^[A-Z]+")).Value
                $candidate[(Get-CellText -Cell $cell -SharedStrings $sharedStrings -Namespace $namespace)] = $column
            }
            if ($candidate.ContainsKey("Perk Name")) {
                $headerRow = $row
                $headerColumns = $candidate
                break
            }
        }
        if ($null -eq $headerRow) {
            throw "Sheet '$sheetName' does not contain a Perk Name header."
        }

        foreach ($change in $sheetGroup.Group) {
            $matchedRow = $null
            foreach ($row in $rows) {
                if ([int]$row.Attribute("r").Value -le [int]$headerRow.Attribute("r").Value) {
                    continue
                }
                $perkCell = $row.Elements($namespace + "c") | Where-Object {
                    $_.Attribute("r").Value -match "^$($headerColumns['Perk Name'])\d+$"
                } | Select-Object -First 1
                if ($null -ne $perkCell -and
                    (Get-CellText -Cell $perkCell -SharedStrings $sharedStrings -Namespace $namespace) -eq $change.PerkName) {
                    $matchedRow = $row
                    break
                }
            }
            if ($null -eq $matchedRow) {
                throw "Perk '$($change.PerkName)' was not found on sheet '$sheetName'."
            }

            $rowNumber = $matchedRow.Attribute("r").Value
            foreach ($field in $change.Values.Keys) {
                if (-not $headerColumns.ContainsKey($field)) {
                    throw "Column '$field' was not found on sheet '$sheetName'."
                }
                $cellReference = "$($headerColumns[$field])$rowNumber"
                $cell = $matchedRow.Elements($namespace + "c") | Where-Object {
                    $_.Attribute("r").Value -eq $cellReference
                } | Select-Object -First 1
                if ($null -eq $cell) {
                    throw "Cell '$cellReference' was not found on sheet '$sheetName'."
                }
                Set-InlineCellText -Cell $cell -Value ([string]$change.Values[$field]) -Namespace $namespace
            }
        }

        $existingEntry = $zip.GetEntry($entryPath)
        $existingEntry.Delete()
        $replacement = $zip.CreateEntry($entryPath, [IO.Compression.CompressionLevel]::Optimal)
        $stream = $replacement.Open()
        try {
            $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
            try {
                $worksheet.Save($writer, [System.Xml.Linq.SaveOptions]::DisableFormatting)
            }
            finally {
                $writer.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
        }

        $statsSheetName = "Character Stats"
        if (-not $sheetPaths.ContainsKey($statsSheetName)) {
            throw "Workbook sheet '$statsSheetName' was not found."
        }

        $statsEntryPath = $sheetPaths[$statsSheetName]
        $statsWorksheet = [System.Xml.Linq.XDocument]::Parse(
            (Read-ZipEntryText -Zip $zip -EntryPath $statsEntryPath),
            [System.Xml.Linq.LoadOptions]::PreserveWhitespace)
        $statsNamespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        $statsRows = @($statsWorksheet.Descendants($statsNamespace + "row"))
        $templateRows = @($statsRows | Where-Object {
            [int]$_.Attribute("r").Value -in @(64, 65)
        })

        foreach ($change in $characterStatChanges) {
            $rowNumber = [int]([regex]::Match($change.Cell, "\d+$").Value)
            $row = $statsRows | Where-Object {
                [int]$_.Attribute("r").Value -eq $rowNumber
            } | Select-Object -First 1
            if ($null -eq $row) {
                throw "Row '$rowNumber' was not found on sheet '$statsSheetName'."
            }

            $cell = Get-OrCreateWorksheetCell `
                -Row $row `
                -CellReference $change.Cell `
                -TemplateRows $templateRows `
                -Namespace $statsNamespace
            if ($change.Type -eq "Number") {
                Set-NumericCellValue -Cell $cell -Value ([double]$change.Value) -Namespace $statsNamespace
            }
            else {
                Set-InlineCellText -Cell $cell -Value ([string]$change.Value) -Namespace $statsNamespace
            }
        }

        $existingStatsEntry = $zip.GetEntry($statsEntryPath)
        $existingStatsEntry.Delete()
        $statsReplacement = $zip.CreateEntry($statsEntryPath, [IO.Compression.CompressionLevel]::Optimal)
        $statsStream = $statsReplacement.Open()
        try {
            $statsWriter = [IO.StreamWriter]::new($statsStream, [Text.UTF8Encoding]::new($false))
            try {
                $statsWorksheet.Save($statsWriter, [System.Xml.Linq.SaveOptions]::DisableFormatting)
            }
            finally {
                $statsWriter.Dispose()
            }
        }
        finally {
            $statsStream.Dispose()
        }

        foreach ($sheetGroup in ($auditSheetChanges | Group-Object { $_.Sheet })) {
            $auditSheetName = $sheetGroup.Name
            if (-not $sheetPaths.ContainsKey($auditSheetName)) {
                throw "Workbook sheet '$auditSheetName' was not found."
            }

            $auditEntryPath = $sheetPaths[$auditSheetName]
            $auditWorksheet = [System.Xml.Linq.XDocument]::Parse(
                (Read-ZipEntryText -Zip $zip -EntryPath $auditEntryPath),
                [System.Xml.Linq.LoadOptions]::PreserveWhitespace)
            $auditNamespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"

            foreach ($change in $sheetGroup.Group) {
                $cell = $auditWorksheet.Descendants($auditNamespace + "c") |
                    Where-Object { $_.Attribute("r").Value -eq $change.Cell } |
                    Select-Object -First 1
                if ($null -eq $cell) {
                    throw "Cell '$($change.Cell)' was not found on sheet '$auditSheetName'."
                }

                Set-InlineCellText -Cell $cell -Value ([string]$change.Value) -Namespace $auditNamespace
            }

            $existingAuditEntry = $zip.GetEntry($auditEntryPath)
            $existingAuditEntry.Delete()
            $auditReplacement = $zip.CreateEntry($auditEntryPath, [IO.Compression.CompressionLevel]::Optimal)
            $auditStream = $auditReplacement.Open()
            try {
                $auditWriter = [IO.StreamWriter]::new($auditStream, [Text.UTF8Encoding]::new($false))
                try {
                    $auditWorksheet.Save($auditWriter, [System.Xml.Linq.SaveOptions]::DisableFormatting)
                }
                finally {
                    $auditWriter.Dispose()
                }
            }
            finally {
                $auditStream.Dispose()
            }
        }
    }
    finally {
        $zip.Dispose()
    }

    [IO.File]::Copy($tempWorkbookPath, $workbookFullPath, $true)
}
finally {
    if ([IO.File]::Exists($tempWorkbookPath)) {
        [IO.File]::Delete($tempWorkbookPath)
    }
}

Write-Host "Applied $($perkChanges.Count) perk corrections, $($characterStatChanges.Count) Character Stats corrections, and $($auditSheetChanges.Count) audit-sheet corrections."
