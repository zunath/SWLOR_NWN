# Weather area audit

Audit of all 453 ARE resources and their matching GIT area locals, updated 2026-09-12.
Resource filenames, rather than stale embedded ResRef fields, identify matching files.
Flags: 1 = interior, 2 = underground, 4 = natural. Either shelter bit stops exposure.

Native-only areas retain their authored visual settings and have no scripted hazard damage.
They are generic prefabs or technical areas without an assigned planet climate.
Interior tileset classifications were cross-checked against HAK `.set` GENERAL/Interior metadata;
three authored open-air layouts deliberately retain outdoor flags. This is a source audit,
not an in-client inspection of every tile. See [Weather.md](Weather.md) for deployment and playtests.

| Policy | Areas |
|---|---:|
| Authored native only | 67 |
| Scripted: Dantooine | 18 |
| Scripted: Dathomir | 11 |
| Scripted: Hutlar | 8 |
| Scripted: Kashyyyk | 5 |
| Scripted: Korriban | 10 |
| Scripted: MonCala | 6 |
| Scripted: Ossus | 4 |
| Scripted: SmugglersMoon | 8 |
| Scripted: Tatooine | 44 |
| Scripted: Viscara | 22 |
| Sheltered | 250 |

| Resource | Area | Flags | Weather policy | Audit note |
|---|---|---:|---|---|
| `anc_dsrt_speeder` | Tatooine - Anchorhead - To Mos Eisley | 4 | Scripted: Tatooine |  |
| `anchor_entreenor` | Tatooine - Anchorhead - North Entrance | 4 | Scripted: Tatooine |  |
| `anchor_entreesud` | Tatooine - Anchorhead - South Entry | 4 | Scripted: Tatooine |  |
| `anchor_road_est` | Tatooine - Anchorhead - Sandy Dune Desert - North East | 4 | Scripted: Tatooine |  |
| `anchor_roche01` | Tatooine - Rocky Pass | 4 | Scripted: Tatooine |  |
| `apartment_002` | Small Player Apartment - Style 1 | 1 | Sheltered |  |
| `apartment_2` | Medium Player Apartment - Style 1 | 1 | Sheltered |  |
| `apartment_3` | Large Player Apartment - Style 1 | 1 | Sheltered |  |
| `ar_pw_indusvel` | [Prefab] City, Industrial Slum | 0 | Authored native only |  |
| `ar_scor_kacademy` | Korriban - Sith Academy | 3 | Sheltered |  |
| `ar_scor_korrcan` | Korriban - Starport - Cantina | 1 | Sheltered |  |
| `ar_scor_kortemp` | Korriban - Valley Temples | 0 | Scripted: Korriban |  |
| `ar_scor_kvalinte` | Korriban - Valley Temple Interiors | 3 | Sheltered |  |
| `area` | Viscara - Cavern | 3 | Sheltered |  |
| `area_template` | area_template | 4 | Authored native only |  |
| `bank` | Building Template - Bank Style 1 | 1 | Sheltered |  |
| `cantina` | Building Template - Cantina Style 1 | 3 | Sheltered |  |
| `canyon_001` | Tatooine - Deep Canyon | 4 | Scripted: Tatooine |  |
| `char_migration` | *Character Rebuild | 4 | Authored native only |  |
| `city_hall` | Building Template - City Hall Style 1 | 1 | Sheltered |  |
| `coolship` | [Prefab] Ship - The Gwenivere | 3 | Sheltered |  |
| `coxxian_hq` | Viscara - Coxxion Headquarters | 1 | Sheltered |  |
| `cz220shipbreaker` | CZ-220 - Breaker Yard | 1 | Sheltered |  |
| `cz220shipbreakin` | CZ-220 - Breaker Bay
 | 1 | Sheltered |  |
| `czs220_hangar` | CZ-220 - Hangar | 1 | Sheltered |  |
| `czs220_maintlvl` | CZ-220 - Maintenance Level | 3 | Sheltered |  |
| `dan_battlemon` | Dantooine - Battle Monster Gym | 1 | Sheltered |  |
| `dan_centcolony` | Dantooine - Colony Central | 0 | Scripted: Dantooine |  |
| `dan_colony` | Dantooine - Colony | 0 | Scripted: Dantooine |  |
| `dan_colonyfarms` | Dantooine - Colony South Farms | 0 | Scripted: Dantooine |  |
| `dan_colonyspa` | Dantooine - Colony Spa | 1 | Sheltered |  |
| `dan_crafterbase` | Dantooine - Central Craft Base | 1 | Sheltered |  |
| `dan_crystalcavez` | Dantooine - Canyon Crystal Caves | 7 | Sheltered |  |
| `dan_crystalflied` | Dantooine - Canyon Crystal Fields | 0 | Scripted: Dantooine |  |
| `dan_destroyfarm` | Dantooine - Ruined Farmlands | 0 | Scripted: Dantooine |  |
| `dan_enclosemount` | Dantooine - Enclosed Mountain | 0 | Scripted: Dantooine |  |
| `dan_fieldtrail` | Dantooine - Field Trail | 0 | Scripted: Dantooine |  |
| `dan_hiddenmount` | Dantooine - Hidden Trail | 0 | Scripted: Dantooine |  |
| `dan_interiors` | Dantooine - Interior | 1 | Sheltered |  |
| `dan_iriazfarm` | Dantooine - Iriaz Fields | 0 | Scripted: Dantooine |  |
| `dan_jantacaves` | Dantooine - Janta Caves | 3 | Sheltered |  |
| `dan_jedienclave` | Dantooine - Jedi Enclave | 1 | Sheltered |  |
| `dan_jedienlibry` | Dantooine - Jedi Enclave Library | 3 | Sheltered |  |
| `dan_jedlibrary` | Dantooine - Jedi Library | 1 | Sheltered |  |
| `dan_jungle1` | Dantooine - Forsaken Jungles | 4 | Scripted: Dantooine |  |
| `dan_junglemount` | Dantooine - Jungle Mountain | 0 | Scripted: Dantooine |  |
| `dan_kathden` | Dantooine - Janta Caves Lower | 3 | Sheltered |  |
| `dan_kinrathcave` | Dantooine - Kinrath Caves | 7 | Sheltered |  |
| `dan_lakencave` | Dantooine - Lake | 2 | Sheltered |  |
| `dan_medical` | Dantooine - Medical Safehouse | 0 | Scripted: Dantooine |  |
| `dan_medinterior` | Dantooine - Medical Interior | 1 | Sheltered |  |
| `dan_mountcrycave` | Dantooine - Mountain Crystal Caves | 1 | Sheltered |  |
| `dan_playerland2` | Dantooine - Clear Jungles | 0 | Scripted: Dantooine |  |
| `dan_playerlands` | Dantooine - Tranquil Plains | 0 | Scripted: Dantooine |  |
| `dan_repgarrison` | Dantooine - Republic Garrison | 0 | Scripted: Dantooine |  |
| `dan_repinside` | Dantooine - Republic Base | 1 | Sheltered |  |
| `dan_repubmed` | Dantooine - Republic Med Center | 1 | Sheltered |  |
| `dan_rivercanyon` | Dantooine - Canyon  River | 0 | Scripted: Dantooine |  |
| `dan_smugcaverns` | Dantooine - Smuggler Caverns | 7 | Sheltered |  |
| `dan_tribefields` | Dantooine - South Fields | 0 | Scripted: Dantooine |  |
| `dan_warehouse` | Dantooine - Abandoned Warehouse | 3 | Sheltered |  |
| `dan_wildplain` | Dantooine - Wild Plains | 0 | Scripted: Dantooine |  |
| `dantooineorbit` | Space - Dantooine Orbit | 1 | Sheltered |  |
| `dath_caveruins1` | Dathomir - Cave Ruins | 3 | Sheltered |  |
| `dath_cz_baseok` | Dathomir - Czerka Base | 0 | Scripted: Dathomir |  |
| `dath_desert` | Dathomir - Desert | 0 | Scripted: Dathomir |  |
| `dath_grottos` | Dathomir - Grottos | 0 | Scripted: Dathomir |  |
| `dath_hidtunnels` | Dathomir - Hidden Cave | 7 | Sheltered |  |
| `dath_landingpad` | Dathomir - Jungle Landing | 0 | Scripted: Dathomir |  |
| `dath_mountains` | Dathomir - Mountains | 0 | Scripted: Dathomir |  |
| `dath_mountcaves` | Dathomir - Mountain Caves | 3 | Sheltered |  |
| `dath_ruin_base` | Dathomir - Ruin Base | 3 | Sheltered |  |
| `dath_tarnjungles` | Dathomir - Tarnished Jungles | 0 | Scripted: Dathomir |  |
| `dath_tranjungl2` | Dathomir - Tarnished Jungles North | 0 | Scripted: Dathomir |  |
| `dath_tribevill` | Dathomir - Tribe Village | 0 | Scripted: Dathomir |  |
| `dath_waterfallru` | Dathomir - Waterfall Ruins | 0 | Scripted: Dathomir |  |
| `dath_west_desert` | Dathomir - Desert West Side | 0 | Scripted: Dathomir |  |
| `dathgrottocavern` | Dathomir - Grotto Caverns | 7 | Sheltered |  |
| `dmfi_custom_enc` | *DMFI Custom Encounter Region | 4 | Authored native only |  |
| `druz_shalim` | Viscara - Druzer | 4 | Scripted: Viscara |  |
| `druz_wpnarm` | Viscara - Druzer Store | 1 | Sheltered |  |
| `ebonhawk` | [Prefab] Ebon Hawk | 3 | Sheltered |  |
| `esriauncharted` | [Prefab] Esria - Uncharted Island | 4 | Authored native only |  |
| `foszchimed` | Viscara - Chi-Med | 1 | Sheltered | Shelter flags corrected |
| `foszestate` | [Prefab] Former Fosz Estate | 1 | Sheltered | Shelter flags corrected |
| `foszestateext` | [Prefab] Former Fosz Estate - Exterior | 0 | Scripted: Viscara |  |
| `gl_ksthacdmyextr` | [Prefab] Korriban - Sith Academy Exterior | 4 | Authored native only |  |
| `hiddenquestzone` | *Hidden Access Area | 3 | Sheltered |  |
| `house_int_1` | Small Player Home - Style 1 | 1 | Sheltered |  |
| `house_int_10` | Small Player Home - Style 4 | 5 | Sheltered |  |
| `house_int_11` | Medium Player Home - Style 4 | 5 | Sheltered |  |
| `house_int_12` | Large Player Home - Style 4 | 5 | Sheltered |  |
| `house_int_2` | Medium Player Home - Style 1 | 1 | Sheltered |  |
| `house_int_3` | Large Player Home - Style 1 | 1 | Sheltered |  |
| `house_int_4` | Small Player Home - Style 2 | 5 | Sheltered |  |
| `house_int_5` | Medium Player Home - Style 2 | 5 | Sheltered |  |
| `house_int_6` | Large Player Home - Style 2 | 5 | Sheltered |  |
| `house_int_7` | Small Player Home - Style 3 | 1 | Sheltered |  |
| `house_int_8` | Medium Player Home - Style 3 | 1 | Sheltered |  |
| `house_int_9` | Large Player Home - Style 3 | 5 | Sheltered |  |
| `hutlar_frozen_wa` | Hutlar - Frozen Wastes | 0 | Scripted: Hutlar | Humidity +9: permanent snow |
| `hutlar_orbit` | Space - Hutlar Orbit | 1 | Sheltered |  |
| `hutlar_outpost` | Hutlar - Outpost | 0 | Scripted: Hutlar |  |
| `hutlar_qion` | Hutlar - Qion Tundra | 0 | Scripted: Hutlar |  |
| `hutlar_smuggleba` | Hutlar - Abandoned Outpost | 1 | Sheltered | Shelter flags corrected |
| `hutlar_testsite` | Hutlar - Cloning Test Site | 0 | Scripted: Hutlar |  |
| `hutlar_valley` | Hutlar - Qion Valley | 0 | Scripted: Hutlar |  |
| `hutlar_wastes_ca` | Hutlar - Frozen Caves | 7 | Sheltered |  |
| `jedishuttle` | [Prefab] Ship - Nova Shuttle | 1 | Sheltered |  |
| `jeditemp_int` | Viscara - Jedi Temple Interior | 1 | Sheltered |  |
| `ka_ar_czweaparen` | Smuggler's Moon - Czerka Blast-Safe Cell | 0 | Scripted: SmugglersMoon |  |
| `ka_crnt_corellia` | [Prefab] Corellia, Bridge | 0 | Authored native only |  |
| `ka_dropsh_cr_sno` | [Prefab] Winter Crash Site | 0 | Authored native only |  |
| `ka_dropship_spac` | [Prefab] Dropship | 1 | Sheltered |  |
| `ka_drps_crsh_kor` | [Prefab] Korriban Crash Site | 0 | Authored native only |  |
| `ka_drps_crsh_vis` | [Prefab] Viscaran Crash Site | 4 | Authored native only |  |
| `ka_trenchpr1` | [Prefab] Trench Warfare | 0 | Authored native only |  |
| `ka_vis_mntscrapy` | [Prefab] Scrapyard | 0 | Authored native only |  |
| `karthviscdun1` | [Prefab] Old Mine | 3 | Sheltered |  |
| `kash_village` | Kashyyyk - Village | 0 | Scripted: Kashyyyk | Explicit climate override |
| `kashyyyk_camp` | Kashyyyk - War Camp | 4 | Scripted: Kashyyyk | Explicit climate override |
| `kashyyykpaths` | Kashyyyk - Forest Paths | 4 | Scripted: Kashyyyk | Explicit climate override |
| `kashyyykshadow` | Kashyyyk - Shadowlands | 4 | Scripted: Kashyyyk | Explicit climate override |
| `korr_cavern` | Korriban - Caverns | 3 | Sheltered | Shelter flags corrected |
| `korr_crypt_zil` | Korriban - Sith Crypt | 3 | Sheltered | Shelter flags corrected |
| `korr_plains` | Korriban - Dunes | 4 | Scripted: Korriban |  |
| `korr_ravine` | Korriban - Ravine | 4 | Scripted: Korriban |  |
| `korr_valley` | Korriban - Valley | 0 | Scripted: Korriban |  |
| `korr_waste2_zil` | Korriban - Wastelands - South | 4 | Scripted: Korriban |  |
| `korr_waste_zil` | Korriban - Wastelands - North | 4 | Scripted: Korriban |  |
| `korribanlandingp` | Korriban - Starport | 4 | Scripted: Korriban |  |
| `manda_facility` | Viscara - Mandalorian Facility | 3 | Sheltered |  |
| `medical_center` | Building Template - Medical Center Style 1 | 1 | Sheltered |  |
| `moncala_swamp` | Mon Cala - Sunkenhedge Swamps | 0 | Scripted: MonCala |  |
| `moncalacifacilit` | Mon Cala - Coral Isles - Facility | 3 | Sheltered |  |
| `moncalacorali001` | Mon Cala - Coral Isles - Outer | 4 | Scripted: MonCala |  |
| `moncalacoralisle` | Mon Cala - Coral Isles - Inner | 4 | Scripted: MonCala |  |
| `moncaladaccityex` | Mon Cala - Dac City - The "Elite" Hotel | 3 | Sheltered |  |
| `moncaladaccitysu` | Mon Cala - Dac City - Surface | 4 | Scripted: MonCala |  |
| `moncaladungeon1` | Mon Cala - Sharptooth Jungle - Caves | 3 | Sheltered |  |
| `moncalajungelsu` | Mon Cala - Sharptooth Jungle - South | 0 | Scripted: MonCala |  |
| `moncalaorbit` | Space - Mon Cala Orbit | 1 | Sheltered |  |
| `moncalawildjungl` | Mon Cala - Sharptooth Jungle - North | 0 | Scripted: MonCala |  |
| `moseis_dow_ca001` | Tatooine - Mos Eisley - Docking Bay 94 | 4 | Scripted: Tatooine |  |
| `moseis_dow_can` | Tatooine - Mos Esper - Dowager Queen | 4 | Scripted: Tatooine |  |
| `moseis_exit_sud` | Tatooine - Mos Esper - South Entrance | 4 | Scripted: Tatooine |  |
| `moseis_lucky` | Tatooine - Mos Esper - Cantina Quarter | 4 | Scripted: Tatooine |  |
| `moseis_sand_001` | Tatooine - Dune Desert | 4 | Scripted: Tatooine |  |
| `moseis_sand_003` | Tatooine - Deep Canyon | 4 | Scripted: Tatooine |  |
| `moseis_sand_004` | Tatooine - Dune Desert | 4 | Scripted: Tatooine |  |
| `moseis_sand_005` | Tatooine - Dune Desert | 4 | Scripted: Tatooine |  |
| `mus_ironwoods_ka` | [Prefab] Lava Planet - Iron Woods | 0 | Authored native only |  |
| `mustafar_lava_ka` | [Prefab] Lava Planet - Mine | 0 | Authored native only |  |
| `na_ka_pref_scene` | [Prefab] City, Battle | 0 | Authored native only |  |
| `nanostation015` | CZ-220 - Offices & Labs | 3 | Sheltered |  |
| `narshad_czklab` | [Prefab] Czerka Tower, Exterior | 0 | Authored native only |  |
| `narshadaar_midoc` | [Prefab] Nar Shaddaa - Midcity Docks | 0 | Authored native only |  |
| `narshadaar_pr001` | [Prefab] Promenade Casino | 1 | Sheltered | Shelter flags corrected |
| `narshadaar_promi` | [Prefab] Promenade | 0 | Authored native only |  |
| `narshadorbit` | Space - Smuggler's Moon Orbit | 1 | Sheltered |  |
| `nashada_czlabf2` | [Prefab] Czerka Lab Floor 2 | 1 | Sheltered | Shelter flags corrected |
| `nashadaa_czlabin` | [Prefab] Czerka Lab Floor 1 | 1 | Sheltered | Shelter flags corrected |
| `no_access` | *No Access | 0 | Authored native only |  |
| `ns_comrcial_ka` | [Prefab] City, Commerce | 0 | Authored native only |  |
| `ns_industrialsec` | [Prefab] Industrial Sector | 0 | Authored native only |  |
| `ns_prfb_nsinteri` | [Prefab] Nar Shaddaa - Interior | 1 | Sheltered | Shelter flags corrected |
| `ooc_area` | *Welcome to Star Wars: Legends of the Old Republic! | 1 | Sheltered |  |
| `ossu_wastesruin` | Ossus - Wastes Ruins | 0 | Scripted: Ossus | Explicit climate override |
| `ossus_cavefacili` | Ossus - Wastes Cave Facility | 3 | Sheltered | Shelter flags corrected |
| `ossus_landstart` | Ossus - Landing Pads | 0 | Scripted: Ossus | Explicit climate override |
| `ossus_tempinisde` | Ossus - Jedi Temple | 3 | Sheltered |  |
| `ossus_wastejungl` | Ossus - Waste Jungle | 4 | Scripted: Ossus | Explicit climate override |
| `ossustemp` | Ossus - Ruin Temple | 0 | Scripted: Ossus | Explicit climate override |
| `player_rnd` | Building Template - Lab Style 1 | 5 | Sheltered |  |
| `playerap_l_fur` | Large Player Apartment - Style 2 (Furnished) | 1 | Sheltered |  |
| `playerap_l_unf` | Large Player Apartment - Style 2 (Unfurnished) | 1 | Sheltered |  |
| `playerap_m_fur` | Medium Player Apartment - Style 2 (Furnished) | 1 | Sheltered |  |
| `playerap_m_unf` | Medium Player Apartment - Style 2 (Unfurnished) | 1 | Sheltered |  |
| `playerap_s_fur` | Small Player Apartment - Style 2 (Furnished) | 1 | Sheltered |  |
| `playerap_s_unf` | Small Player Apartment - Style 2 (Unfurnished) | 1 | Sheltered |  |
| `pref_caves` | [Prefab] Caves | 7 | Sheltered |  |
| `pref_facilidark` | [Prefab] Facility, Dark | 1 | Sheltered |  |
| `pref_facility` | [Prefab] Facility | 1 | Sheltered |  |
| `pref_forest` | [Prefab] Forest | 0 | Authored native only |  |
| `pref_frozen` | [Prefab] Frozen Waste | 4 | Authored native only |  |
| `pref_hammerhead` | [Prefab] Hammerhead Cruiser | 1 | Sheltered |  |
| `pref_interior` | [Prefab] Interior, Generic | 1 | Sheltered |  |
| `pref_leviathan` | [Prefab] Leviathan, Revan's Flagship, Bridge | 1 | Sheltered |  |
| `pref_ship1` | [Prefab] Ship Bridge | 1 | Sheltered |  |
| `pref_shipbattle` | [Prefab] Ships | 1 | Sheltered |  |
| `pref_snow` | [Prefab] Forest, Winter | 0 | Authored native only |  |
| `pref_wasteland` | [Prefab] Wasteland | 0 | Authored native only |  |
| `pref_wildlands` | [Prefab] Wildlands | 0 | Authored native only |  |
| `prefab_beachside` | [Prefab] Beachside | 4 | Authored native only |  |
| `prefab_desoutp` | [Prefab] Desert Outpost | 0 | Authored native only |  |
| `prefab_hauntcave` | [Prefab] Haunted Cave | 3 | Sheltered |  |
| `prefab_space` | [Prefab] Space | 1 | Sheltered |  |
| `prefab_space003` | [Prefab] Space 3 | 1 | Sheltered |  |
| `prefab_space004` | [Prefab] The Void | 1 | Sheltered |  |
| `prefab_space2` | [Prefab] Space 2 | 1 | Sheltered |  |
| `prefab_sunkenlab` | [Prefab] Sunken Lab | 1 | Sheltered |  |
| `prefab_underwtun` | [Prefab] Underwater Tunnel | 1 | Sheltered | Shelter flags corrected |
| `prefabcorvette` | [Prefab] Corvette | 1 | Sheltered |  |
| `prefabdarkbase` | [Prefab] Dark Side Area | 3 | Sheltered |  |
| `prefabfighter` | [Prefab] Fighter | 1 | Sheltered |  |
| `prefabgriddesert` | [Prefab Grid] Desert | 0 | Authored native only |  |
| `prefabgridgrass` | [Prefab Grid] Grasslands | 4 | Authored native only |  |
| `prefabgridsnowy` | [Prefab Grid] Snowy City | 0 | Authored native only |  |
| `prefabgridwaste` | [Prefab Grid] Wastelands | 1 | Sheltered |  |
| `prefabgridwater` | [Prefab Grid] Water/Ocean | 4 | Authored native only |  |
| `prefabwarehouse` | [Prefab] Warehouse | 1 | Sheltered | Shelter flags corrected |
| `pw_ar_abovcity` | [Prefab] Above the City | 0 | Authored native only |  |
| `pw_ar_bhbar` | Smuggler's Moon - The Tilted Visor | 1 | Sheltered |  |
| `pw_ar_citbat003` | [Prefab] City, Warfare | 0 | Authored native only |  |
| `pw_ar_czarmrange` | Smuggler's Moon - Czerka Weapons Testing Facility | 1 | Sheltered | Shelter flags corrected |
| `pw_ar_czoffice` | Smuggler's Moon - Czerka Shipyard Office | 1 | Sheltered |  |
| `pw_ar_gentemp` | [Prefab] Temple, Generic | 1 | Sheltered |  |
| `pw_ar_indzon_vka` | [Prefab] City, Industrial Sector | 0 | Authored native only |  |
| `pw_ar_ka_arkania` | [Prefab] Arkania  | 0 | Authored native only |  |
| `pw_ar_kashyk` | Kashyyyk - Wookie Village, Canopy | 0 | Scripted: Kashyyyk | Explicit climate override |
| `pw_ar_narcatwalk` | Smuggler's Moon - Catwalks | 0 | Scripted: SmugglersMoon |  |
| `pw_ar_nardocks` | Smuggler's Moon - Shipping District | 0 | Scripted: SmugglersMoon |  |
| `pw_ar_narpromena` | Smuggler's Moon - Promenade | 0 | Scripted: SmugglersMoon |  |
| `pw_ar_nars_canhd` | Smuggler's Moon - Hyper Dive Cantina | 1 | Sheltered |  |
| `pw_ar_narscorpd` | Smuggler's Moon - Corporate District | 0 | Scripted: SmugglersMoon |  |
| `pw_ar_narshahub` | Smuggler's Moon - The Hub | 0 | Scripted: SmugglersMoon |  |
| `pw_ar_narslum` | Smuggler's Moon - The Slums | 0 | Scripted: SmugglersMoon |  |
| `pw_ar_ns_doffice` | Smuggler's Moon - Shipping District, Foreman's Office | 1 | Sheltered |  |
| `pw_ar_ns_medical` | Smuggler's Moon - Medshed | 1 | Sheltered |  |
| `pw_ar_nscasino` | Smuggler's Moon - Casino | 1 | Sheltered |  |
| `pw_ar_nscrafting` | Smuggler's Moon - Fabrication Facility | 1 | Sheltered |  |
| `pw_ar_nsczgnstr` | Smuggler's Moon - Czerka Arms, Store | 1 | Sheltered |  |
| `pw_ar_nsficlub` | Smuggler's Moon - Fight Club | 1 | Sheltered |  |
| `pw_ar_nsgsidunge` | Smuggler's Moon - GSI Base | 1 | Sheltered |  |
| `pw_ar_nsshipyard` | Smuggler's Moon - Landing Pads | 0 | Scripted: SmugglersMoon |  |
| `pw_ar_oceancity` | [Prefab] Ocean Planet City | 0 | Authored native only |  |
| `pw_ar_prfbcitysc` | [Prefab] Cityscape,  Vertical | 0 | Authored native only |  |
| `pw_ar_sc_arkcave` | [Prefab] Arkania - Caves | 6 | Sheltered |  |
| `pw_ar_sc_arki001` | [Prefab] Arkania - Interior | 1 | Sheltered | Shelter flags corrected |
| `pw_ar_sc_arkmine` | [Prefab] Arkania - Crystal Mines | 0 | Authored native only |  |
| `pw_ar_trench3bl` | [Prefab] Trenches, Front Line | 0 | Authored native only |  |
| `pw_ar_trenchv2` | [Prefab] Trenches, Back Line | 0 | Authored native only |  |
| `pw_ar_undrnasha` | Smuggler's Moon - Sewers | 1 | Sheltered |  |
| `pw_ar_undrwat` | [Prefab] Underwater | 7 | Sheltered |  |
| `pw_ar_velrevam` | [Prefab] City Planet | 0 | Authored native only |  |
| `pw_ar_velundr` | [Prefab] Undercity  | 3 | Sheltered |  |
| `pw_sc_aboveland` | [Prefab] Above the Land | 0 | Authored native only |  |
| `pw_sc_abovewater` | [Prefab] Above the Water | 0 | Authored native only |  |
| `pw_sc_canyonpit` | Tatooine - Canyon Dueling Pit | 4 | Scripted: Tatooine | Reviewed open-air layout |
| `pw_sc_dantmedsub` | Dantooine - Medical Sublevel | 7 | Sheltered |  |
| `pw_sc_dantprowar` | Dantooine - Protected Ward | 3 | Sheltered |  |
| `pw_sc_dath_apexd` | Dathomir - Grotto Apex Den | 7 | Sheltered |  |
| `pw_sc_dath_sden` | Dathomir - Sealed Apex Den | 7 | Sheltered |  |
| `pw_sc_emfbackr` | Smuggler's Moon - Fight Club Backrooms
 | 3 | Sheltered |  |
| `pw_sc_jeditrial` | Dantooine - Saber Trial Chamber | 1 | Sheltered |  |
| `pw_sc_korrforge` | Korriban - Champion Forge | 7 | Sheltered |  |
| `pw_sc_qioncore` | Hutlar - Overload Chamber | 1 | Sheltered |  |
| `pw_sc_repubcmd` | Viscara - Engineering Command Room | 3 | Sheltered |  |
| `pw_sc_sithritual` | Korriban - Final Ritual Chamber | 3 | Sheltered |  |
| `pw_sc_smarena` | Smuggler's Moon - Private Pit | 3 | Sheltered |  |
| `pw_sc_tarnalpha` | Dathomir - Alpha Beast Hollow | 4 | Scripted: Dathomir |  |
| `pw_sc_velescmd` | Viscara - Militia Command Room | 1 | Sheltered |  |
| `pw_sc_velsewboss` | Viscara - Veles - Sewers - The Cistern Ring | 3 | Sheltered |  |
| `r_prax_centralsp` | Twilight Praxeum - Central Spire | 3 | Sheltered |  |
| `r_prax_combatdec` | Twilight Praxeum - Combat Deck | 1 | Sheltered |  |
| `r_prax_hangar` | Twilight Praxeum - Hangar and Cantina | 1 | Sheltered |  |
| `r_prax_interiors` | Twilight Praxeum - Interiors | 1 | Sheltered |  |
| `r_prax_sith` | Twilight Praxeum - Sith Chambers | 1 | Sheltered |  |
| `randoncity_01` | [Prefab] Randon - City | 4 | Authored native only |  |
| `randoncity_02` | [Prefab] Randon - City Ruins | 0 | Authored native only |  |
| `republicshipevnt` | [Prefab] Republic Cruiser - The Sovereign - Lower Decks | 1 | Sheltered | Shelter flags corrected |
| `roch_govbuild` | [Prefab] Roche System - Nickel One - Verpine Starships Enterprise | 1 | Sheltered |  |
| `rochenickelone` | [Prefab] Roche System - Nickel One | 1 | Sheltered |  |
| `rochenickeltwo` | [Prefab] Roche System - Nickel One Under Assault! | 1 | Sheltered |  |
| `scor_knwinterior` | Korriban - Wasteland Interiors | 1 | Sheltered |  |
| `scor_kscaves` | Korriban - Wastelands Tunnels | 7 | Sheltered |  |
| `ship_basi_v` | Starship - Basilisk | 1 | Sheltered |  |
| `ship_condor_z` | Starship - Condor | 1 | Sheltered |  |
| `ship_consul_z` | Starship - Consular | 1 | Sheltered |  |
| `ship_corv_v` | Starship - Corvette | 1 | Sheltered |  |
| `ship_falchion_z` | Starship - Falchion | 1 | Sheltered |  |
| `ship_fight_v` | Starship - Fighter | 1 | Sheltered |  |
| `ship_firstlight` | [Prefab] Ship - The First Light | 3 | Sheltered |  |
| `ship_hound_z` | Starship - Hound | 1 | Sheltered |  |
| `ship_merchant_z` | Starship - Merchant | 1 | Sheltered |  |
| `ship_mule_z` | Starship - Mule | 1 | Sheltered |  |
| `ship_panth_z` | Starship - Panther | 1 | Sheltered |  |
| `ship_saber_z` | Starship - Saber | 1 | Sheltered |  |
| `ship_secchan_int` | Starship - Second Chance | 1 | Sheltered |  |
| `ship_strike_z` | Starship - Striker | 1 | Sheltered |  |
| `ship_throne_z` | Starship - Throne | 1 | Sheltered |  |
| `shuttle` | Starship Shuttle - Interior | 1 | Sheltered |  |
| `smesks_entry` | Tatooine - Smesk's Entryway | 7 | Sheltered |  |
| `smesks_palace` | Tatooine - Mos Esper - Smesk's Palace | 1 | Sheltered | Shelter flags corrected |
| `sol_hutlarqcanyo` | Hutlar - Qion Box Canyon | 4 | Scripted: Hutlar |  |
| `sol_hutlarqfooth` | Hutlar - Qion Foothills | 4 | Scripted: Hutlar |  |
| `sol_mandaloriani` | Hutlar - Qion Box Canyon - Fort Ka'ra | 3 | Sheltered |  |
| `sol_swampsbase` | Viscara - Swamp Base - Interior | 3 | Sheltered |  |
| `sol_swampstemple` | Viscara - Swamp Base - Temple | 3 | Sheltered |  |
| `space_dathomir` | Space - Dathomir Orbit | 1 | Sheltered |  |
| `space_derelict_k` | [Prefab] Derelict Facility | 1 | Sheltered |  |
| `space_korriban` | Space - Korriban Orbit | 1 | Sheltered |  |
| `space_midrim1` | Space - A Nebula within the Mid Rim | 1 | Sheltered |  |
| `space_midrim2` | Space - Approaching the Outer Rim | 1 | Sheltered |  |
| `spacenarshaddung` | Smuggler's Moon Station - Abandoned | 1 | Sheltered | Shelter flags corrected |
| `spacenarshalower` | Smuggler's Moon Station - Lower Level | 3 | Sheltered |  |
| `spending_area` | *Character Rebuild - Spending Area | 1 | Sheltered | Shelter flags corrected |
| `starport` | Building Template - Starport Style 1 | 1 | Sheltered |  |
| `starship1_int` | Starship - Light Freighter 1 | 1 | Sheltered |  |
| `starship2_int` | Starship - Light Escort 1 | 1 | Sheltered |  |
| `starship3_int` | Starship - Light Freighter 1 | 1 | Sheltered |  |
| `starship4_int` | Starship - Heavy Freighter - Interior | 1 | Sheltered |  |
| `tat_anc_aridhill` | Tatooine - Arid Hilly Desert | 4 | Scripted: Tatooine |  |
| `tat_anc_astropor` | Tatooine - Anchorhead - Spaceport | 4 | Scripted: Tatooine |  |
| `tat_anc_cantina` | Tatooine - Anchorhead - Cantina | 3 | Sheltered |  |
| `tat_anc_desroad1` | Tatooine - Desert Road | 4 | Scripted: Tatooine |  |
| `tat_anc_desroad2` | Tatooine - Desert Road | 4 | Scripted: Tatooine |  |
| `tat_anc_droidshp` | Tatooine - Anchorhead - Droid Shop | 1 | Sheltered | Shelter flags corrected |
| `tat_anc_flatlnd1` | Tatooine - Flatlands | 0 | Scripted: Tatooine |  |
| `tat_anc_flatlnd2` | Tatooine - Flatlands | 0 | Scripted: Tatooine |  |
| `tat_anc_gocorpst` | Tatooine - Anchorhead - Go-Corp Station | 1 | Sheltered | Shelter flags corrected |
| `tat_anc_hillydes` | Tatooine - Hilly Desert | 4 | Scripted: Tatooine |  |
| `tat_anc_junix` | Tatooine - Anchorhead - Junix's Joint | 1 | Sheltered | Shelter flags corrected |
| `tat_anc_medical` | Tatooine - Anchorhead - Medical Center | 1 | Sheltered |  |
| `tat_anc_nedunes` | Tatooine - North East Dunes | 4 | Scripted: Tatooine |  |
| `tat_anc_nminecli` | Tatooine - North Mine Cliffs | 4 | Scripted: Tatooine |  |
| `tat_anc_northdis` | Tatooine - Anchorhead - North District | 4 | Scripted: Tatooine |  |
| `tat_anc_northhil` | Tatooine - North Hills | 4 | Scripted: Tatooine |  |
| `tat_anc_nthdunes` | Tatooine - Northern Dunes | 0 | Scripted: Tatooine |  |
| `tat_anc_rckpass1` | Tatooine - Rocky Pass | 4 | Scripted: Tatooine |  |
| `tat_anc_rckpass2` | Tatooine - Rocky Pass | 4 | Scripted: Tatooine |  |
| `tat_anc_rockdess` | Tatooine - Rocky Desert | 4 | Scripted: Tatooine |  |
| `tat_anc_southdis` | Tatooine - Anchorhead - South District | 4 | Scripted: Tatooine |  |
| `tat_anc_southent` | Tatooine - Anchorhead - Southern Entrance | 4 | Scripted: Tatooine |  |
| `tat_anc_southpas` | Tatooine - Southern Pass | 0 | Scripted: Tatooine |  |
| `tat_anc_totoche1` | Tatooine - To Tochee | 4 | Scripted: Tatooine |  |
| `tat_anc_totoche2` | Tatooine - To Tochee | 4 | Scripted: Tatooine |  |
| `tat_anc_totoche3` | Tatooine - To Tochee | 4 | Scripted: Tatooine |  |
| `tat_anc_tuskncmp` | Tatooine - Tusken Camp | 4 | Scripted: Tatooine |  |
| `tat_anc_tuskntnt` | Tatooine - Tusken Raider Tent | 1 | Sheltered |  |
| `tat_anc_verpexba` | Tatooine - Anchorhead - Verpex Bazaar | 3 | Sheltered |  |
| `tat_babysarlacc` | Tatooine - Baby Sarlacc Cave | 7 | Sheltered |  |
| `tat_brokenjawa` | Tatooine - Broken Down Jawa Machine | 4 | Scripted: Tatooine |  |
| `tat_chasmpass` | Tatooine - Chasm Pass | 0 | Scripted: Tatooine |  |
| `tat_elevagiifarm` | Tatooine - Elevagii Farm | 4 | Scripted: Tatooine |  |
| `tat_rancorcave` | Tatooine - Rancor Cave | 7 | Sheltered |  |
| `tat_rockypasslge` | Tatooine - Rocky Pass | 4 | Scripted: Tatooine |  |
| `tat_smeskspalace` | Tatooine - Smesk's Palace | 4 | Scripted: Tatooine |  |
| `tat_tocheemain` | Tatooine - Tochee | 4 | Scripted: Tatooine |  |
| `tat_tomoseisley1` | Tatooine - To Mos Eisley | 4 | Scripted: Tatooine |  |
| `tat_tuskcavebot` | Tatooine - Tusken Raider Cave - Bottom Floor | 7 | Sheltered |  |
| `tat_tuskcavemain` | Tatooine - Tusken Raider Cave - Main Floor | 3 | Sheltered |  |
| `tat_tuskcavetunn` | Tatooine - Tusken Raider Cave - Tunnels | 7 | Sheltered |  |
| `tat_wormden` | Tatooine - The Worm Den | 7 | Sheltered |  |
| `tatooineorbit` | Space - Tatooine Orbit | 1 | Sheltered |  |
| `tochee_cantina` | Tatooine - Anchorhead - Club d'Ash | 3 | Sheltered |  |
| `tosche_cantina_s` | Tatooine - Anchorhead - Smuggler's Den | 3 | Sheltered |  |
| `v_cox_base` | Viscara - Coxxion Base | 1 | Sheltered |  |
| `v_jediforest_cav` | Viscara - Jedi Temple Forest Cave | 7 | Sheltered |  |
| `v_jeditemple_v2` | Viscara - Jedi Temple Exterior | 4 | Scripted: Viscara |  |
| `v_repubbase_1` | Viscara - Republic Base - Entrance | 3 | Sheltered |  |
| `v_repubbase_2` | Viscara - Republic Base - Medical and Brig | 3 | Sheltered |  |
| `v_repubbase_cd` | Viscara - Republic Base - Combat Deck | 1 | Sheltered |  |
| `v_repubbase_ext` | Viscara - Republic Base - Exterior | 0 | Scripted: Viscara |  |
| `v_repubbase_hang` | Viscara - Republic Base - Hangar | 3 | Sheltered |  |
| `v_repubbase_jnrm` | Viscara - Republic Base - Junior Mess | 1 | Sheltered |  |
| `v_repubbase_off` | Viscara - Republic Base - Offices | 1 | Sheltered |  |
| `v_sithlake_int` | Viscara - Sith Lake Outpost | 3 | Sheltered |  |
| `v_swamp_base` | Viscara - Swamplands - Sith Base | 4 | Scripted: Viscara |  |
| `v_swamp_ruins001` | Viscara - Swamplands Ruins | 4 | Scripted: Viscara |  |
| `v_swamp_undergro` | Viscara - Swamplands Underground | 3 | Sheltered |  |
| `valkorrdung1a` | Korriban - Sith Fortress, Approach | 4 | Scripted: Korriban |  |
| `valkorrdung1b` | Korriban - Sith Fortress, Courtyard | 0 | Scripted: Korriban | Reviewed open-air layout |
| `valkorrdung1c` | Korriban - Sith Fortress, Forgeworks | 1 | Sheltered | Shelter flags corrected |
| `valkorrdung1d` | Korriban - Sith Fortress, Lord's Quarters | 3 | Sheltered |  |
| `veles_cantina` | Viscara - Veles - Racin' Jims | 3 | Sheltered |  |
| `veles_cz_tower` | Viscara - Veles - Czerka Tower | 1 | Sheltered |  |
| `veles_exterior` | Viscara - Veles | 0 | Scripted: Viscara |  |
| `veles_genstore` | Viscara - Veles - General Store | 1 | Sheltered |  |
| `veles_holonews` | Viscara - Veles - HNN Office | 1 | Sheltered |  |
| `veles_sewers` | Viscara - Veles - Sewers | 3 | Sheltered |  |
| `veles_sheriff` | Viscara - Veles - Sheriff/Clinic | 1 | Sheltered |  |
| `veles_shops` | Viscara - Veles - Shops | 1 | Sheltered |  |
| `velesinterior` | Viscara - Veles - Starport | 1 | Sheltered |  |
| `velesrestgarden` | Viscara - Rest's Public Gardens | 1 | Sheltered |  |
| `visc_forcevision` | Force Vision - First Rites | 1 | Sheltered |  |
| `visc_sewer_depth` | Viscara - Veles - Sewers Depths | 3 | Sheltered |  |
| `viscara_archive` | [Prefab] Czerka Archives | 3 | Sheltered |  |
| `viscara_cave_2ka` | [Prefab] Overgrown Caves | 7 | Sheltered |  |
| `viscara_forkeast` | Viscara - Crossroads East | 0 | Scripted: Viscara |  |
| `viscara_forkwest` | Viscara - Crossroads West | 0 | Scripted: Viscara |  |
| `viscara_jedigrou` | Viscara - Jedi Forest Grounds | 0 | Scripted: Viscara |  |
| `viscara_lakegrou` | Viscara - Lake Grounds | 0 | Scripted: Viscara |  |
| `viscara_mountasc` | Viscara - Mountain Ascent | 0 | Scripted: Viscara |  |
| `viscara_npcship1` | [Prefab] Ship - The Ivory Harrier | 1 | Sheltered |  |
| `viscara_wwnorth` | Viscara - Wildwoods - North | 0 | Scripted: Viscara |  |
| `viscara_wwruined` | Viscara - Wildwoods - Ruined | 0 | Scripted: Viscara |  |
| `viscaradeepmount` | Viscara - Deep Mountains | 4 | Scripted: Viscara |  |
| `viscaradeepwo001` | Viscara - Deepwoods | 4 | Scripted: Viscara |  |
| `viscaralake` | Viscara - Lake | 4 | Scripted: Viscara |  |
| `viscaranswamp` | Viscara - Eastern Swamplands | 4 | Scripted: Viscara |  |
| `viscaranwswamp` | Viscara - Western Swamplands | 4 | Scripted: Viscara |  |
| `viscaraorbit` | Space - Viscara Orbit | 1 | Sheltered |  |
| `viscarawildlands` | Viscara - Wildlands | 4 | Scripted: Viscara |  |
| `viscarawildwest` | Viscara - Mountain Valley | 4 | Scripted: Viscara |  |
| `viscarawildwoods` | Viscara - Wildwoods | 4 | Scripted: Viscara |  |
| `vlegionbasemain` | Viscara - Legion Base | 1 | Sheltered |  |
| `vlegsecond` | Viscara - Legion Base - Second Floor | 1 | Sheltered |  |
| `vprefsith1` | [Prefab] Sith Crypt | 3 | Sheltered | Shelter flags corrected |
| `vrotranccsitharc` | [Prefab] Ancient Sith Archive | 3 | Sheltered |  |
| `vrotrbescloudext` | [Prefab] Bespin - Cloud City | 0 | Authored native only |  |
| `vrotrbescloudint` | [Prefab] Bespin - Cloud City Interior | 1 | Sheltered |  |
| `vrotrbescomshop` | [Prefab] Bespin - Commerce Guild Shop | 1 | Sheltered |  |
| `vrotrbesgasmine` | [Prefab] Bespin - Tibanna Gas Mine | 3 | Sheltered |  |
| `vrotrbeslanding` | [Prefab] Bespin - Landing Pads | 0 | Authored native only |  |
| `vrotrbesporttown` | [Prefab] Bespin - Port Town | 3 | Sheltered |  |
| `vrotrcapship1` | [Prefab] Capital Ship | 3 | Sheltered |  |
| `vrotrcorelclouds` | [Prefab] Corellia - City in the Clouds | 0 | Authored native only |  |
| `vrotrdantcourt` | [Prefab] Dantooine - Courtyard | 4 | Authored native only |  |
| `vrotrdantcrystal` | [Prefab] Dantooine - Crystal Cave | 7 | Sheltered |  |
| `vrotrdantfarms` | [Prefab] Dantooine - Farmlands | 4 | Authored native only |  |
| `vrotrdantkhoonda` | [Prefab] Dantooine - Khoonda | 4 | Authored native only |  |
| `vrotrdantplains` | [Prefab] Dantooine - Plains | 4 | Authored native only |  |
| `vrotrghostship` | [Prefab] Ghost Ship | 1 | Sheltered |  |
| `vrotrhighoffice` | [Prefab] Senator's Office | 1 | Sheltered | Shelter flags corrected |
| `vrotrkorracadarc` | [Prefab] Korriban - Academy Archives | 3 | Sheltered |  |
| `vrotrkorracadrft` | [Prefab] Korriban - Academy Rooftop | 0 | Authored native only | Reviewed open-air layout |
| `vrotrkorracsacad` | [Prefab] Korriban - Sith Academy | 3 | Sheltered |  |
| `vrotrkorrdnkside` | [Prefab] Korriban - The Drunk Side | 1 | Sheltered | Shelter flags corrected |
| `vrotrkorrdresh` | [Prefab] Korriban - Dreshdae | 4 | Authored native only |  |
| `vrotrkorretpyre` | [Prefab] Korriban - Eternal Pyre Pathway | 4 | Authored native only |  |
| `vrotrkorrforgcav` | [Prefab] Korriban - Forgotten Cave | 7 | Sheltered |  |
| `vrotrkorrmission` | [Prefab] Korriban - Wasteland Ruins | 4 | Scripted: Korriban |  |
| `vrotrkorrvalley` | [Prefab] Korriban - Valley of the Dark Lords | 4 | Authored native only |  |
| `vrotrnabmission` | [Prefab] Dantooine - Fortress | 4 | Authored native only |  |
| `vrotrnsdockbay` | [Prefab] Nar Shaddaa - Docking Bay | 0 | Authored native only |  |
| `vrotrnsinterior` | [Prefab] Nar Shaddaa - Interior | 1 | Sheltered | Shelter flags corrected |
| `vrotrnsroofconf` | [Prefab] Nar Shaddaa - Rooftop Confrontation | 0 | Authored native only |  |
| `vrotrnsrooftops` | [Prefab] Nar Shaddaa - Rooftops | 0 | Authored native only |  |
| `vrotrnsrooftops2` | [Prefab] Nar Shaddaa - Rooftops 2 | 0 | Authored native only |  |
| `vrotrnsslums` | [Prefab] Nar Shaddaa - Slums | 0 | Authored native only |  |
| `vrotrsithlordext` | [Prefab] Sith Lord's Fortress | 0 | Authored native only |  |
| `vrotrsithlordint` | [Prefab] Sith Lord's Quarters | 3 | Sheltered |  |
| `vrotrtatdescamp` | [Prefab] Tatooine - Desert Camp | 4 | Authored native only |  |
| `vrotrviscvokouts` | [Prefab] Viscara - Vokus Outskirts | 4 | Authored native only |  |
| `yavin` | [Prefab] Yavin | 0 | Authored native only |  |
| `ziyhutdung1a` | Hutlar - Qion Hive, Approach | 0 | Scripted: Hutlar |  |
| `ziyhutdung1b` | Hutlar - Qion Hive, Ziggurat | 3 | Sheltered |  |
| `ziyhutdung1c` | Hutlar - Qion Hive, Outer Hive | 3 | Sheltered |  |
| `ziyhutdung1d` | Hutlar - Qion Hive, Broodmother | 3 | Sheltered |  |
| `zomb_abanstatio2` | Abandoned Station - Restricted Level | 1 | Sheltered |  |
| `zomb_abanstatio3` | Abandoned Station - Director's Chambers | 1 | Sheltered |  |
| `zomb_abanstation` | Abandoned Station - Main Level | 1 | Sheltered |  |
