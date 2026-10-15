namespace SWLOR.Game.Server.Service.KeyItemService
{
    public enum KeyItemType
    {
        [KeyItem(KeyItemCategoryType.Invalid, "Invalid", false, "")]
        Invalid = 0,
        [KeyItem(KeyItemCategoryType.QuestItems, "Avix Tatham's Work Receipt", true, "You received this work receipt from Avix Tatham, mining coordinator on CZ-220.")]
        AvixTathamsWorkReceipt = 1,
        [KeyItem(KeyItemCategoryType.QuestItems, "Halron Linth's Work Receipt", true, "You received this work receipt from Halron Linth, security officer on CZ-220.")]
        HalronLinthsWorkReceipt = 2,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crafting Terminal Droid Operator's Work Receipt", true, "You received this work receipt from the Crafting Terminal Droid Operator on CZ-220.")]
        CraftingTerminalDroidOperatorsWorkReceipt = 3,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crafting Terminal Droid Operator's Work Order", true, "This is a work order you received from the droid responsible for item construction on CZ-220. Obtain the item(s) requested and return them to him.")]
        CraftingTerminalDroidOperatorsWorkOrder = 4,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Shuttle Pass", true, "This shuttle pass enables you to travel between CZ-220 and planet Viscara.")]
        CZ220ShuttlePass = 5,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Experiment Room Key", true, "This unlocks the door leading to the experiment room, where the Colicoid should be located.")]
        CZ220ExperimentRoomKey = 6,
        [KeyItem(KeyItemCategoryType.Keys, "Mandalorian Facility Key", true, "This key unlocks the door to the Mandalorian facility in the Viscara Wildlands.")]
        MandalorianFacilityKey = 7,
        [KeyItem(KeyItemCategoryType.Keys, "Yellow Key Card", true, "This yellow key card can be used somewhere in the Mandalorian facility on Viscara.")]
        YellowKeyCard = 8,
        [KeyItem(KeyItemCategoryType.Keys, "Red Key Card", true, "This red key card can be used somewhere in the Mandalorian facility on Viscara.")]
        RedKeyCard = 9,
        [KeyItem(KeyItemCategoryType.Keys, "Blue Key Card", true, "This blue key card can be used somewhere in the Mandalorian facility on Viscara.")]
        BlueKeyCard = 10,
        [KeyItem(KeyItemCategoryType.QuestItems, "Slicing Program", true, "A data disc with a program used to slice the terminals in the Mandalorian facility.")]
        SlicingProgram = 11,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #1", true, "The first disc containing data on the Mandalorian Facility.")]
        DataDisc1 = 12,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #2", true, "The second disc containing data on the Mandalorian Facility.")]
        DataDisc2 = 13,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #3", true, "The third disc containing data on the Mandalorian Facility.")]
        DataDisc3 = 14,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #4", true, "The fourth disc containing data on the Mandalorian Facility.")]
        DataDisc4 = 15,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #5", true, "The fifth disc containing data on the Mandalorian Facility.")]
        DataDisc5 = 16,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #6", true, "The sixth disc containing data on the Mandalorian Facility.")]
        DataDisc6 = 17,
        [KeyItem(KeyItemCategoryType.QuestItems, "Package for Denam Reyholm", true, "Roy Moss gave you this package to deliver to Denam Reyholm.")]
        PackageForDenamReyholm = 18,
        [KeyItem(KeyItemCategoryType.Documents, "Old Tome", true, "A man known only as \"L\" gave you this tome. It's very old and the words have faded.")]
        OldTome = 19,
        [KeyItem(KeyItemCategoryType.Keys, "Coxxion Base Key", true, "This key will unlock the doors to the Coxxion base located in the deep mountains of Viscara.")]
        CoxxionBaseKey = 20,
        [KeyItem(KeyItemCategoryType.Keys, "Taxi Hailing Device", true, "This device will enable you to call upon a taxi to quickly transport you across a region.")]
        TaxiHailingDevice = 21,

        [KeyItem(KeyItemCategoryType.Maps, "CZ-220 - Maintenance Level Map", true, "Map of the CZ-220 Maintenance Level.")]
        CZ220MaintenanceLevelMap = 22,
        [KeyItem(KeyItemCategoryType.Maps, "CZ-220 - Offices & Labs Map", true, "Map of the CZ-220 Offices & Labs.")]
        CZ220OfficesAndLabsMap = 23,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Outpost Map", true, "Map of the Hutlar Outpost.")]
        HutlarOutpostMap = 24,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Qion Tundra Map", true, "Map of Qion Tundra.")]
        QionTundraMap = 25,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Qion Valley Map", true, "Map of Qion Valley.")]
        QionValleyMap = 26,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Facility Map", true, "Map of the Coral Isles Facility.")]
        CoralIslesFacilityMap = 27,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Inner Map", true, "Map of the Inner Coral Isles.")]
        CoralIslesInnerMap = 28,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Outer Map", true, "Map of the Outer Coral Isles.")]
        CoralIslesOuterMap = 29,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - The 'Elite' Hotel Map", true, "Map of the 'Elite' Hotel.")]
        EliteHotelMap = 30,

        [KeyItem(KeyItemCategoryType.Maps, "Hutlar Orbit Map", true, "Map of the space surrounding Hutlar.")]
        HutlarOrbitMap = 31,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala Orbit Map", true, "Map of the space surrounding Mon Cala.")]
        MonCalaOrbitMap = 32,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine Orbit Map", true, "Map of the space surrounding Tatooine.")]
        TatooineOrbitMap = 33,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara Orbit Map", true, "Map of the space surrounding Viscara and CZ-220.")]
        ViscaraOrbitMap = 34,

        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Anchorhead Map", true, "Map of Anchorhead.")]
        AnchorheadMap = 35,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Desert Map", true, "Map of the Tatooine deserts.")]
        TatooineDesertMap = 36,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Mos Esper Map", true, "Map of Mos Esper.")]
        MosEsperMap = 37,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Tusken Raider Cave Map", true, "Map of Tusken Raider cave.")]
        TuskenRaiderCaveMap = 38,

        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Cavern Map", true, "Map of Viscara caverns.")]
        ViscaraCavernMap = 39,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Veles Colony Map", true, "Map of Veles Colony.")]
        VelesColonyMap = 40,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Coxxion Base Map", true, "Map of the Coxxion Base.")]
        CoxxionBaseMap = 41,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Deep Mountains Map", true, "Map of the Deep Mountains.")]
        DeepMountainsMap = 42,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Deepwoods Map", true, "Map of the Deepwoods.")]
        DeepwoodsMap = 43,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Druzer Map", true, "Map of Druzer.")]
        DruzerMap = 44,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Swamplands Map", true, "Map of Swamplands.")]
        ViscaraSwamplandsMap = 45,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Jedi Temple Map", true, "Map of the Viscara Jedi Temple.")]
        ViscaraJediTempleMap = 46,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Wildlands Map", true, "Map of the Wildlands.")]
        WildlandsMap = 47,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Mandalorian Facility Map", true, "Map of the Mandalorian Facility.")]
        MandalorianFacilityMap = 48,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Mountain Valley Map", true, "Map of the Mountain Valley.")]
        MountainValleyMap = 49,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Republic Base Map", true, "Map of the Viscara Republic Base.")]
        ViscaraRepublicBaseMap = 50,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Sith Lake Outpost Map", true, "Map of the Viscara Sith Lake Outpost.")]
        SithLakeOutpostMap = 51,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Sewers Map", true, "Map of the Viscara Sewers.")]
        ViscaraSewersMap = 52,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Wildwoods Map", true, "Map of the Wildwoods.")]
        WildwoodsMap = 53,

        [KeyItem(KeyItemCategoryType.Maps, "Korriban Orbit Map", true, "Map of the space surrounding Korriban.")]
        KorribanOrbitMap = 54,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Wastelands Map", true, "Map of the wastelands on Korriban.")]
        KorribanWastelandsMap = 55,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Sith Crypt Map", true, "Map of the Sith crypt on Korriban.")]
        KorribanSithCryptMap = 56,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Caverns Map", true, "Map of the caverns on Korriban.")]
        KorribanCavernsMap = 57,

        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sunkenhead Swamps Map", true, "Map of the Sunkenhead Swamps on Mon Cala.")]
        MonCalaSunkenhedgeSwampsMap = 58,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sharptooth Jungles Map", true, "Map of the Sharptooth Jungles on Mon Cala.")]
        MonCalaSharptoothJunglesMap = 59,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sharptooth Caverns Map", true, "Map of the Sharptooth Caverns on Mon Cala.")]
        MonCalaSharptoothCavernsMap = 60,

        [KeyItem(KeyItemCategoryType.Maps, "Dathomir Orbit Map", true, "Map of the space surrounding Dathomir.")]
        DathomirOrbitMap = 61,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Cave Ruins Map", true, "Map of the Cave Ruins on Dathomir.")]
        DathomirCaveRuinsMap = 62,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Desert Map", true, "Map of the Deserts on Dathomir.")]
        DathomirDesertMap = 63,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Grottos Map", true, "Map of the Grottos on Dathomir.")]
        DathomirGrottosMap = 64,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Grotto Caverns Map", true, "Map of the Grotto Caverns on Dathomir.")]
        DathomirGrottoCavernsMap = 65,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Jungles Map", true, "Map of the Jungles on Dathomir.")]
        DathomirJunglesMap = 66,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Mountains Map", true, "Map of the Mountain region on Dathomir.")]
        DathomirMountainsMap = 67,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Ruins Base Map", true, "Map of the Ruins Base on Dathomir.")]
        DathomirRuinsBaseMap = 68,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Tribes Village Map", true, "Map of the Tribes Village on Dathomir.")]
        DathomirTribeVillageMap = 69,

        [KeyItem(KeyItemCategoryType.Maps, "Dantooine Orbit Map", true, "Map of the space surrounding Dantooine.")]
        DantooineOrbitMap = 70,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Lake Map", true, "Map of the Lake on Dantooine.")]
        DantooineLakeMap = 71,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Kinrath Cave Map", true, "Map of the Kinrath Tunnels on Dantooine.")]
        DantooineKinrathMap = 72,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Tribal Map", true, "Map of the South Plains Tribes on Dantooine.")]
        DantooineTribalMap = 73,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Forsaken Jungles Map", true, "Map of the Forsaken Jungles Caverns on Dantooine.")]
        DantooineForsakenJungleMap = 74,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Mountain Jungles Map", true, "Map of the Mountain Jungles on Dantooine.")]
        DantooineMountainJunglesMap = 75,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Crystal Cave Map", true, "Map of the Crystal Cave on Dantooine.")]
        DantooineCrystalCaveMap = 76,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Abandoned Warehouse Map", true, "Map of the Abandoned Warehouse Base on Dantooine.")]
        DantooineWarehouseMap = 77,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Canyon River Map", true, "Map of the Canyon Rivers on Dantooine.")]
        DantooineCanyonRiverMap = 78,
        [KeyItem(KeyItemCategoryType.QuestItems, "Shovel for cave", true, "You've been given a shovel to clear the rock near the lake cave.")]
        DantooineShovel = 79,
        [KeyItem(KeyItemCategoryType.Keys, "Viscara Lake Basement Key", true, "This key allows you to enter the super secret sith basement! Shhhhhh!")]
        SithBasementKey = 80,
        [KeyItem(KeyItemCategoryType.QuestItems, "Smuggler Pass", true, "You've been given a key pass to Nar Shaddaa.")]
        SmugglerPass = 86,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon Orbit Map", true, "Map of the space surrounding Smuggler's Moon.")]
        SmugglersMoonOrbitMap = 87,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Casino Map", true, "Map of the Casino on Smuggler's Moon.")]
        SmugglersMoonCasinoMap = 88,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Catwalks Map", true, "Map of the Catwalks on Smuggler's Moon.")]
        SmugglersMoonCatwalksMap = 89,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Corporate District Map", true, "Map of the Corporate District on Smuggler's Moon.")]
        SmugglersMoonCorporateDistrictMap = 90,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Czerka Arms Map", true, "Map of Czerka Arms on Smuggler's Moon.")]
        SmugglersMoonCzerkaArmsMap = 91,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Czerka Shipyard Office Map", true, "Map of the Czerka Shipyard Office on Smuggler's Moon.")]
        SmugglersMoonCzerkaShipyardOfficeMap = 92,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Fabrication Facility Map", true, "Map of the Fabrication Facility on Smuggler's Moon.")]
        SmugglersMoonFabricationFacilityMap = 93,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Fight Club Map", true, "Map of the Fight Club on Smuggler's Moon.")]
        SmugglersMoonFightClubMap = 94,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - GSI Base Map", true, "Map of the GSI Base on Smuggler's Moon.")]
        SmugglersMoonGSIBaseMap = 95,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Hyper Dive Cantina Map", true, "Map of the Hyper Dive Cantina on Smuggler's Moon.")]
        SmugglersMoonHyperDiveCantinaMap = 96,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Landing Pads Map", true, "Map of the Landing Pads on Smuggler's Moon.")]
        SmugglersMoonLandingPadsMap = 97,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Medshed Map", true, "Map of the Medshed on Smuggler's Moon.")]
        SmugglersMoonMedshedMap = 98,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Promenade Map", true, "Map of the Promenade on Smuggler's Moon.")]
        SmugglersMoonPromenadeMap = 99,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Sewers Map", true, "Map of the Sewers on Smuggler's Moon.")]
        SmugglersMoonSewersMap = 100,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Shipping District Map", true, "Map of the Shipping District on Smuggler's Moon.")]
        SmugglersMoonShippingDistrictMap = 101,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - The Hub Map", true, "Map of The Hub on Smuggler's Moon.")]
        SmugglersMoonTheHubMap = 102,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - The Slums Map", true, "Map of The Slums on Smuggler's Moon.")]
        SmugglersMoonTheSlumsMap = 103,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - The Tilted Visor Map", true, "Map of The Tilted Visor on Smuggler's Moon.")]
        SmugglersMoonTiltedVisorMap = 104,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon Station - Abandoned Station Map", true, "Map of the Abandoned Station on Smuggler's Moon Station.")]
        SmugglersMoonStationAbandonedStationMap = 105,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon Station - Lower Level Map", true, "Map of the Lower Level on Smuggler's Moon Station.")]
        SmugglersMoonStationLowerLevelMap = 106,
        [KeyItem(KeyItemCategoryType.Keys, "Viscara Sewers Depths Key", true, "Sera Vonn gave you this key to the sealed Viscara Sewers Depths.")]
        ViscaraSewersDepthsKey = 81,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Vein Codex", true, "Sera Vonn's Blood Frenzy codex, recovered from the Viscara Sewers Depths scavengers.")]
        BloodFrenzyRedVeinCodex = 82,
        [KeyItem(KeyItemCategoryType.QuestItems, "Pulse Metronome", true, "A timing core recovered from the Pulse-Frame Training Droids in the Viscara Sewers Depths.")]
        BloodFrenzyPulseMetronome = 83,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adrenal Glass", true, "A shard of clotted adrenal glass taken after defeating the Blood Frenzy Butcher in the Viscara Sewers Depths.")]
        BloodFrenzyAdrenalGlass = 84,
        [KeyItem(KeyItemCategoryType.QuestItems, "Blood Frenzy Charm Fragments", true, "Fragments recovered from the Blood Frenzy Duelists in the Viscara Sewers Depths.")]
        BloodFrenzyCharmFragments = 85,
        [KeyItem(KeyItemCategoryType.Keys, "Veles Militia Annex Key", true, "A militia keycard stamped with the Veles garrison seal. It opens the sealed door to the Veles Militia Annex, deep inside the Sheriff's Office in Veles on Viscara.")]
        CapstoneVelesMilitiaAnnexKey = 107,
        [KeyItem(KeyItemCategoryType.Keys, "Hutlar Jedi Trial Halls Key", true, "A worn training chit etched with a Jedi sigil. It opens the blast door of the concrete entrance block on the east side of the ruin at the north end of the Cloning Test Site on Hutlar, the way down to the hidden Jedi Trial Halls.")]
        CapstoneHutlarJediTrialHallsKey = 108,
        [KeyItem(KeyItemCategoryType.Keys, "Tatooine Forge Caverns Key", true, "A heavy iron key, its bit shaped like a forge glyph. It opens the old mine tunnel entrance in the canyon just inside the North Mine Cliffs on Tatooine, as you come in from the Rocky Desert.")]
        CapstoneTatooineForgeCavernsKey = 109,
        [KeyItem(KeyItemCategoryType.Keys, "Smuggler's Moon Fight Club Backrooms Key", true, "A greasy door code scratched onto a betting chit. It opens the Back Rooms door on the far west wall of the Fight Club, below the Slums on Smuggler's Moon.")]
        CapstoneSmugglersMoonFightClubBackroomsKey = 110,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Breaker Yard Key", true, "A Czerka maintenance override card with a cracked holo-stripe. It drops the force-field wall into the Breaker Bay in the far north-east corner of the CZ-220 Maintenance Level.")]
        CapstoneCZ220BreakerYardKey = 111,
        [KeyItem(KeyItemCategoryType.Keys, "Anchorhead Canyon Range Key", true, "A sand-scoured range pass on a leather cord. It opens the sealed door to the Anchorhead Canyon Range in the Rocky Pass on Tatooine, a short walk south of where the trail from Anchorhead's Southern Entrance comes in.")]
        CapstoneAnchorheadCanyonRangeKey = 112,
        [KeyItem(KeyItemCategoryType.Keys, "Czerka Arms Test Range Key", true, "A Czerka range clearance card bearing a test-division stamp. It opens the Czerka Weapons Testing Facility door in the Corporate District on Smuggler's Moon, a short walk south of the GSI Tower doors.")]
        CapstoneCzerkaArmsTestRangeKey = 113,
        [KeyItem(KeyItemCategoryType.Keys, "Hutlar Qion Test Site Key", true, "A frost-scarred site key with a blast-safety tag still wired to it. It opens the sealed Qion Test Site entrance in the Qion Box Canyon on Hutlar, south of the road up to Fort Ka'ra.")]
        CapstoneHutlarQionTestSiteKey = 114,
        [KeyItem(KeyItemCategoryType.Keys, "Mon Cala Sith Crypt Depths Key", true, "A cold, rune-cut key of dark stone. It opens the sealed door in the old tomb wall on the north side of the first cavern in the Sharptooth Jungle Caves on Mon Cala.")]
        CapstoneMonCalaSithCryptDepthsKey = 115,
        [KeyItem(KeyItemCategoryType.Keys, "Viscara Republic Engineering Bunker Key", true, "A Republic engineering access card with a cracked corps insignia. It opens the sealed door of a weathered concrete bunker half-sunk in the Western Swamplands on Viscara, a short walk west of where you come in from the Eastern Swamplands.")]
        CapstoneViscaraRepublicEngineeringBunkerKey = 116,
        [KeyItem(KeyItemCategoryType.Keys, "Mon Cala Medical Sublevel Key", true, "A medical keycard, its clip stained with old kolto. It works the service lift in the red-walled bay just east of the office block in the Coral Isles Facility on Mon Cala, past the sliding door off the entry hall, the way down to the Medical Sublevel.")]
        CapstoneMonCalaMedicalSublevelKey = 117,
        [KeyItem(KeyItemCategoryType.Keys, "Dathomir Tarn Jungle Preserve Key", true, "A carved hide tag strung with a preserve warden's knot. It opens the sealed gate to the Tarn Jungle Preserve in the north-east part of the Tarnished Jungles on Dathomir.")]
        CapstoneDathomirTarnJunglePreserveKey = 118,
        [KeyItem(KeyItemCategoryType.Keys, "Dathomir Grotto Apex Den Key", true, "A bone key wrapped in grotto moss and old rancor hide. It opens the way into the Grotto Apex Den through the dark cave mouth of the rock mound on the west side of the Grotto Caverns on Dathomir, beside the cave water.")]
        CapstoneDathomirGrottoApexDenKey = 119,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Veles Drill Ledger", true, "A militia drill ledger recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleVelesDrillLedger = 120,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Militia Range Relay", true, "A battered range-control relay recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleMilitiaRangeRelay = 121,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Scored Challenge Badge", true, "A scarred militia challenge badge recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleScoredChallengeBadge = 122,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Captain's Challenge Chit", true, "A captain's encrypted challenge chit recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleCaptainsChallengeChit = 123,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Veles Drill Ledger", true, "A militia drill ledger recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureVelesDrillLedger = 124,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Militia Range Relay", true, "A battered range-control relay recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureMilitiaRangeRelay = 125,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Scored Challenge Badge", true, "A scarred militia challenge badge recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureScoredChallengeBadge = 126,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Captain's Challenge Chit", true, "A captain's encrypted challenge chit recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureCaptainsChallengeChit = 127,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Veles Drill Ledger", true, "A militia drill ledger recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownVelesDrillLedger = 128,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Militia Range Relay", true, "A battered range-control relay recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownMilitiaRangeRelay = 129,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Scored Challenge Badge", true, "A scarred militia challenge badge recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownScoredChallengeBadge = 130,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Captain's Challenge Chit", true, "A captain's encrypted challenge chit recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownCaptainsChallengeChit = 131,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Enclave Trial Slate", true, "An enclave training slate recovered in the Hutlar Jedi Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormEnclaveTrialSlate = 132,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Kyber Focus Shard", true, "A humming kyber focus shard recovered in the Hutlar Jedi Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormKyberFocusShard = 133,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Fractured Trial Sigil", true, "A fractured Jedi trial sigil recovered in the Hutlar Jedi Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormFracturedTrialSigil = 134,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Council Trial Chit", true, "A sealed council trial chit recovered in the Hutlar Jedi Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormCouncilTrialChit = 135,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Enclave Trial Slate", true, "An enclave training slate recovered in the Hutlar Jedi Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterEnclaveTrialSlate = 136,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Kyber Focus Shard", true, "A humming kyber focus shard recovered in the Hutlar Jedi Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterKyberFocusShard = 137,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Fractured Trial Sigil", true, "A fractured Jedi trial sigil recovered in the Hutlar Jedi Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterFracturedTrialSigil = 138,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Council Trial Chit", true, "A sealed council trial chit recovered in the Hutlar Jedi Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterCouncilTrialChit = 139,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Enclave Trial Slate", true, "An enclave training slate recovered in the Hutlar Jedi Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneEnclaveTrialSlate = 140,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Kyber Focus Shard", true, "A humming kyber focus shard recovered in the Hutlar Jedi Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneKyberFocusShard = 141,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Fractured Trial Sigil", true, "A fractured Jedi trial sigil recovered in the Hutlar Jedi Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneFracturedTrialSigil = 142,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Council Trial Chit", true, "A sealed council trial chit recovered in the Hutlar Jedi Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneCouncilTrialChit = 143,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Forge Heat Ledger", true, "A heat-scarred forge ledger recovered in the Tatooine Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseForgeHeatLedger = 144,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Sith Tempering Matrix", true, "A volatile Sith tempering matrix recovered in the Tatooine Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseSithTemperingMatrix = 145,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Cracked Anvil Sigil", true, "A cracked forge-anvil sigil recovered in the Tatooine Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseCrackedAnvilSigil = 146,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Overseer's Clearance Token", true, "An overseer's clearance token recovered in the Tatooine Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseOverseersClearanceToken = 147,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Forge Heat Ledger", true, "A heat-scarred forge ledger recovered in the Tatooine Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionForgeHeatLedger = 148,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Sith Tempering Matrix", true, "A volatile Sith tempering matrix recovered in the Tatooine Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionSithTemperingMatrix = 149,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Cracked Anvil Sigil", true, "A cracked forge-anvil sigil recovered in the Tatooine Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionCrackedAnvilSigil = 150,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Overseer's Clearance Token", true, "An overseer's clearance token recovered in the Tatooine Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionOverseersClearanceToken = 151,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Forge Heat Ledger", true, "A heat-scarred forge ledger recovered in the Tatooine Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneForgeHeatLedger = 152,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Sith Tempering Matrix", true, "A volatile Sith tempering matrix recovered in the Tatooine Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneSithTemperingMatrix = 153,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Cracked Anvil Sigil", true, "A cracked forge-anvil sigil recovered in the Tatooine Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneCrackedAnvilSigil = 154,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Overseer's Clearance Token", true, "An overseer's clearance token recovered in the Tatooine Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneOverseersClearanceToken = 155,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Backroom Bout Ledger", true, "A coded backroom bout ledger recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefenseBackroomBoutLedger = 156,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Ring Shock Regulator", true, "A tampered ring shock regulator recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefenseRingShockRegulator = 157,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Cracked Pit Sigil", true, "A cracked pit-fighter sigil recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefenseCrackedPitSigil = 158,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Promoter's Payout Chit", true, "A promoter's hidden payout chit recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefensePromotersPayoutChit = 159,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Backroom Bout Ledger", true, "A coded backroom bout ledger recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomBackroomBoutLedger = 160,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Ring Shock Regulator", true, "A tampered ring shock regulator recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomRingShockRegulator = 161,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Cracked Pit Sigil", true, "A cracked pit-fighter sigil recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomCrackedPitSigil = 162,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Promoter's Payout Chit", true, "A promoter's hidden payout chit recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomPromotersPayoutChit = 163,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Backroom Bout Ledger", true, "A coded backroom bout ledger recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomBackroomBoutLedger = 164,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Ring Shock Regulator", true, "A tampered ring shock regulator recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomRingShockRegulator = 165,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Cracked Pit Sigil", true, "A cracked pit-fighter sigil recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomCrackedPitSigil = 166,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Promoter's Payout Chit", true, "A promoter's hidden payout chit recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomPromotersPayoutChit = 167,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Breaker Yard Work Order", true, "A grease-stained breaker yard work order recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardBreakerYardWorkOrder = 168,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Junkline Control Relay", true, "A sparking junkline control relay recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardJunklineControlRelay = 169,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Sheared Bay Sigil", true, "A sheared breaker bay sigil recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardShearedBaySigil = 170,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Foreman's Override Chip", true, "A foreman's override chip recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardForemansOverrideChip = 171,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Breaker Yard Work Order", true, "A grease-stained breaker yard work order recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownBreakerYardWorkOrder = 172,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Junkline Control Relay", true, "A sparking junkline control relay recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownJunklineControlRelay = 173,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Sheared Bay Sigil", true, "A sheared breaker bay sigil recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownShearedBaySigil = 174,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Foreman's Override Chip", true, "A foreman's override chip recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownForemansOverrideChip = 175,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Breaker Yard Work Order", true, "A grease-stained breaker yard work order recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerBreakerYardWorkOrder = 176,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Junkline Control Relay", true, "A sparking junkline control relay recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerJunklineControlRelay = 177,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Sheared Bay Sigil", true, "A sheared breaker bay sigil recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerShearedBaySigil = 178,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Foreman's Override Chip", true, "A foreman's override chip recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerForemansOverrideChip = 179,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Canyon Range Tally", true, "A sand-worn canyon range tally recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterCanyonRangeTally = 180,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Sightline Calibrator", true, "A heat-bleached sightline calibrator recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterSightlineCalibrator = 181,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Shattered Range Crest", true, "A shattered marksman range crest recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterShatteredRangeCrest = 182,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Marshal's Challenge Chit", true, "A marshal's range challenge chit recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterMarshalsChallengeChit = 183,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Canyon Range Tally", true, "A sand-worn canyon range tally recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordCanyonRangeTally = 184,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Sightline Calibrator", true, "A heat-bleached sightline calibrator recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordSightlineCalibrator = 185,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Shattered Range Crest", true, "A shattered marksman range crest recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordShatteredRangeCrest = 186,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Marshal's Challenge Chit", true, "A marshal's range challenge chit recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordMarshalsChallengeChit = 187,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Canyon Range Tally", true, "A sand-worn canyon range tally recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandCanyonRangeTally = 188,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Sightline Calibrator", true, "A heat-bleached sightline calibrator recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandSightlineCalibrator = 189,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Shattered Range Crest", true, "A shattered marksman range crest recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandShatteredRangeCrest = 190,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Marshal's Challenge Chit", true, "A marshal's range challenge chit recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandMarshalsChallengeChit = 191,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Czerka Test Docket", true, "A redacted Czerka test docket recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxCzerkaTestDocket = 192,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Blast-Cell Regulator", true, "A Czerka blast-cell regulator recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxBlastCellRegulator = 193,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Scored Range Crest", true, "A scored Czerka range crest recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxScoredRangeCrest = 194,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Czerka Clearance Chit", true, "A Czerka clearance chit recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxCzerkaClearanceChit = 195,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Czerka Test Docket", true, "A redacted Czerka test docket recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotCzerkaTestDocket = 196,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Blast-Cell Regulator", true, "A Czerka blast-cell regulator recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotBlastCellRegulator = 197,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Scored Range Crest", true, "A scored Czerka range crest recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotScoredRangeCrest = 198,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Czerka Clearance Chit", true, "A Czerka clearance chit recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotCzerkaClearanceChit = 199,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Czerka Test Docket", true, "A redacted Czerka test docket recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelCzerkaTestDocket = 200,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Blast-Cell Regulator", true, "A Czerka blast-cell regulator recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelBlastCellRegulator = 201,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Scored Range Crest", true, "A scored Czerka range crest recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelScoredRangeCrest = 202,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Czerka Clearance Chit", true, "A Czerka clearance chit recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelCzerkaClearanceChit = 203,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Qion Test Log", true, "A frost-cracked Qion test log recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurryQionTestLog = 204,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Cryo-Range Regulator", true, "A malfunctioning cryo-range regulator recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurryCryoRangeRegulator = 205,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Frostburned Test Crest", true, "A frostburned weapons test crest recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurryFrostburnedTestCrest = 206,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Site Chief's Override Chip", true, "A site chief's override chip recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurrySiteChiefsOverrideChip = 207,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Qion Test Log", true, "A frost-cracked Qion test log recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorQionTestLog = 208,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Cryo-Range Regulator", true, "A malfunctioning cryo-range regulator recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorCryoRangeRegulator = 209,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Frostburned Test Crest", true, "A frostburned weapons test crest recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorFrostburnedTestCrest = 210,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Site Chief's Override Chip", true, "A site chief's override chip recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorSiteChiefsOverrideChip = 211,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Qion Test Log", true, "A frost-cracked Qion test log recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageQionTestLog = 212,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Cryo-Range Regulator", true, "A malfunctioning cryo-range regulator recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageCryoRangeRegulator = 213,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Frostburned Test Crest", true, "A frostburned weapons test crest recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageFrostburnedTestCrest = 214,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Site Chief's Override Chip", true, "A site chief's override chip recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageSiteChiefsOverrideChip = 215,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Crypt Trial Tablet", true, "An etched crypt trial tablet recovered in the Mon Cala Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightCryptTrialTablet = 216,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Ritual Focus Shard", true, "A pulsing ritual focus shard recovered in the Mon Cala Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightRitualFocusShard = 217,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Splintered Tomb Sigil", true, "A splintered Sith tomb sigil recovered in the Mon Cala Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightSplinteredTombSigil = 218,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Keeper's Rite Token", true, "A crypt keeper's rite token recovered in the Mon Cala Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightKeepersRiteToken = 219,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Crypt Trial Tablet", true, "An etched crypt trial tablet recovered in the Mon Cala Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkCryptTrialTablet = 220,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Ritual Focus Shard", true, "A pulsing ritual focus shard recovered in the Mon Cala Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkRitualFocusShard = 221,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Splintered Tomb Sigil", true, "A splintered Sith tomb sigil recovered in the Mon Cala Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkSplinteredTombSigil = 222,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Keeper's Rite Token", true, "A crypt keeper's rite token recovered in the Mon Cala Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkKeepersRiteToken = 223,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Crypt Trial Tablet", true, "An etched crypt trial tablet recovered in the Mon Cala Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveCryptTrialTablet = 224,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Ritual Focus Shard", true, "A pulsing ritual focus shard recovered in the Mon Cala Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveRitualFocusShard = 225,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Splintered Tomb Sigil", true, "A splintered Sith tomb sigil recovered in the Mon Cala Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveSplinteredTombSigil = 226,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Keeper's Rite Token", true, "A crypt keeper's rite token recovered in the Mon Cala Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveKeepersRiteToken = 227,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Republic Bunker Docket", true, "A Republic bunker operations docket recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconRepublicBunkerDocket = 228,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Shield Grid Relay", true, "A Republic shield-grid relay recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconShieldGridRelay = 229,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Cracked Command Crest", true, "A cracked Republic command crest recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconCrackedCommandCrest = 230,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Quartermaster Override Chip", true, "A quartermaster override chip recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconQuartermasterOverrideChip = 231,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Republic Bunker Docket", true, "A Republic bunker operations docket recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerRepublicBunkerDocket = 232,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Shield Grid Relay", true, "A Republic shield-grid relay recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerShieldGridRelay = 233,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Cracked Command Crest", true, "A cracked Republic command crest recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerCrackedCommandCrest = 234,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Quartermaster Override Chip", true, "A quartermaster override chip recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerQuartermasterOverrideChip = 235,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Republic Bunker Docket", true, "A Republic bunker operations docket recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandRepublicBunkerDocket = 236,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Shield Grid Relay", true, "A Republic shield-grid relay recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandShieldGridRelay = 237,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Cracked Command Crest", true, "A cracked Republic command crest recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandCrackedCommandCrest = 238,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Quartermaster Override Chip", true, "A quartermaster override chip recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandQuartermasterOverrideChip = 239,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Triage Ward Ledger", true, "A triage ward ledger recovered in the Mon Cala Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineTriageWardLedger = 240,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Kolto Conduit Coupler", true, "A pressurized kolto conduit coupler recovered in the Mon Cala Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineKoltoConduitCoupler = 241,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Fractured Ward Sigil", true, "A fractured medical ward sigil recovered in the Mon Cala Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineFracturedWardSigil = 242,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Matron's Ward Token", true, "A matron's ward token recovered in the Mon Cala Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineMatronsWardToken = 243,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Triage Ward Ledger", true, "A triage ward ledger recovered in the Mon Cala Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailTriageWardLedger = 244,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Kolto Conduit Coupler", true, "A pressurized kolto conduit coupler recovered in the Mon Cala Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailKoltoConduitCoupler = 245,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Fractured Ward Sigil", true, "A fractured medical ward sigil recovered in the Mon Cala Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailFracturedWardSigil = 246,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Matron's Ward Token", true, "A matron's ward token recovered in the Mon Cala Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailMatronsWardToken = 247,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Triage Ward Ledger", true, "A triage ward ledger recovered in the Mon Cala Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitTriageWardLedger = 248,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Kolto Conduit Coupler", true, "A pressurized kolto conduit coupler recovered in the Mon Cala Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitKoltoConduitCoupler = 249,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Fractured Ward Sigil", true, "A fractured medical ward sigil recovered in the Mon Cala Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitFracturedWardSigil = 250,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Matron's Ward Token", true, "A matron's ward token recovered in the Mon Cala Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitMatronsWardToken = 251,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Tarn Hunt Tally", true, "A scratched tarn hunt tally recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBiteTarnHuntTally = 252,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Beast-Pen Scent Vial", true, "A pungent beast-pen scent vial recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBiteBeastPenScentVial = 253,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Clawed Alpha Totem", true, "A clawed alpha-beast totem recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBiteClawedAlphaTotem = 254,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Preserve Keeper's Token", true, "A preserve keeper's bone token recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBitePreserveKeepersToken = 255,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Tarn Hunt Tally", true, "A scratched tarn hunt tally recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastTarnHuntTally = 256,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Beast-Pen Scent Vial", true, "A pungent beast-pen scent vial recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastBeastPenScentVial = 257,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Clawed Alpha Totem", true, "A clawed alpha-beast totem recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastClawedAlphaTotem = 258,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Preserve Keeper's Token", true, "A preserve keeper's bone token recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastPreserveKeepersToken = 259,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Tarn Hunt Tally", true, "A scratched tarn hunt tally recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmTarnHuntTally = 260,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Beast-Pen Scent Vial", true, "A pungent beast-pen scent vial recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmBeastPenScentVial = 261,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Clawed Alpha Totem", true, "A clawed alpha-beast totem recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmClawedAlphaTotem = 262,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Preserve Keeper's Token", true, "A preserve keeper's bone token recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmPreserveKeepersToken = 263,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Grotto Track Slate", true, "A mud-darkened grotto track slate recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunGrottoTrackSlate = 264,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Resonant Fang Charm", true, "A resonant fang charm recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunResonantFangCharm = 265,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Cracked Apex Totem", true, "A cracked apex-beast totem recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunCrackedApexTotem = 266,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Den-Mother's Fang Token", true, "A den-mother's fang token recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunDenMothersFangToken = 267,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Grotto Track Slate", true, "A mud-darkened grotto track slate recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctGrottoTrackSlate = 268,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Resonant Fang Charm", true, "A resonant fang charm recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctResonantFangCharm = 269,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Cracked Apex Totem", true, "A cracked apex-beast totem recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctCrackedApexTotem = 270,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Den-Mother's Fang Token", true, "A den-mother's fang token recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctDenMothersFangToken = 271,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Grotto Track Slate", true, "A mud-darkened grotto track slate recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastGrottoTrackSlate = 272,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Resonant Fang Charm", true, "A resonant fang charm recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastResonantFangCharm = 273,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Cracked Apex Totem", true, "A cracked apex-beast totem recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastCrackedApexTotem = 274,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Den-Mother's Fang Token", true, "A den-mother's fang token recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastDenMothersFangToken = 275,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Droid Foundry Map", true, "Map of the droid foundry beneath the Abandoned Outpost on Hutlar.")]
        HutlarDroidFoundryMap = 276,
        [KeyItem(KeyItemCategoryType.Maps, "Abandoned Station Map", true, "Map of the Main Level, Restricted Level, and Director's Chambers of the Abandoned Station.")]
        AbandonedStationMap = 277,

        // Incubation field notes — one per mutation target beast. The declared Name is a
        // readable fallback; IncubationFieldNote injects the canonical Name and the full
        // requirement Description at boot from the live mutation configuration.
        [KeyItem(KeyItemCategoryType.FieldNotes, "Aardvark", true, "")]
        IncubationFieldNoteAardvark = 2000,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Abyssweb Ravager", true, "")]
        IncubationFieldNoteAbysswebRavager = 2001,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Allosaurus", true, "")]
        IncubationFieldNoteAllosaurus = 2002,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Amberhide Nimbrel", true, "")]
        IncubationFieldNoteAmberhideNimbrel = 2003,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Amethyst Selori", true, "")]
        IncubationFieldNoteAmethystSelori = 2004,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Armourback Spineguard", true, "")]
        IncubationFieldNoteArmourbackSpineguard = 2005,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ashen Moonprowler", true, "")]
        IncubationFieldNoteAshenMoonprowler = 2006,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Azurehorn Kargath", true, "")]
        IncubationFieldNoteAzurehornKargath = 2007,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Balanoro Force Mite", true, "")]
        IncubationFieldNoteBalanoroForceMite = 2008,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Basalt Gorgath", true, "")]
        IncubationFieldNoteBasaltGorgath = 2009,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bearbug", true, "")]
        IncubationFieldNoteBearbug = 2010,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bhalir", true, "")]
        IncubationFieldNoteBhalir = 2011,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Binarian Sabercat", true, "")]
        IncubationFieldNoteBinarianSabercat = 2012,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Blastail", true, "")]
        IncubationFieldNoteBlastail = 2013,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Blinkstep Vekara", true, "")]
        IncubationFieldNoteBlinkstepVekara = 2014,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Blistering Beetle", true, "")]
        IncubationFieldNoteBlisteringBeetle = 2015,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bloodtusk Ravor", true, "")]
        IncubationFieldNoteBloodtuskRavor = 2016,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Boma Beast", true, "")]
        IncubationFieldNoteBomaBeast = 2017,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Boma Beast Baby", true, "")]
        IncubationFieldNoteBomaBeastBaby = 2018,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bramble Lynx", true, "")]
        IncubationFieldNoteBrambleLynx = 2019,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Brassjaw Pyralisk", true, "")]
        IncubationFieldNoteBrassjawPyralisk = 2020,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bronzecrest Thundros", true, "")]
        IncubationFieldNoteBronzecrestThundros = 2021,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Burrowberry Bird", true, "")]
        IncubationFieldNoteBurrowberryBird = 2022,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Burrowberry Pack", true, "")]
        IncubationFieldNoteBurrowberryPack = 2023,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cannok", true, "")]
        IncubationFieldNoteCannok = 2024,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Char Hound", true, "")]
        IncubationFieldNoteCharHound = 2025,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cloudcall Aurelith", true, "")]
        IncubationFieldNoteCloudcallAurelith = 2026,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cobalt Hornwyrm", true, "")]
        IncubationFieldNoteCobaltHornwyrm = 2027,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Coppercoil Mirelisk", true, "")]
        IncubationFieldNoteCoppercoilMirelisk = 2028,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cragmane Valshar", true, "")]
        IncubationFieldNoteCragmaneValshar = 2029,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cragscale", true, "")]
        IncubationFieldNoteCragscale = 2030,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Crimson Skyrender", true, "")]
        IncubationFieldNoteCrimsonSkyrender = 2031,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Crocodile", true, "")]
        IncubationFieldNoteCrocodile = 2032,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Crystalflow Skimmer", true, "")]
        IncubationFieldNoteCrystalflowSkimmer = 2033,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dathomir Wyrmling", true, "")]
        IncubationFieldNoteDathomirWyrmling = 2034,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dawnfang Hound", true, "")]
        IncubationFieldNoteDawnfangHound = 2035,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Deeprock Mauler", true, "")]
        IncubationFieldNoteDeeprockMauler = 2036,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Deepstone Graxal", true, "")]
        IncubationFieldNoteDeepstoneGraxal = 2037,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Deepwoods Rager", true, "")]
        IncubationFieldNoteDeepwoodsRager = 2038,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dewback", true, "")]
        IncubationFieldNoteDewback = 2039,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Direfang Lupikar", true, "")]
        IncubationFieldNoteDirefangLupikar = 2040,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dreadmaw Barghest", true, "")]
        IncubationFieldNoteDreadmawBarghest = 2041,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dreamcap Myconite", true, "")]
        IncubationFieldNoteDreamcapMyconite = 2042,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dreamwalker", true, "")]
        IncubationFieldNoteDreamwalker = 2043,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Drexclaw Marauder", true, "")]
        IncubationFieldNoteDrexclawMarauder = 2044,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Duneshag Bantha", true, "")]
        IncubationFieldNoteDuneshagBantha = 2045,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Duskfang Hound", true, "")]
        IncubationFieldNoteDuskfangHound = 2046,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Duskmane Ursadon", true, "")]
        IncubationFieldNoteDuskmaneUrsadon = 2047,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Elderspore Oraculum", true, "")]
        IncubationFieldNoteEldersporeOraculum = 2048,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Emberback Bristal", true, "")]
        IncubationFieldNoteEmberbackBristal = 2049,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Emeraldcrest Kalyth", true, "")]
        IncubationFieldNoteEmeraldcrestKalyth = 2050,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Frog", true, "")]
        IncubationFieldNoteFrog = 2051,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Frostback Spineguard", true, "")]
        IncubationFieldNoteFrostbackSpineguard = 2052,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Frostmaw Glacieron", true, "")]
        IncubationFieldNoteFrostmawGlacieron = 2053,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Fungal Shambler", true, "")]
        IncubationFieldNoteFungalShambler = 2054,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Garral", true, "")]
        IncubationFieldNoteGarral = 2055,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Garu Bear Ripper", true, "")]
        IncubationFieldNoteGaruBearRipper = 2056,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Giant Garu Bear", true, "")]
        IncubationFieldNoteGiantGaruBear = 2057,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Gilded Mirewyrm", true, "")]
        IncubationFieldNoteGildedMirewyrm = 2058,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Glimmerwing Mykal", true, "")]
        IncubationFieldNoteGlimmerwingMykal = 2059,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Gloomthread Skiver", true, "")]
        IncubationFieldNoteGloomthreadSkiver = 2060,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Goldpelt Sahrak", true, "")]
        IncubationFieldNoteGoldpeltSahrak = 2061,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Graniteback Ursavar", true, "")]
        IncubationFieldNoteGranitebackUrsavar = 2062,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Graymire Amalgam", true, "")]
        IncubationFieldNoteGraymireAmalgam = 2063,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Greenbulk Wallow", true, "")]
        IncubationFieldNoteGreenbulkWallow = 2064,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Grutchin", true, "")]
        IncubationFieldNoteGrutchin = 2065,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Hanadak", true, "")]
        IncubationFieldNoteHanadak = 2066,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Horned Kath Hound", true, "")]
        IncubationFieldNoteHornedKathHound = 2067,
        [KeyItem(KeyItemCategoryType.FieldNotes, "House Cat", true, "")]
        IncubationFieldNoteHouseCat = 2068,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Hssiss", true, "")]
        IncubationFieldNoteHssiss = 2069,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Hutlar Penguin", true, "")]
        IncubationFieldNoteHutlarPenguin = 2070,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Icewing Kestrelith", true, "")]
        IncubationFieldNoteIcewingKestrelith = 2071,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ironmaw Bastionback", true, "")]
        IncubationFieldNoteIronmawBastionback = 2072,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Jadeclaw Vyrkol", true, "")]
        IncubationFieldNoteJadeclawVyrkol = 2073,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Juvenile Chirodactyl", true, "")]
        IncubationFieldNoteJuvenileChirodactyl = 2074,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Juvenile Rancor", true, "")]
        IncubationFieldNoteJuvenileRancor = 2075,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Katarn", true, "")]
        IncubationFieldNoteKatarn = 2076,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Moonthorn Veloria", true, "")]
        IncubationFieldNoteMoonthornVeloria = 2077,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mush Warrior", true, "")]
        IncubationFieldNoteMushWarrior = 2078,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mustardlash Slime", true, "")]
        IncubationFieldNoteMustardlashSlime = 2079,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mutated Boar", true, "")]
        IncubationFieldNoteMutatedBoar = 2080,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mutated Frog", true, "")]
        IncubationFieldNoteMutatedFrog = 2081,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Nightspot Aralynx", true, "")]
        IncubationFieldNoteNightspotAralynx = 2082,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ochre Maw", true, "")]
        IncubationFieldNoteOchreMaw = 2083,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Orbak Water Horse", true, "")]
        IncubationFieldNoteOrbakWaterHorse = 2084,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Orray", true, "")]
        IncubationFieldNoteOrray = 2085,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Phaseleg Silkstalker", true, "")]
        IncubationFieldNotePhaselegSilkstalker = 2086,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Porg", true, "")]
        IncubationFieldNotePorg = 2087,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Pyrestem Scarab", true, "")]
        IncubationFieldNotePyrestemScarab = 2088,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Razorhide Hound", true, "")]
        IncubationFieldNoteRazorhideHound = 2089,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Redcrest Tatterquill", true, "")]
        IncubationFieldNoteRedcrestTatterquill = 2090,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ronto", true, "")]
        IncubationFieldNoteRonto = 2091,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Rootbound Colossus", true, "")]
        IncubationFieldNoteRootboundColossus = 2092,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Royal Plumage", true, "")]
        IncubationFieldNoteRoyalPlumage = 2093,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Rubyback Drakon", true, "")]
        IncubationFieldNoteRubybackDrakon = 2094,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ruinfang Mongrel", true, "")]
        IncubationFieldNoteRuinfangMongrel = 2095,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Rustwhisker Gnawfiend", true, "")]
        IncubationFieldNoteRustwhiskerGnawfiend = 2096,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Saberleg Kharaxis", true, "")]
        IncubationFieldNoteSaberlegKharaxis = 2097,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sapphire Veylori", true, "")]
        IncubationFieldNoteSapphireVeylori = 2098,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sapphireback Vorex", true, "")]
        IncubationFieldNoteSapphirebackVorex = 2099,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Scrap Rat", true, "")]
        IncubationFieldNoteScrapRat = 2100,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Serene Grovetreader", true, "")]
        IncubationFieldNoteSereneGrovetreader = 2101,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Shatterpelt Lurax", true, "")]
        IncubationFieldNoteShatterpeltLurax = 2102,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Silverveil Aerolith", true, "")]
        IncubationFieldNoteSilverveilAerolith = 2103,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sink Crab", true, "")]
        IncubationFieldNoteSinkCrab = 2104,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sootbelly Mirekit", true, "")]
        IncubationFieldNoteSootbellyMirekit = 2105,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Speckled Seer", true, "")]
        IncubationFieldNoteSpeckledSeer = 2106,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Spined Crawler", true, "")]
        IncubationFieldNoteSpinedCrawler = 2107,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Spinosaurus", true, "")]
        IncubationFieldNoteSpinosaurus = 2108,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Stegosaurus", true, "")]
        IncubationFieldNoteStegosaurus = 2109,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Stinging Swarm", true, "")]
        IncubationFieldNoteStingingSwarm = 2110,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Stoneclad Behemoth", true, "")]
        IncubationFieldNoteStonecladBehemoth = 2111,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Strayfang Kavor", true, "")]
        IncubationFieldNoteStrayfangKavor = 2112,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sumpback Chitinmaw", true, "")]
        IncubationFieldNoteSumpbackChitinmaw = 2113,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Swamp Rat", true, "")]
        IncubationFieldNoteSwampRat = 2114,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tach", true, "")]
        IncubationFieldNoteTach = 2115,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tempest Bulwark", true, "")]
        IncubationFieldNoteTempestBulwark = 2116,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Terentatek", true, "")]
        IncubationFieldNoteTerentatek = 2117,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tideplume Striderel", true, "")]
        IncubationFieldNoteTideplumeStriderel = 2118,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Torosaurus", true, "")]
        IncubationFieldNoteTorosaurus = 2119,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Triceratops", true, "")]
        IncubationFieldNoteTriceratops = 2120,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tukata", true, "")]
        IncubationFieldNoteTukata = 2121,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tundra Ponderer", true, "")]
        IncubationFieldNoteTundraPonderer = 2122,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tyrannosaurus", true, "")]
        IncubationFieldNoteTyrannosaurus = 2123,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ubese Thorn", true, "")]
        IncubationFieldNoteUbeseThorn = 2124,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Umberroot Arctara", true, "")]
        IncubationFieldNoteUmberrootArctara = 2125,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Umbral Barghest", true, "")]
        IncubationFieldNoteUmbralBarghest = 2126,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Umbratalon Corvax", true, "")]
        IncubationFieldNoteUmbratalonCorvax = 2127,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Underbrush Scamp", true, "")]
        IncubationFieldNoteUnderbrushScamp = 2128,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Undersea Carver", true, "")]
        IncubationFieldNoteUnderseaCarver = 2129,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Veilphase Arachnyx", true, "")]
        IncubationFieldNoteVeilphaseArachnyx = 2130,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Venomspike Laigrek", true, "")]
        IncubationFieldNoteVenomspikeLaigrek = 2131,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Verdant Thornwold", true, "")]
        IncubationFieldNoteVerdantThornwold = 2132,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Vermilion Ravager", true, "")]
        IncubationFieldNoteVermilionRavager = 2133,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Viridian Platewyrm", true, "")]
        IncubationFieldNoteViridianPlatewyrm = 2134,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Voidmire Echo", true, "")]
        IncubationFieldNoteVoidmireEcho = 2135,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Weasel", true, "")]
        IncubationFieldNoteWeasel = 2136,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Wraid", true, "")]
        IncubationFieldNoteWraid = 2137,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Wraithweb Nythrax", true, "")]
        IncubationFieldNoteWraithwebNythrax = 2138,

        [KeyItem(KeyItemCategoryType.Music, "Rural Day 1", true, "A recording of \"Rural Day 1\" in your music collection.")]
        SongRuralDay1 = 3001,
        [KeyItem(KeyItemCategoryType.Music, "Rural Day 2", true, "A recording of \"Rural Day 2\" in your music collection.")]
        SongRuralDay2 = 3002,
        [KeyItem(KeyItemCategoryType.Music, "Rural Night", true, "A recording of \"Rural Night\" in your music collection.")]
        SongRuralNight = 3003,
        [KeyItem(KeyItemCategoryType.Music, "Forest Day 1", true, "A recording of \"Forest Day 1\" in your music collection.")]
        SongForestDay1 = 3004,
        [KeyItem(KeyItemCategoryType.Music, "Forest Day 2", true, "A recording of \"Forest Day 2\" in your music collection.")]
        SongForestDay2 = 3005,
        [KeyItem(KeyItemCategoryType.Music, "Forest Night", true, "A recording of \"Forest Night\" in your music collection.")]
        SongForestNight = 3006,
        [KeyItem(KeyItemCategoryType.Music, "Generic Dungeon", true, "A recording of \"Generic Dungeon\" in your music collection.")]
        SongGenericDungeon = 3007,
        [KeyItem(KeyItemCategoryType.Music, "Sewer", true, "A recording of \"Sewer\" in your music collection.")]
        SongSewer = 3008,
        [KeyItem(KeyItemCategoryType.Music, "Mines 1", true, "A recording of \"Mines 1\" in your music collection.")]
        SongMines1 = 3009,
        [KeyItem(KeyItemCategoryType.Music, "Mines 2", true, "A recording of \"Mines 2\" in your music collection.")]
        SongMines2 = 3010,
        [KeyItem(KeyItemCategoryType.Music, "Crypt 1", true, "A recording of \"Crypt 1\" in your music collection.")]
        SongCrypt1 = 3011,
        [KeyItem(KeyItemCategoryType.Music, "Crypt 2", true, "A recording of \"Crypt 2\" in your music collection.")]
        SongCrypt2 = 3012,
        [KeyItem(KeyItemCategoryType.Music, "Evil Dungeon 1", true, "A recording of \"Evil Dungeon 1\" in your music collection.")]
        SongEvilDungeon1 = 3013,
        [KeyItem(KeyItemCategoryType.Music, "Evil Dungeon 2", true, "A recording of \"Evil Dungeon 2\" in your music collection.")]
        SongEvilDungeon2 = 3014,
        [KeyItem(KeyItemCategoryType.Music, "City Slum Day", true, "A recording of \"City Slum Day\" in your music collection.")]
        SongCitySlumDay = 3015,
        [KeyItem(KeyItemCategoryType.Music, "City Slum Night", true, "A recording of \"City Slum Night\" in your music collection.")]
        SongCitySlumNight = 3016,
        [KeyItem(KeyItemCategoryType.Music, "City Docks Day", true, "A recording of \"City Docks Day\" in your music collection.")]
        SongCityDocksDay = 3017,
        [KeyItem(KeyItemCategoryType.Music, "City Docks Night", true, "A recording of \"City Docks Night\" in your music collection.")]
        SongCityDocksNight = 3018,
        [KeyItem(KeyItemCategoryType.Music, "City Wealthy", true, "A recording of \"City Wealthy\" in your music collection.")]
        SongCityWealthy = 3019,
        [KeyItem(KeyItemCategoryType.Music, "City Marketplace", true, "A recording of \"City Marketplace\" in your music collection.")]
        SongCityMarketplace = 3020,
        [KeyItem(KeyItemCategoryType.Music, "City Night", true, "A recording of \"City Night\" in your music collection.")]
        SongCityNight = 3021,
        [KeyItem(KeyItemCategoryType.Music, "Tavern 1", true, "A recording of \"Tavern 1\" in your music collection.")]
        SongTavern1 = 3022,
        [KeyItem(KeyItemCategoryType.Music, "Tavern 2", true, "A recording of \"Tavern 2\" in your music collection.")]
        SongTavern2 = 3023,
        [KeyItem(KeyItemCategoryType.Music, "Tavern 3", true, "A recording of \"Tavern 3\" in your music collection.")]
        SongTavern3 = 3024,
        [KeyItem(KeyItemCategoryType.Music, "Generic Wealthy House", true, "A recording of \"Generic Wealthy House\" in your music collection.")]
        SongGenericWealthyHouse = 3025,
        [KeyItem(KeyItemCategoryType.Music, "Generic Store", true, "A recording of \"Generic Store\" in your music collection.")]
        SongGenericStore = 3026,
        [KeyItem(KeyItemCategoryType.Music, "Good Temple", true, "A recording of \"Good Temple\" in your music collection.")]
        SongGoodTemple = 3027,
        [KeyItem(KeyItemCategoryType.Music, "Evil Temple", true, "A recording of \"Evil Temple\" in your music collection.")]
        SongEvilTemple = 3028,
        [KeyItem(KeyItemCategoryType.Music, "NWN City Theme", true, "A recording of \"NWN City Theme\" in your music collection.")]
        SongNWNCityTheme = 3029,
        [KeyItem(KeyItemCategoryType.Music, "NWN Chapter 1", true, "A recording of \"NWN Chapter 1\" in your music collection.")]
        SongNWNChapter1 = 3030,
        [KeyItem(KeyItemCategoryType.Music, "NWN Chapter 2", true, "A recording of \"NWN Chapter 2\" in your music collection.")]
        SongNWNChapter2 = 3031,
        [KeyItem(KeyItemCategoryType.Music, "NWN Chapter 3", true, "A recording of \"NWN Chapter 3\" in your music collection.")]
        SongNWNChapter3 = 3032,
        [KeyItem(KeyItemCategoryType.Music, "NWN Chapter 4", true, "A recording of \"NWN Chapter 4\" in your music collection.")]
        SongNWNChapter4 = 3033,
        [KeyItem(KeyItemCategoryType.Music, "Rural Combat 1", true, "A recording of \"Rural Combat 1\" in your music collection.")]
        SongRuralCombat1 = 3034,
        [KeyItem(KeyItemCategoryType.Music, "Forest Combat 1", true, "A recording of \"Forest Combat 1\" in your music collection.")]
        SongForestCombat1 = 3035,
        [KeyItem(KeyItemCategoryType.Music, "Forest Combat 2", true, "A recording of \"Forest Combat 2\" in your music collection.")]
        SongForestCombat2 = 3036,
        [KeyItem(KeyItemCategoryType.Music, "Dungeon Combat 1", true, "A recording of \"Dungeon Combat 1\" in your music collection.")]
        SongDungeonCombat1 = 3037,
        [KeyItem(KeyItemCategoryType.Music, "Dungeon Combat 2", true, "A recording of \"Dungeon Combat 2\" in your music collection.")]
        SongDungeonCombat2 = 3038,
        [KeyItem(KeyItemCategoryType.Music, "Dungeon Combat 3", true, "A recording of \"Dungeon Combat 3\" in your music collection.")]
        SongDungeonCombat3 = 3039,
        [KeyItem(KeyItemCategoryType.Music, "City Combat 1", true, "A recording of \"City Combat 1\" in your music collection.")]
        SongCityCombat1 = 3040,
        [KeyItem(KeyItemCategoryType.Music, "City Combat 2", true, "A recording of \"City Combat 2\" in your music collection.")]
        SongCityCombat2 = 3041,
        [KeyItem(KeyItemCategoryType.Music, "City Combat 3", true, "A recording of \"City Combat 3\" in your music collection.")]
        SongCityCombat3 = 3042,
        [KeyItem(KeyItemCategoryType.Music, "Boss Combat City", true, "A recording of \"Boss Combat City\" in your music collection.")]
        SongBossCombatCity = 3043,
        [KeyItem(KeyItemCategoryType.Music, "Boss Combat Forest", true, "A recording of \"Boss Combat Forest\" in your music collection.")]
        SongBossCombatForest = 3044,
        [KeyItem(KeyItemCategoryType.Music, "Boss Lizardfolk", true, "A recording of \"Boss Lizardfolk\" in your music collection.")]
        SongBossLizardfolk = 3045,
        [KeyItem(KeyItemCategoryType.Music, "Dragon Combat", true, "A recording of \"Dragon Combat\" in your music collection.")]
        SongDragonCombat = 3046,
        [KeyItem(KeyItemCategoryType.Music, "Combat Aribeth", true, "A recording of \"Combat Aribeth\" in your music collection.")]
        SongCombatAribeth = 3047,
        [KeyItem(KeyItemCategoryType.Music, "Final Boss Combat Source Stone", true, "A recording of \"Final Boss Combat Source Stone\" in your music collection.")]
        SongFinalBossCombatSourceStone = 3048,
        [KeyItem(KeyItemCategoryType.Music, "Good Temple 2", true, "A recording of \"Good Temple 2\" in your music collection.")]
        SongGoodTemple2 = 3049,
        [KeyItem(KeyItemCategoryType.Music, "Generic Castle Interior", true, "A recording of \"Generic Castle Interior\" in your music collection.")]
        SongGenericCastleInterior = 3050,
        [KeyItem(KeyItemCategoryType.Music, "Aribeth Theme 1", true, "A recording of \"Aribeth Theme 1\" in your music collection.")]
        SongAribethTheme1 = 3051,
        [KeyItem(KeyItemCategoryType.Music, "Aribeth Theme 2", true, "A recording of \"Aribeth Theme 2\" in your music collection.")]
        SongAribethTheme2 = 3052,
        [KeyItem(KeyItemCategoryType.Music, "Theme Aarin Gend", true, "A recording of \"Theme Aarin Gend\" in your music collection.")]
        SongThemeAarinGend = 3053,
        [KeyItem(KeyItemCategoryType.Music, "Theme Maugrim", true, "A recording of \"Theme Maugrim\" in your music collection.")]
        SongThemeMaugrim = 3054,
        [KeyItem(KeyItemCategoryType.Music, "Theme Morag", true, "A recording of \"Theme Morag\" in your music collection.")]
        SongThemeMorag = 3055,
        [KeyItem(KeyItemCategoryType.Music, "Tavern 4", true, "A recording of \"Tavern 4\" in your music collection.")]
        SongTavern4 = 3056,
        [KeyItem(KeyItemCategoryType.Music, "Desert Combat", true, "A recording of \"Desert Combat\" in your music collection.")]
        SongDesertCombat = 3057,
        [KeyItem(KeyItemCategoryType.Music, "Desert Day", true, "A recording of \"Desert Day\" in your music collection.")]
        SongDesertDay = 3058,
        [KeyItem(KeyItemCategoryType.Music, "Winter Day", true, "A recording of \"Winter Day\" in your music collection.")]
        SongWinterDay = 3059,
        [KeyItem(KeyItemCategoryType.Music, "Fight! Fight!", true, "A recording of \"Fight! Fight!\" in your music collection.")]
        SongFightFight = 3060,
        [KeyItem(KeyItemCategoryType.Music, "Desert Night", true, "A recording of \"Desert Night\" in your music collection.")]
        SongDesertNight = 3061,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Theme", true, "A recording of \"XP2 - Theme\" in your music collection.")]
        SongXP2Theme = 3062,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Waterdeep", true, "A recording of \"XP2 - Waterdeep\" in your music collection.")]
        SongXP2Waterdeep = 3063,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Undermountain", true, "A recording of \"XP2 - Undermountain\" in your music collection.")]
        SongXP2Undermountain = 3064,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Rebel Camp", true, "A recording of \"XP2 - Rebel Camp\" in your music collection.")]
        SongXP2RebelCamp = 3065,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Beholder Caves", true, "A recording of \"XP2 - Beholder Caves\" in your music collection.")]
        SongXP2BeholderCaves = 3066,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Queen of the Shattered Mirror", true, "A recording of \"XP2 - Queen of the Shattered Mirror\" in your music collection.")]
        SongXP2QueenOfTheShatteredMirror = 3067,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Chapter 3", true, "A recording of \"XP2 - Chapter 3\" in your music collection.")]
        SongXP2Chapter3 = 3068,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Dracolich", true, "A recording of \"XP2 - Dracolich\" in your music collection.")]
        SongXP2Dracolich = 3069,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Battle, Small", true, "A recording of \"XP2 - Battle, Small\" in your music collection.")]
        SongXP2BattleSmall = 3070,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Battle, Medium", true, "A recording of \"XP2 - Battle, Medium\" in your music collection.")]
        SongXP2BattleMedium = 3071,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Battle, Large", true, "A recording of \"XP2 - Battle, Large\" in your music collection.")]
        SongXP2BattleLarge = 3072,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Battle, Chapter 3", true, "A recording of \"XP2 - Battle, Chapter 3\" in your music collection.")]
        SongXP2BattleChapter3 = 3073,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Battle, Matron", true, "A recording of \"XP2 - Battle, Matron\" in your music collection.")]
        SongXP2BattleMatron = 3074,
        [KeyItem(KeyItemCategoryType.Music, "XP2 - Battle, Final", true, "A recording of \"XP2 - Battle, Final\" in your music collection.")]
        SongXP2BattleFinal = 3075,
        [KeyItem(KeyItemCategoryType.Music, "PM - Kingmaker Theme", true, "A recording of \"PM - Kingmaker Theme\" in your music collection.")]
        SongPMKingmakerTheme = 3076,
        [KeyItem(KeyItemCategoryType.Music, "PM - ShadowGuard Theme", true, "A recording of \"PM - ShadowGuard Theme\" in your music collection.")]
        SongPMShadowGuardTheme = 3077,
        [KeyItem(KeyItemCategoryType.Music, "PM - Witch's Wake Theme", true, "A recording of \"PM - Witch's Wake Theme\" in your music collection.")]
        SongPMWitchsWakeTheme = 3078,
        [KeyItem(KeyItemCategoryType.Music, "PM - Pirates Theme", true, "A recording of \"PM - Pirates Theme\" in your music collection.")]
        SongPMPiratesTheme = 3079,
        [KeyItem(KeyItemCategoryType.Music, "PM - Pirates Theme Variation", true, "A recording of \"PM - Pirates Theme Variation\" in your music collection.")]
        SongPMPiratesThemeVariation = 3080,
        [KeyItem(KeyItemCategoryType.Music, "PM - Infinite Dungeons", true, "A recording of \"PM - Infinite Dungeons\" in your music collection.")]
        SongPMInfiniteDungeons = 3081,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Ogma", true, "A recording of \"PM - Wyvern Crown Ogma\" in your music collection.")]
        SongPMWyvernCrownOgma = 3082,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Open Road", true, "A recording of \"PM - Wyvern Crown Open Road\" in your music collection.")]
        SongPMWyvernCrownOpenRoad = 3083,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Smoke and Ash", true, "A recording of \"PM - Wyvern Crown Smoke and Ash\" in your music collection.")]
        SongPMWyvernCrownSmokeAndAsh = 3084,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Rural", true, "A recording of \"PM - Wyvern Crown Rural\" in your music collection.")]
        SongPMWyvernCrownRural = 3085,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Rural Guitar", true, "A recording of \"PM - Wyvern Crown Rural Guitar\" in your music collection.")]
        SongPMWyvernCrownRuralGuitar = 3086,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Tears", true, "A recording of \"PM - Wyvern Crown Tears\" in your music collection.")]
        SongPMWyvernCrownTears = 3087,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Battle Chase", true, "A recording of \"PM - Wyvern Crown Battle Chase\" in your music collection.")]
        SongPMWyvernCrownBattleChase = 3088,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Theme", true, "A recording of \"PM - Wyvern Crown Theme\" in your music collection.")]
        SongPMWyvernCrownTheme = 3089,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Armchair", true, "A recording of \"PM - Wyvern Crown Armchair\" in your music collection.")]
        SongPMWyvernCrownArmchair = 3090,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Battle", true, "A recording of \"PM - Wyvern Crown Battle\" in your music collection.")]
        SongPMWyvernCrownBattle = 3091,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Solace", true, "A recording of \"PM - Wyvern Crown Solace\" in your music collection.")]
        SongPMWyvernCrownSolace = 3092,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Knighted", true, "A recording of \"PM - Wyvern Crown Knighted\" in your music collection.")]
        SongPMWyvernCrownKnighted = 3093,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Opening", true, "A recording of \"PM - Wyvern Crown Opening\" in your music collection.")]
        SongPMWyvernCrownOpening = 3094,
        [KeyItem(KeyItemCategoryType.Music, "PM - Wyvern Crown Morfen", true, "A recording of \"PM - Wyvern Crown Morfen\" in your music collection.")]
        SongPMWyvernCrownMorfen = 3095,
        [KeyItem(KeyItemCategoryType.Music, "Caves, DJohn", true, "A recording of \"Caves, DJohn\" in your music collection.")]
        SongCavesDJohn = 3190,
        [KeyItem(KeyItemCategoryType.Music, "Battle FOT Groove", true, "A recording of \"Battle FOT Groove\" in your music collection.")]
        SongBattleFOTGroove = 3211,
        [KeyItem(KeyItemCategoryType.Music, "Drums 4", true, "A recording of \"Drums 4\" in your music collection.")]
        SongDrums4 = 3215,
        [KeyItem(KeyItemCategoryType.Music, "Space River", true, "A recording of \"Space River\" in your music collection.")]
        SongSpaceRiver = 3260,
        [KeyItem(KeyItemCategoryType.Music, "ST Suspense 1", true, "A recording of \"ST Suspense 1\" in your music collection.")]
        SongSTSuspense1 = 3263,
        [KeyItem(KeyItemCategoryType.Music, "SW Trash Mash", true, "A recording of \"SW Trash Mash\" in your music collection.")]
        SongSWTrashMash = 3268,
        [KeyItem(KeyItemCategoryType.Music, "Control Freak", true, "A recording of \"Control Freak\" in your music collection.")]
        SongControlFreak = 3272,
        [KeyItem(KeyItemCategoryType.Music, "Feel Hot Nightclub", true, "A recording of \"Feel Hot Nightclub\" in your music collection.")]
        SongFeelHotNightclub = 3277,
        [KeyItem(KeyItemCategoryType.Music, "Musik Box Nightclub", true, "A recording of \"Musik Box Nightclub\" in your music collection.")]
        SongMusikBoxNightclub = 3279,
        [KeyItem(KeyItemCategoryType.Music, "Cheyenne", true, "A recording of \"Cheyenne\" in your music collection.")]
        SongCheyenne = 3280,
        [KeyItem(KeyItemCategoryType.Music, "Battle C&C Generals", true, "A recording of \"Battle C&C Generals\" in your music collection.")]
        SongBattleCCGenerals = 3299,
        [KeyItem(KeyItemCategoryType.Music, "C&C Generals USA 4", true, "A recording of \"C&C Generals USA 4\" in your music collection.")]
        SongCCGeneralsUSA4 = 3300,
        [KeyItem(KeyItemCategoryType.Music, "C&C Generals USA 8", true, "A recording of \"C&C Generals USA 8\" in your music collection.")]
        SongCCGeneralsUSA8 = 3301,
        [KeyItem(KeyItemCategoryType.Music, "Battle Fr Quarter Chase", true, "A recording of \"Battle Fr Quarter Chase\" in your music collection.")]
        SongBattleFrQuarterChase = 3304,
        [KeyItem(KeyItemCategoryType.Music, "Ghost Recon Anthem", true, "A recording of \"Ghost Recon Anthem\" in your music collection.")]
        SongGhostReconAnthem = 3305,
        [KeyItem(KeyItemCategoryType.Music, "Ghost Recon Outro", true, "A recording of \"Ghost Recon Outro\" in your music collection.")]
        SongGhostReconOutro = 3306,
        [KeyItem(KeyItemCategoryType.Music, "Battle Hulk Destruction", true, "A recording of \"Battle Hulk Destruction\" in your music collection.")]
        SongBattleHulkDestruction = 3308,
        [KeyItem(KeyItemCategoryType.Music, "Battle Lady Death War", true, "A recording of \"Battle Lady Death War\" in your music collection.")]
        SongBattleLadyDeathWar = 3310,
        [KeyItem(KeyItemCategoryType.Music, "Battle Lineage Conflict", true, "A recording of \"Battle Lineage Conflict\" in your music collection.")]
        SongBattleLineageConflict = 3311,
        [KeyItem(KeyItemCategoryType.Music, "Lineage Ominous Vis", true, "A recording of \"Lineage Ominous Vis\" in your music collection.")]
        SongLineageOminousVis = 3312,
        [KeyItem(KeyItemCategoryType.Music, "Lineage Trailer", true, "A recording of \"Lineage Trailer\" in your music collection.")]
        SongLineageTrailer = 3313,
        [KeyItem(KeyItemCategoryType.Music, "Battle Swamp", true, "A recording of \"Battle Swamp\" in your music collection.")]
        SongBattleSwamp = 3319,
        [KeyItem(KeyItemCategoryType.Music, "Temple of the Moon", true, "A recording of \"Temple of the Moon\" in your music collection.")]
        SongTempleOfTheMoon = 3320,
        [KeyItem(KeyItemCategoryType.Music, "Battle TLJ Danger", true, "A recording of \"Battle TLJ Danger\" in your music collection.")]
        SongBattleTLJDanger = 3322,
        [KeyItem(KeyItemCategoryType.Music, "TLJ Techno Eagle", true, "A recording of \"TLJ Techno Eagle\" in your music collection.")]
        SongTLJTechnoEagle = 3331,
        [KeyItem(KeyItemCategoryType.Music, "Trapped Air Vent", true, "A recording of \"Trapped Air Vent\" in your music collection.")]
        SongTrappedAirVent = 3333,
        [KeyItem(KeyItemCategoryType.Music, "Battle Zero Hour Theme", true, "A recording of \"Battle Zero Hour Theme\" in your music collection.")]
        SongBattleZeroHourTheme = 3337,
        [KeyItem(KeyItemCategoryType.Music, "NWN Main Theme", true, "A recording of \"NWN Main Theme\" in your music collection.")]
        SongNWNMainTheme = 3338,
        [KeyItem(KeyItemCategoryType.Music, "Cantina Iziz", true, "A recording of \"Cantina Iziz\" in your music collection.")]
        SongCantinaIziz = 3339,
        [KeyItem(KeyItemCategoryType.Music, "Cantina Pazaak", true, "A recording of \"Cantina Pazaak\" in your music collection.")]
        SongCantinaPazaak = 3340,
        [KeyItem(KeyItemCategoryType.Music, "Cantina Javyars", true, "A recording of \"Cantina Javyars\" in your music collection.")]
        SongCantinaJavyars = 3341,
        [KeyItem(KeyItemCategoryType.Music, "Korriban Cave 1", true, "A recording of \"Korriban Cave 1\" in your music collection.")]
        SongKorribanCave1 = 3342,
        [KeyItem(KeyItemCategoryType.Music, "Citadel Station 1", true, "A recording of \"Citadel Station 1\" in your music collection.")]
        SongCitadelStation1 = 3343,
        [KeyItem(KeyItemCategoryType.Music, "Citadel Station 2", true, "A recording of \"Citadel Station 2\" in your music collection.")]
        SongCitadelStation2 = 3344,
        [KeyItem(KeyItemCategoryType.Music, "Anchorhead", true, "A recording of \"Anchorhead\" in your music collection.")]
        SongAnchorhead = 3345,
        [KeyItem(KeyItemCategoryType.Music, "Jedi Enclave K1", true, "A recording of \"Jedi Enclave K1\" in your music collection.")]
        SongJediEnclaveK1 = 3346,
        [KeyItem(KeyItemCategoryType.Music, "Jedi Enclave K2", true, "A recording of \"Jedi Enclave K2\" in your music collection.")]
        SongJediEnclaveK2 = 3347,
        [KeyItem(KeyItemCategoryType.Music, "Dantooine K1", true, "A recording of \"Dantooine K1\" in your music collection.")]
        SongDantooineK1 = 3348,
        [KeyItem(KeyItemCategoryType.Music, "Dantooine K2", true, "A recording of \"Dantooine K2\" in your music collection.")]
        SongDantooineK2 = 3349,
        [KeyItem(KeyItemCategoryType.Music, "Dune Sea", true, "A recording of \"Dune Sea\" in your music collection.")]
        SongDuneSea = 3350,
        [KeyItem(KeyItemCategoryType.Music, "Dxun Ext 1", true, "A recording of \"Dxun Ext 1\" in your music collection.")]
        SongDxunExt1 = 3351,
        [KeyItem(KeyItemCategoryType.Music, "Dxun Ext 2", true, "A recording of \"Dxun Ext 2\" in your music collection.")]
        SongDxunExt2 = 3352,
        [KeyItem(KeyItemCategoryType.Music, "Ebon Hawk K2", true, "A recording of \"Ebon Hawk K2\" in your music collection.")]
        SongEbonHawkK2 = 3353,
        [KeyItem(KeyItemCategoryType.Music, "Harbinger Int 1", true, "A recording of \"Harbinger Int 1\" in your music collection.")]
        SongHarbingerInt1 = 3354,
        [KeyItem(KeyItemCategoryType.Music, "Harbinger Int 2", true, "A recording of \"Harbinger Int 2\" in your music collection.")]
        SongHarbingerInt2 = 3355,
        [KeyItem(KeyItemCategoryType.Music, "Hrakert Facility", true, "A recording of \"Hrakert Facility\" in your music collection.")]
        SongHrakertFacility = 3356,
        [KeyItem(KeyItemCategoryType.Music, "Jek Jek Tar", true, "A recording of \"Jek Jek Tar\" in your music collection.")]
        SongJekJekTar = 3357,
        [KeyItem(KeyItemCategoryType.Music, "Kashyyyk Canopy", true, "A recording of \"Kashyyyk Canopy\" in your music collection.")]
        SongKashyyykCanopy = 3358,
        [KeyItem(KeyItemCategoryType.Music, "Kashyyyk Shadow", true, "A recording of \"Kashyyyk Shadow\" in your music collection.")]
        SongKashyyykShadow = 3359,
        [KeyItem(KeyItemCategoryType.Music, "Korriban Ext K1", true, "A recording of \"Korriban Ext K1\" in your music collection.")]
        SongKorribanExtK1 = 3360,
        [KeyItem(KeyItemCategoryType.Music, "Korriban Ext K2", true, "A recording of \"Korriban Ext K2\" in your music collection.")]
        SongKorribanExtK2 = 3361,
        [KeyItem(KeyItemCategoryType.Music, "Korriban Visions", true, "A recording of \"Korriban Visions\" in your music collection.")]
        SongKorribanVisions = 3362,
        [KeyItem(KeyItemCategoryType.Music, "Lehon Ext 1", true, "A recording of \"Lehon Ext 1\" in your music collection.")]
        SongLehonExt1 = 3363,
        [KeyItem(KeyItemCategoryType.Music, "Manaan Ext 1", true, "A recording of \"Manaan Ext 1\" in your music collection.")]
        SongManaanExt1 = 3364,
        [KeyItem(KeyItemCategoryType.Music, "Mandalorian Camp", true, "A recording of \"Mandalorian Camp\" in your music collection.")]
        SongMandalorianCamp = 3365,
        [KeyItem(KeyItemCategoryType.Music, "Nar Shaddaa Ext", true, "A recording of \"Nar Shaddaa Ext\" in your music collection.")]
        SongNarShaddaaExt = 3366,
        [KeyItem(KeyItemCategoryType.Music, "Onderon Ext 1", true, "A recording of \"Onderon Ext 1\" in your music collection.")]
        SongOnderonExt1 = 3367,
        [KeyItem(KeyItemCategoryType.Music, "Onderon Palace", true, "A recording of \"Onderon Palace\" in your music collection.")]
        SongOnderonPalace = 3368,
        [KeyItem(KeyItemCategoryType.Music, "Strange K1", true, "A recording of \"Strange K1\" in your music collection.")]
        SongStrangeK1 = 3369,
        [KeyItem(KeyItemCategoryType.Music, "Strange K2", true, "A recording of \"Strange K2\" in your music collection.")]
        SongStrangeK2 = 3370,
        [KeyItem(KeyItemCategoryType.Music, "Rakatan Ruins", true, "A recording of \"Rakatan Ruins\" in your music collection.")]
        SongRakatanRuins = 3371,
        [KeyItem(KeyItemCategoryType.Music, "Sand People Int 1", true, "A recording of \"Sand People Int 1\" in your music collection.")]
        SongSandPeopleInt1 = 3372,
        [KeyItem(KeyItemCategoryType.Music, "Sith Academy K1", true, "A recording of \"Sith Academy K1\" in your music collection.")]
        SongSithAcademyK1 = 3374,
        [KeyItem(KeyItemCategoryType.Music, "Sith Academy K2", true, "A recording of \"Sith Academy K2\" in your music collection.")]
        SongSithAcademyK2 = 3375,
        [KeyItem(KeyItemCategoryType.Music, "Theme Sith Ship", true, "A recording of \"Theme Sith Ship\" in your music collection.")]
        SongThemeSithShip = 3376,
        [KeyItem(KeyItemCategoryType.Music, "Sleheyron Ext 1", true, "A recording of \"Sleheyron Ext 1\" in your music collection.")]
        SongSleheyronExt1 = 3377,
        [KeyItem(KeyItemCategoryType.Music, "Theme Starforge", true, "A recording of \"Theme Starforge\" in your music collection.")]
        SongThemeStarforge = 3378,
        [KeyItem(KeyItemCategoryType.Music, "Jedi Enclave Sub", true, "A recording of \"Jedi Enclave Sub\" in your music collection.")]
        SongJediEnclaveSub = 3379,
        [KeyItem(KeyItemCategoryType.Music, "Taris Apartments", true, "A recording of \"Taris Apartments\" in your music collection.")]
        SongTarisApartments = 3380,
        [KeyItem(KeyItemCategoryType.Music, "Taris Lower City", true, "A recording of \"Taris Lower City\" in your music collection.")]
        SongTarisLowerCity = 3381,
        [KeyItem(KeyItemCategoryType.Music, "Taris Sewer", true, "A recording of \"Taris Sewer\" in your music collection.")]
        SongTarisSewer = 3382,
        [KeyItem(KeyItemCategoryType.Music, "Taris Upper City", true, "A recording of \"Taris Upper City\" in your music collection.")]
        SongTarisUpperCity = 3383,
        [KeyItem(KeyItemCategoryType.Music, "Telos Academy", true, "A recording of \"Telos Academy\" in your music collection.")]
        SongTelosAcademy = 3384,
        [KeyItem(KeyItemCategoryType.Music, "Trayus Academy", true, "A recording of \"Trayus Academy\" in your music collection.")]
        SongTrayusAcademy = 3385,
        [KeyItem(KeyItemCategoryType.Music, "Goto's Yacht", true, "A recording of \"Goto's Yacht\" in your music collection.")]
        SongGotosYacht = 3386,
        [KeyItem(KeyItemCategoryType.Music, "Theme Bastila", true, "A recording of \"Theme Bastila\" in your music collection.")]
        SongThemeBastila = 3387,
        [KeyItem(KeyItemCategoryType.Music, "Theme Carth", true, "A recording of \"Theme Carth\" in your music collection.")]
        SongThemeCarth = 3388,
        [KeyItem(KeyItemCategoryType.Music, "Theme Council", true, "A recording of \"Theme Council\" in your music collection.")]
        SongThemeCouncil = 3389,
        [KeyItem(KeyItemCategoryType.Music, "Theme Czerka", true, "A recording of \"Theme Czerka\" in your music collection.")]
        SongThemeCzerka = 3390,
        [KeyItem(KeyItemCategoryType.Music, "Theme Kreia", true, "A recording of \"Theme Kreia\" in your music collection.")]
        SongThemeKreia = 3391,
        [KeyItem(KeyItemCategoryType.Music, "Theme Lightside", true, "A recording of \"Theme Lightside\" in your music collection.")]
        SongThemeLightside = 3392,
        [KeyItem(KeyItemCategoryType.Music, "Theme Malak", true, "A recording of \"Theme Malak\" in your music collection.")]
        SongThemeMalak = 3394,
        [KeyItem(KeyItemCategoryType.Music, "Theme Nihilus", true, "A recording of \"Theme Nihilus\" in your music collection.")]
        SongThemeNihilus = 3395,
        [KeyItem(KeyItemCategoryType.Music, "Theme Republic", true, "A recording of \"Theme Republic\" in your music collection.")]
        SongThemeRepublic = 3396,
        [KeyItem(KeyItemCategoryType.Music, "Theme Sion", true, "A recording of \"Theme Sion\" in your music collection.")]
        SongThemeSion = 3397,
        [KeyItem(KeyItemCategoryType.Music, "Theme Sith", true, "A recording of \"Theme Sith\" in your music collection.")]
        SongThemeSith = 3398,
        [KeyItem(KeyItemCategoryType.Music, "Theme Traya", true, "A recording of \"Theme Traya\" in your music collection.")]
        SongThemeTraya = 3401,
        [KeyItem(KeyItemCategoryType.Music, "Theme Tyrus", true, "A recording of \"Theme Tyrus\" in your music collection.")]
        SongThemeTyrus = 3402,
        [KeyItem(KeyItemCategoryType.Music, "Anchorhead Battle", true, "A recording of \"Anchorhead Battle\" in your music collection.")]
        SongAnchorheadBattle = 3403,
        [KeyItem(KeyItemCategoryType.Music, "Base Battle", true, "A recording of \"Base Battle\" in your music collection.")]
        SongBaseBattle = 3404,
        [KeyItem(KeyItemCategoryType.Music, "Dantooine K1 Battle", true, "A recording of \"Dantooine K1 Battle\" in your music collection.")]
        SongDantooineK1Battle = 3405,
        [KeyItem(KeyItemCategoryType.Music, "Dune Sea Battle", true, "A recording of \"Dune Sea Battle\" in your music collection.")]
        SongDuneSeaBattle = 3406,
        [KeyItem(KeyItemCategoryType.Music, "Dxun Battle", true, "A recording of \"Dxun Battle\" in your music collection.")]
        SongDxunBattle = 3407,
        [KeyItem(KeyItemCategoryType.Music, "Battle Fast 1 K2", true, "A recording of \"Battle Fast 1 K2\" in your music collection.")]
        SongBattleFast1K2 = 3408,
        [KeyItem(KeyItemCategoryType.Music, "Battle Fast 2 K2", true, "A recording of \"Battle Fast 2 K2\" in your music collection.")]
        SongBattleFast2K2 = 3409,
        [KeyItem(KeyItemCategoryType.Music, "Battle Generic 1 K2", true, "A recording of \"Battle Generic 1 K2\" in your music collection.")]
        SongBattleGeneric1K2 = 3410,
        [KeyItem(KeyItemCategoryType.Music, "Battle Generic 2 K2", true, "A recording of \"Battle Generic 2 K2\" in your music collection.")]
        SongBattleGeneric2K2 = 3411,
        [KeyItem(KeyItemCategoryType.Music, "Battle Generic 3 K2", true, "A recording of \"Battle Generic 3 K2\" in your music collection.")]
        SongBattleGeneric3K2 = 3412,
        [KeyItem(KeyItemCategoryType.Music, "Hrakert Facility Battle", true, "A recording of \"Hrakert Facility Battle\" in your music collection.")]
        SongHrakertFacilityBattle = 3413,
        [KeyItem(KeyItemCategoryType.Music, "Kashyyyk Shadow Battle", true, "A recording of \"Kashyyyk Shadow Battle\" in your music collection.")]
        SongKashyyykShadowBattle = 3414,
        [KeyItem(KeyItemCategoryType.Music, "Theme Malak Battle", true, "A recording of \"Theme Malak Battle\" in your music collection.")]
        SongThemeMalakBattle = 3415,
        [KeyItem(KeyItemCategoryType.Music, "Battle Large 1 K2", true, "A recording of \"Battle Large 1 K2\" in your music collection.")]
        SongBattleLarge1K2 = 3416,
        [KeyItem(KeyItemCategoryType.Music, "Manaan Ext Battle", true, "A recording of \"Manaan Ext Battle\" in your music collection.")]
        SongManaanExtBattle = 3417,
        [KeyItem(KeyItemCategoryType.Music, "Battle Medium 1 K2", true, "A recording of \"Battle Medium 1 K2\" in your music collection.")]
        SongBattleMedium1K2 = 3418,
        [KeyItem(KeyItemCategoryType.Music, "Nar Shaddaa Ext Battle", true, "A recording of \"Nar Shaddaa Ext Battle\" in your music collection.")]
        SongNarShaddaaExtBattle = 3419,
        [KeyItem(KeyItemCategoryType.Music, "Onderon Ext Battle 1", true, "A recording of \"Onderon Ext Battle 1\" in your music collection.")]
        SongOnderonExtBattle1 = 3420,
        [KeyItem(KeyItemCategoryType.Music, "Onderon Ext Battle 2", true, "A recording of \"Onderon Ext Battle 2\" in your music collection.")]
        SongOnderonExtBattle2 = 3421,
        [KeyItem(KeyItemCategoryType.Music, "Peragus Battle", true, "A recording of \"Peragus Battle\" in your music collection.")]
        SongPeragusBattle = 3422,
        [KeyItem(KeyItemCategoryType.Music, "Lehon Ext Battle", true, "A recording of \"Lehon Ext Battle\" in your music collection.")]
        SongLehonExtBattle = 3423,
        [KeyItem(KeyItemCategoryType.Music, "Lehon Int Battle", true, "A recording of \"Lehon Int Battle\" in your music collection.")]
        SongLehonIntBattle = 3424,
        [KeyItem(KeyItemCategoryType.Music, "Rakata Ruins Battle", true, "A recording of \"Rakata Ruins Battle\" in your music collection.")]
        SongRakataRuinsBattle = 3425,
        [KeyItem(KeyItemCategoryType.Music, "Sand People Battle", true, "A recording of \"Sand People Battle\" in your music collection.")]
        SongSandPeopleBattle = 3426,
        [KeyItem(KeyItemCategoryType.Music, "Battle Small 1 K2", true, "A recording of \"Battle Small 1 K2\" in your music collection.")]
        SongBattleSmall1K2 = 3427,
        [KeyItem(KeyItemCategoryType.Music, "Tarris Upper City Battle", true, "A recording of \"Tarris Upper City Battle\" in your music collection.")]
        SongTarrisUpperCityBattle = 3428,
        [KeyItem(KeyItemCategoryType.Music, "Taris Sewer Battle", true, "A recording of \"Taris Sewer Battle\" in your music collection.")]
        SongTarisSewerBattle = 3429,
        [KeyItem(KeyItemCategoryType.Music, "Battle Urban 1 K2", true, "A recording of \"Battle Urban 1 K2\" in your music collection.")]
        SongBattleUrban1K2 = 3430,
        [KeyItem(KeyItemCategoryType.Music, "Battle Urban 2 K2", true, "A recording of \"Battle Urban 2 K2\" in your music collection.")]
        SongBattleUrban2K2 = 3431,
        [KeyItem(KeyItemCategoryType.Music, "Battle Urban 3 K2", true, "A recording of \"Battle Urban 3 K2\" in your music collection.")]
        SongBattleUrban3K2 = 3432,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Bespin", true, "A recording of \"Theme, Bespin\" in your music collection.")]
        SongThemeBespin = 3433,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Corellia", true, "A recording of \"Theme, Corellia\" in your music collection.")]
        SongThemeCorellia = 3434,
        [KeyItem(KeyItemCategoryType.Music, "Ambience, Creepy", true, "A recording of \"Ambience, Creepy\" in your music collection.")]
        SongAmbienceCreepy = 3435,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Dathomir", true, "A recording of \"Theme, Dathomir\" in your music collection.")]
        SongThemeDathomir = 3436,
        [KeyItem(KeyItemCategoryType.Music, "Design Salon 1", true, "A recording of \"Design Salon 1\" in your music collection.")]
        SongDesignSalon1 = 3437,
        [KeyItem(KeyItemCategoryType.Music, "Design Salon 3", true, "A recording of \"Design Salon 3\" in your music collection.")]
        SongDesignSalon3 = 3439,
        [KeyItem(KeyItemCategoryType.Music, "Jungle 1", true, "A recording of \"Jungle 1\" in your music collection.")]
        SongJungle1 = 3440,
        [KeyItem(KeyItemCategoryType.Music, "Jungle 2", true, "A recording of \"Jungle 2\" in your music collection.")]
        SongJungle2 = 3441,
        [KeyItem(KeyItemCategoryType.Music, "Swamp 1", true, "A recording of \"Swamp 1\" in your music collection.")]
        SongSwamp1 = 3442,
        [KeyItem(KeyItemCategoryType.Music, "Swamp 2", true, "A recording of \"Swamp 2\" in your music collection.")]
        SongSwamp2 = 3443,
        [KeyItem(KeyItemCategoryType.Music, "Sunrise 2", true, "A recording of \"Sunrise 2\" in your music collection.")]
        SongSunrise2 = 3445,
        [KeyItem(KeyItemCategoryType.Music, "Sunset 2", true, "A recording of \"Sunset 2\" in your music collection.")]
        SongSunset2 = 3447,
        [KeyItem(KeyItemCategoryType.Music, "Explore 1", true, "A recording of \"Explore 1\" in your music collection.")]
        SongExplore1 = 3448,
        [KeyItem(KeyItemCategoryType.Music, "Baroque Recital", true, "A recording of \"Baroque Recital\" in your music collection.")]
        SongBaroqueRecital = 3449,
        [KeyItem(KeyItemCategoryType.Music, "Celebration", true, "A recording of \"Celebration\" in your music collection.")]
        SongCelebration = 3450,
        [KeyItem(KeyItemCategoryType.Music, "Generic 1", true, "A recording of \"Generic 1\" in your music collection.")]
        SongGeneric1 = 3451,
        [KeyItem(KeyItemCategoryType.Music, "Generic 2", true, "A recording of \"Generic 2\" in your music collection.")]
        SongGeneric2 = 3452,
        [KeyItem(KeyItemCategoryType.Music, "Generic 3", true, "A recording of \"Generic 3\" in your music collection.")]
        SongGeneric3 = 3453,
        [KeyItem(KeyItemCategoryType.Music, "Gloom 1", true, "A recording of \"Gloom 1\" in your music collection.")]
        SongGloom1 = 3454,
        [KeyItem(KeyItemCategoryType.Music, "Gloom 2", true, "A recording of \"Gloom 2\" in your music collection.")]
        SongGloom2 = 3455,
        [KeyItem(KeyItemCategoryType.Music, "Humor 1", true, "A recording of \"Humor 1\" in your music collection.")]
        SongHumor1 = 3456,
        [KeyItem(KeyItemCategoryType.Music, "Max Rebo 1", true, "A recording of \"Max Rebo 1\" in your music collection.")]
        SongMaxRebo1 = 3457,
        [KeyItem(KeyItemCategoryType.Music, "Max Rebo 2", true, "A recording of \"Max Rebo 2\" in your music collection.")]
        SongMaxRebo2 = 3458,
        [KeyItem(KeyItemCategoryType.Music, "Holidays 1", true, "A recording of \"Holidays 1\" in your music collection.")]
        SongHolidays1 = 3459,
        [KeyItem(KeyItemCategoryType.Music, "Holidays 2", true, "A recording of \"Holidays 2\" in your music collection.")]
        SongHolidays2 = 3460,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Zombie", true, "A recording of \"Theme, Zombie\" in your music collection.")]
        SongThemeZombie = 3461,
        [KeyItem(KeyItemCategoryType.Music, "Duel of the Fates", true, "A recording of \"Duel of the Fates\" in your music collection.")]
        SongDuelOfTheFates = 3462,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Emperor", true, "A recording of \"Theme, Emperor\" in your music collection.")]
        SongThemeEmperor = 3463,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Endor", true, "A recording of \"Theme, Endor\" in your music collection.")]
        SongThemeEndor = 3464,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Exar", true, "A recording of \"Theme, Exar\" in your music collection.")]
        SongThemeExar = 3465,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Force Evil", true, "A recording of \"Theme, Force Evil\" in your music collection.")]
        SongThemeForceEvil = 3466,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Force Good", true, "A recording of \"Theme, Force Good\" in your music collection.")]
        SongThemeForceGood = 3467,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Force Neutral", true, "A recording of \"Theme, Force Neutral\" in your music collection.")]
        SongThemeForceNeutral = 3468,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Force", true, "A recording of \"Theme, Force\" in your music collection.")]
        SongThemeForce = 3469,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Grievous", true, "A recording of \"Theme, Grievous\" in your music collection.")]
        SongThemeGrievous = 3470,
        [KeyItem(KeyItemCategoryType.Music, "Theme, HK-47", true, "A recording of \"Theme, HK-47\" in your music collection.")]
        SongThemeHK47 = 3471,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Lando", true, "A recording of \"Theme, Lando\" in your music collection.")]
        SongThemeLando = 3472,
        [KeyItem(KeyItemCategoryType.Music, "Mando'a Chant", true, "A recording of \"Mando'a Chant\" in your music collection.")]
        SongMandoaChant = 3473,
        [KeyItem(KeyItemCategoryType.Music, "Mystery 1", true, "A recording of \"Mystery 1\" in your music collection.")]
        SongMystery1 = 3474,
        [KeyItem(KeyItemCategoryType.Music, "Opera 1", true, "A recording of \"Opera 1\" in your music collection.")]
        SongOpera1 = 3475,
        [KeyItem(KeyItemCategoryType.Music, "Theme, Palpatine", true, "A recording of \"Theme, Palpatine\" in your music collection.")]
        SongThemePalpatine = 3476,
        [KeyItem(KeyItemCategoryType.Music, "Ryyatt Trail", true, "A recording of \"Ryyatt Trail\" in your music collection.")]
        SongRyyattTrail = 3478,
        [KeyItem(KeyItemCategoryType.Music, "Tragic 2", true, "A recording of \"Tragic 2\" in your music collection.")]
        SongTragic2 = 3479,
        [KeyItem(KeyItemCategoryType.Music, "Atlantica 2", true, "A recording of \"Atlantica 2\" in your music collection.")]
        SongAtlantica2 = 3481,
        [KeyItem(KeyItemCategoryType.Music, "Glory Eternal", true, "A recording of \"Glory Eternal\" in your music collection.")]
        SongGloryEternal = 3483,
        [KeyItem(KeyItemCategoryType.Music, "Kara'tar", true, "A recording of \"Kara'tar\" in your music collection.")]
        SongKaratar = 3484,
        [KeyItem(KeyItemCategoryType.Music, "Cantina 1", true, "A recording of \"Cantina 1\" in your music collection.")]
        SongCantina1 = 3509,
        [KeyItem(KeyItemCategoryType.Music, "Cantina 2", true, "A recording of \"Cantina 2\" in your music collection.")]
        SongCantina2 = 3510,
        [KeyItem(KeyItemCategoryType.Music, "Battle 1", true, "A recording of \"Battle 1\" in your music collection.")]
        SongBattle1 = 3511,
        [KeyItem(KeyItemCategoryType.Music, "Battle 2", true, "A recording of \"Battle 2\" in your music collection.")]
        SongBattle2 = 3512,
        [KeyItem(KeyItemCategoryType.Music, "Battle 3", true, "A recording of \"Battle 3\" in your music collection.")]
        SongBattle3 = 3513,
        [KeyItem(KeyItemCategoryType.Music, "Battle 4", true, "A recording of \"Battle 4\" in your music collection.")]
        SongBattle4 = 3514,
        [KeyItem(KeyItemCategoryType.Music, "Chase", true, "A recording of \"Chase\" in your music collection.")]
        SongChase = 3515,
        [KeyItem(KeyItemCategoryType.Music, "Imperial March", true, "A recording of \"Imperial March\" in your music collection.")]
        SongImperialMarch = 3516,
        [KeyItem(KeyItemCategoryType.Music, "Battle 5", true, "A recording of \"Battle 5\" in your music collection.")]
        SongBattle5 = 3520,
        [KeyItem(KeyItemCategoryType.Music, "Star Wars Title", true, "A recording of \"Star Wars Title\" in your music collection.")]
        SongStarWarsTitle = 3521,
        [KeyItem(KeyItemCategoryType.Music, "Death Star Chase (Lair)", true, "A recording of \"Death Star Chase (Lair)\" in your music collection.")]
        SongDeathStarChaseLair = 3523,
        [KeyItem(KeyItemCategoryType.Music, "Battle 6", true, "A recording of \"Battle 6\" in your music collection.")]
        SongBattle6 = 3524,
        [KeyItem(KeyItemCategoryType.Music, "Death Star Chase", true, "A recording of \"Death Star Chase\" in your music collection.")]
        SongDeathStarChase = 3525,
        [KeyItem(KeyItemCategoryType.Music, "Imperial Base", true, "A recording of \"Imperial Base\" in your music collection.")]
        SongImperialBase = 3526,
        [KeyItem(KeyItemCategoryType.Music, "Battle 7", true, "A recording of \"Battle 7\" in your music collection.")]
        SongBattle7 = 3527,
        [KeyItem(KeyItemCategoryType.Music, "Battle of Yavin", true, "A recording of \"Battle of Yavin\" in your music collection.")]
        SongBattleOfYavin = 3530,
        [KeyItem(KeyItemCategoryType.Music, "Duel of the Fates (Battle)", true, "A recording of \"Duel of the Fates (Battle)\" in your music collection.")]
        SongDuelOfTheFatesBattle = 3531,
        [KeyItem(KeyItemCategoryType.Music, "Battle of Hoth", true, "A recording of \"Battle of Hoth\" in your music collection.")]
        SongBattleOfHoth = 3532,
        [KeyItem(KeyItemCategoryType.Music, "Jedi Battle 1", true, "A recording of \"Jedi Battle 1\" in your music collection.")]
        SongJediBattle1 = 3533,
        [KeyItem(KeyItemCategoryType.Music, "Space Battle", true, "A recording of \"Space Battle\" in your music collection.")]
        SongSpaceBattle = 3534,
        [KeyItem(KeyItemCategoryType.Music, "Tatooine Town", true, "A recording of \"Tatooine Town\" in your music collection.")]
        SongTatooineTown = 3535,
        [KeyItem(KeyItemCategoryType.Music, "Mandalorian Tore", true, "A recording of \"Mandalorian Tore\" in your music collection.")]
        SongMandalorianTore = 3536,
        [KeyItem(KeyItemCategoryType.Music, "Across the Stars", true, "A recording of \"Across the Stars\" in your music collection.")]
        SongAcrossTheStars = 3537,
        [KeyItem(KeyItemCategoryType.Music, "Rebel Alliance", true, "A recording of \"Rebel Alliance\" in your music collection.")]
        SongRebelAlliance = 3538,
        [KeyItem(KeyItemCategoryType.Music, "Lando Calrissian", true, "A recording of \"Lando Calrissian\" in your music collection.")]
        SongLandoCalrissian = 3539,
        [KeyItem(KeyItemCategoryType.Music, "Jedi Training", true, "A recording of \"Jedi Training\" in your music collection.")]
        SongJediTraining = 3540,
        [KeyItem(KeyItemCategoryType.Music, "Learning the Force", true, "A recording of \"Learning the Force\" in your music collection.")]
        SongLearningTheForce = 3541,
        [KeyItem(KeyItemCategoryType.Music, "The Force", true, "A recording of \"The Force\" in your music collection.")]
        SongTheForce = 3542,
        [KeyItem(KeyItemCategoryType.Music, "Yoda and the Force", true, "A recording of \"Yoda and the Force\" in your music collection.")]
        SongYodaAndTheForce = 3543,
        [KeyItem(KeyItemCategoryType.Music, "Han Solo and Leia", true, "A recording of \"Han Solo and Leia\" in your music collection.")]
        SongHanSoloAndLeia = 3544,
        [KeyItem(KeyItemCategoryType.Music, "Luke and Leia", true, "A recording of \"Luke and Leia\" in your music collection.")]
        SongLukeAndLeia = 3545,
        [KeyItem(KeyItemCategoryType.Music, "Leia Organa", true, "A recording of \"Leia Organa\" in your music collection.")]
        SongLeiaOrgana = 3546,
        [KeyItem(KeyItemCategoryType.Music, "Luke Skywalker", true, "A recording of \"Luke Skywalker\" in your music collection.")]
        SongLukeSkywalker = 3547,
        [KeyItem(KeyItemCategoryType.Music, "Yoda", true, "A recording of \"Yoda\" in your music collection.")]
        SongYoda = 3548,
        [KeyItem(KeyItemCategoryType.Music, "Stormtroopers", true, "A recording of \"Stormtroopers\" in your music collection.")]
        SongStormtroopers = 3549,
        [KeyItem(KeyItemCategoryType.Music, "Sith Training", true, "A recording of \"Sith Training\" in your music collection.")]
        SongSithTraining = 3550,
        [KeyItem(KeyItemCategoryType.Music, "The Shadow of Vader", true, "A recording of \"The Shadow of Vader\" in your music collection.")]
        SongTheShadowOfVader = 3551,
        [KeyItem(KeyItemCategoryType.Music, "Dark Deeds", true, "A recording of \"Dark Deeds\" in your music collection.")]
        SongDarkDeeds = 3552,
        [KeyItem(KeyItemCategoryType.Music, "Darth Vader", true, "A recording of \"Darth Vader\" in your music collection.")]
        SongDarthVader = 3553,
        [KeyItem(KeyItemCategoryType.Music, "Dark Nihilus", true, "A recording of \"Dark Nihilus\" in your music collection.")]
        SongDarkNihilus = 3556,
        [KeyItem(KeyItemCategoryType.Music, "Cantina 4", true, "A recording of \"Cantina 4\" in your music collection.")]
        SongCantina4 = 3560,
        [KeyItem(KeyItemCategoryType.Music, "Jungle", true, "A recording of \"Jungle\" in your music collection.")]
        SongJungle = 3561,
        [KeyItem(KeyItemCategoryType.Music, "Dagobah", true, "A recording of \"Dagobah\" in your music collection.")]
        SongDagobah = 3562,
        [KeyItem(KeyItemCategoryType.Music, "Hoth", true, "A recording of \"Hoth\" in your music collection.")]
        SongHoth = 3563,
        [KeyItem(KeyItemCategoryType.Music, "Mos Eisley", true, "A recording of \"Mos Eisley\" in your music collection.")]
        SongMosEisley = 3565,
        [KeyItem(KeyItemCategoryType.Music, "Tusken Camp", true, "A recording of \"Tusken Camp\" in your music collection.")]
        SongTuskenCamp = 3566,
        [KeyItem(KeyItemCategoryType.Music, "Tusken", true, "A recording of \"Tusken\" in your music collection.")]
        SongTusken = 3567,
        [KeyItem(KeyItemCategoryType.Music, "Tatooine 1", true, "A recording of \"Tatooine 1\" in your music collection.")]
        SongTatooine1 = 3568,
        [KeyItem(KeyItemCategoryType.Music, "Tatooine 2", true, "A recording of \"Tatooine 2\" in your music collection.")]
        SongTatooine2 = 3569,
        [KeyItem(KeyItemCategoryType.Music, "Tatooine 3", true, "A recording of \"Tatooine 3\" in your music collection.")]
        SongTatooine3 = 3570,
        [KeyItem(KeyItemCategoryType.Music, "Battle of Carkoon", true, "A recording of \"Battle of Carkoon\" in your music collection.")]
        SongBattleOfCarkoon = 3571,
        [KeyItem(KeyItemCategoryType.Music, "Battle of Endor", true, "A recording of \"Battle of Endor\" in your music collection.")]
        SongBattleOfEndor = 3572,
        [KeyItem(KeyItemCategoryType.Music, "Cantina 5", true, "A recording of \"Cantina 5\" in your music collection.")]
        SongCantina5 = 3575,
        [KeyItem(KeyItemCategoryType.Music, "Cantina 6", true, "A recording of \"Cantina 6\" in your music collection.")]
        SongCantina6 = 3576,
        [KeyItem(KeyItemCategoryType.Music, "Cantina 7", true, "A recording of \"Cantina 7\" in your music collection.")]
        SongCantina7 = 3577,
        [KeyItem(KeyItemCategoryType.Music, "Race 1", true, "A recording of \"Race 1\" in your music collection.")]
        SongRace1 = 3578,
        [KeyItem(KeyItemCategoryType.Music, "Race 2", true, "A recording of \"Race 2\" in your music collection.")]
        SongRace2 = 3579,
        [KeyItem(KeyItemCategoryType.Music, "Race 3", true, "A recording of \"Race 3\" in your music collection.")]
        SongRace3 = 3580,
        [KeyItem(KeyItemCategoryType.Music, "Race 4", true, "A recording of \"Race 4\" in your music collection.")]
        SongRace4 = 3581,
        [KeyItem(KeyItemCategoryType.Music, "Race 5", true, "A recording of \"Race 5\" in your music collection.")]
        SongRace5 = 3582,
        [KeyItem(KeyItemCategoryType.Music, "Race 6", true, "A recording of \"Race 6\" in your music collection.")]
        SongRace6 = 3583,
        [KeyItem(KeyItemCategoryType.Music, "FF Stand", true, "A recording of \"FF Stand\" in your music collection.")]
        SongFFStand = 3585,
        [KeyItem(KeyItemCategoryType.Music, "SWG Ambush", true, "A recording of \"SWG Ambush\" in your music collection.")]
        SongSWGAmbush = 3586,
        [KeyItem(KeyItemCategoryType.Music, "SWG Bounty", true, "A recording of \"SWG Bounty\" in your music collection.")]
        SongSWGBounty = 3587,
        [KeyItem(KeyItemCategoryType.Music, "SWG Break Neck", true, "A recording of \"SWG Break Neck\" in your music collection.")]
        SongSWGBreakNeck = 3588,
        [KeyItem(KeyItemCategoryType.Music, "Galactic Conq", true, "A recording of \"Galactic Conq\" in your music collection.")]
        SongGalacticConq = 3589,
        [KeyItem(KeyItemCategoryType.Music, "Han and Q", true, "A recording of \"Han and Q\" in your music collection.")]
        SongHanAndQ = 3590,
        [KeyItem(KeyItemCategoryType.Music, "JP Alle", true, "A recording of \"JP Alle\" in your music collection.")]
        SongJPAlle = 3591,
        [KeyItem(KeyItemCategoryType.Music, "JP Good", true, "A recording of \"JP Good\" in your music collection.")]
        SongJPGood = 3592,
        [KeyItem(KeyItemCategoryType.Music, "JP Train", true, "A recording of \"JP Train\" in your music collection.")]
        SongJPTrain = 3593,
        [KeyItem(KeyItemCategoryType.Music, "JW Han", true, "A recording of \"JW Han\" in your music collection.")]
        SongJWHan = 3595,
        [KeyItem(KeyItemCategoryType.Music, "JP Break Out", true, "A recording of \"JP Break Out\" in your music collection.")]
        SongJPBreakOut = 3596,
        [KeyItem(KeyItemCategoryType.Music, "JP Chicken", true, "A recording of \"JP Chicken\" in your music collection.")]
        SongJPChicken = 3597,
        [KeyItem(KeyItemCategoryType.Music, "JP Closet", true, "A recording of \"JP Closet\" in your music collection.")]
        SongJPCloset = 3598,
        [KeyItem(KeyItemCategoryType.Music, "JP Corellia", true, "A recording of \"JP Corellia\" in your music collection.")]
        SongJPCorellia = 3599,
        [KeyItem(KeyItemCategoryType.Music, "JP Dice", true, "A recording of \"JP Dice\" in your music collection.")]
        SongJPDice = 3600,
        [KeyItem(KeyItemCategoryType.Music, "JP Flying C", true, "A recording of \"JP Flying C\" in your music collection.")]
        SongJPFlyingC = 3601,
        [KeyItem(KeyItemCategoryType.Music, "JP L3", true, "A recording of \"JP L3\" in your music collection.")]
        SongJPL3 = 3602,
        [KeyItem(KeyItemCategoryType.Music, "JP Listen", true, "A recording of \"JP Listen\" in your music collection.")]
        SongJPListen = 3603,
        [KeyItem(KeyItemCategoryType.Music, "JP MA", true, "A recording of \"JP MA\" in your music collection.")]
        SongJPMA = 3604,
        [KeyItem(KeyItemCategoryType.Music, "JP Maw", true, "A recording of \"JP Maw\" in your music collection.")]
        SongJPMaw = 3605,
        [KeyItem(KeyItemCategoryType.Music, "JP Meet", true, "A recording of \"JP Meet\" in your music collection.")]
        SongJPMeet = 3606,
        [KeyItem(KeyItemCategoryType.Music, "JP Mine", true, "A recording of \"JP Mine\" in your music collection.")]
        SongJPMine = 3607,
        [KeyItem(KeyItemCategoryType.Music, "JP Sav", true, "A recording of \"JP Sav\" in your music collection.")]
        SongJPSav = 3608,
        [KeyItem(KeyItemCategoryType.Music, "JP Seat", true, "A recording of \"JP Seat\" in your music collection.")]
        SongJPSeat = 3609,
        [KeyItem(KeyItemCategoryType.Music, "JP Space", true, "A recording of \"JP Space\" in your music collection.")]
        SongJPSpace = 3610,
        [KeyItem(KeyItemCategoryType.Music, "JP Therap", true, "A recording of \"JP Therap\" in your music collection.")]
        SongJPTherap = 3611,
        [KeyItem(KeyItemCategoryType.Music, "NierAuto", true, "A recording of \"NierAuto\" in your music collection.")]
        SongNierAuto = 3612,
        [KeyItem(KeyItemCategoryType.Music, "Revan", true, "A recording of \"Revan\" in your music collection.")]
        SongRevan = 3613,
        [KeyItem(KeyItemCategoryType.Music, "Sims buy", true, "A recording of \"Sims buy\" in your music collection.")]
        SongSimsBuy = 3614,
        [KeyItem(KeyItemCategoryType.Music, "SW Battle Heroic", true, "A recording of \"SW Battle Heroic\" in your music collection.")]
        SongSWBattleHeroic = 3615,
        [KeyItem(KeyItemCategoryType.Music, "Xeno Day", true, "A recording of \"Xeno Day\" in your music collection.")]
        SongXenoDay = 3616,
        [KeyItem(KeyItemCategoryType.Music, "Xeno Other", true, "A recording of \"Xeno Other\" in your music collection.")]
        SongXenoOther = 3617,
        [KeyItem(KeyItemCategoryType.Music, "Funk Goes On", true, "A recording of \"Funk Goes On\" in your music collection.")]
        SongFunkGoesOn = 3618,
        [KeyItem(KeyItemCategoryType.Music, "Sugaan Essena", true, "A recording of \"Sugaan Essena\" in your music collection.")]
        SongSugaanEssena = 3619,
        [KeyItem(KeyItemCategoryType.Music, "Eseerin Vasahina", true, "A recording of \"Eseerin Vasahina\" in your music collection.")]
        SongEseerinVasahina = 3620,
        [KeyItem(KeyItemCategoryType.Music, "Spiceman 4", true, "A recording of \"Spiceman 4\" in your music collection.")]
        SongSpiceman4 = 3621,
        [KeyItem(KeyItemCategoryType.Music, "Ebon Flow", true, "A recording of \"Ebon Flow\" in your music collection.")]
        SongEbonFlow = 3622,
        [KeyItem(KeyItemCategoryType.Music, "High Waves", true, "A recording of \"High Waves\" in your music collection.")]
        SongHighWaves = 3623,
        [KeyItem(KeyItemCategoryType.Music, "Sublime Raid II 4", true, "A recording of \"Sublime Raid II 4\" in your music collection.")]
        SongSublimeRaidII4 = 3624,
        [KeyItem(KeyItemCategoryType.Music, "Shortpaw's Dance", true, "A recording of \"Shortpaw's Dance\" in your music collection.")]
        SongShortpawsDance = 3625,
        [KeyItem(KeyItemCategoryType.Music, "Zanara", true, "A recording of \"Zanara\" in your music collection.")]
        SongZanara = 3626,
        [KeyItem(KeyItemCategoryType.Music, "Who Mourns for Taris", true, "A recording of \"Who Mourns for Taris\" in your music collection.")]
        SongWhoMournsForTaris = 3627,
        [KeyItem(KeyItemCategoryType.Music, "Rambler's Reach", true, "A recording of \"Rambler's Reach\" in your music collection.")]
        SongRamblersReach = 3628,
        [KeyItem(KeyItemCategoryType.Music, "The Forests Secret", true, "A recording of \"The Forests Secret\" in your music collection.")]
        SongTheForestsSecret = 3629,
        [KeyItem(KeyItemCategoryType.Music, "Basalt Scars", true, "A recording of \"Basalt Scars\" in your music collection.")]
        SongBasaltScars = 3630,
        [KeyItem(KeyItemCategoryType.Music, "Flight", true, "A recording of \"Flight\" in your music collection.")]
        SongFlight = 3631,
        [KeyItem(KeyItemCategoryType.Music, "To the Rescue", true, "A recording of \"To the Rescue\" in your music collection.")]
        SongToTheRescue = 3632,
        [KeyItem(KeyItemCategoryType.Music, "Course Collider", true, "A recording of \"Course Collider\" in your music collection.")]
        SongCourseCollider = 3633,
        [KeyItem(KeyItemCategoryType.Music, "Cold Dawn", true, "A recording of \"Cold Dawn\" in your music collection.")]
        SongColdDawn = 3634,
        [KeyItem(KeyItemCategoryType.Music, "Vode An", true, "A recording of \"Vode An\" in your music collection.")]
        SongVodeAn = 3635,
        [KeyItem(KeyItemCategoryType.Music, "Mando Block", true, "A recording of \"Mando Block\" in your music collection.")]
        SongMandoBlock = 3636,
        [KeyItem(KeyItemCategoryType.Music, "Buir Aden", true, "A recording of \"Buir Aden\" in your music collection.")]
        SongBuirAden = 3637,
        [KeyItem(KeyItemCategoryType.Music, "The Marshal's Tale", true, "A recording of \"The Marshal's Tale\" in your music collection.")]
        SongTheMarshalsTale = 3638,
        [KeyItem(KeyItemCategoryType.Music, "Hey, Mando!", true, "A recording of \"Hey, Mando!\" in your music collection.")]
        SongHeyMando = 3639,
        [KeyItem(KeyItemCategoryType.Music, "Hammer Time", true, "A recording of \"Hammer Time\" in your music collection.")]
        SongHammerTime = 3640,
        [KeyItem(KeyItemCategoryType.Music, "Face To Face", true, "A recording of \"Face To Face\" in your music collection.")]
        SongFaceToFace = 3641,
        [KeyItem(KeyItemCategoryType.Music, "Back For Beskar", true, "A recording of \"Back For Beskar\" in your music collection.")]
        SongBackForBeskar = 3642,
        [KeyItem(KeyItemCategoryType.Music, "Noctilium", true, "A recording of \"Noctilium\" in your music collection.")]
        SongNoctilium = 3643,

	}

	public class KeyItemAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public KeyItemCategoryType Category { get; set; }
        public bool IsActive { get; set; }

        public KeyItemAttribute(KeyItemCategoryType category, string name, bool isActive, string description)
        {
            Category = category;
            Name = name;
            IsActive = isActive;
            Description = description;
        }
    }
}
