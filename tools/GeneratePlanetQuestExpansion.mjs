import fs from "node:fs";

const root = process.cwd().replace(/\\/g, "/");
const planText = fs.readFileSync(`${root}/design/planet-quest-expansion-plan.md`, "utf8");
const planets = ["Viscara", "Mon Cala", "Tatooine", "Dantooine", "Dathomir", "Hutlar"];
const planetPrefix = {
  Viscara: "vis",
  "Mon Cala": "mon",
  Tatooine: "tat",
  Dantooine: "dan",
  Dathomir: "dat",
  Hutlar: "hut",
};
const areaDisplay = new Map(Object.entries({
  dan_battlemon: "the battle gym grounds",
  dan_centcolony: "the central colony",
  dan_colony: "the colony farms",
  dan_colonyfarms: "the south colony farms",
  dan_colonyspa: "the colony spa",
  dan_crafterbase: "the crafter camp",
  dan_crystalcavez: "the crystal caves",
  dan_crystalflied: "the crystal fields",
  dan_destroyfarm: "the ruined farmlands",
  dan_enclosemount: "the enclosed mountain trail",
  dan_fieldtrail: "the field trail",
  dan_hiddenmount: "the hidden mountain path",
  dan_iriazfarm: "the Iriaz pastures",
  dan_jantacaves: "the Janta caves",
  dan_jedienclave: "the Jedi enclave grounds",
  dan_jedienlibry: "the enclave library",
  dan_jedlibrary: "the Jedi library",
  dan_jungle1: "the forsaken jungle",
  dan_junglemount: "the jungle mountain trail",
  dan_kathden: "the lower Janta caves",
  dan_kinrathcave: "the Kinrath cave",
  dan_lakencave: "the lake caverns",
  dan_medical: "the colony medical ward",
  dan_mountcrycave: "the mountain crystal cave",
  dan_playerland2: "the clear jungle",
  dan_playerlands: "the tranquil plains",
  dan_repgarrison: "the Republic garrison",
  dan_repinside: "the garrison interior",
  dan_repubmed: "the Republic med center",
  dan_smugcaverns: "the smuggler caverns",
  dan_tribefields: "the Dantari fields",
  dan_warehouse: "the abandoned warehouse",
  dan_wildplain: "the wild plains",
  dath_caveruins1: "the cave ruins",
  dath_cz_baseok: "the Czerka base",
  dath_desert: "the red desert",
  dath_grottos: "the rancor grottos",
  dath_hidtunnels: "the hidden tunnels",
  dath_landingpad: "the jungle landing",
  dath_mountains: "the mountain paths",
  dath_mountcaves: "the mountain caves",
  dath_ruin_base: "the ruin base",
  dath_tarnjungles: "the tarnished jungle",
  dath_tranjungl2: "the northern jungle",
  dath_tribevill: "the tribal village",
  dath_waterfallru: "the waterfall ruins",
  dath_west_desert: "the western desert",
  dathgrottocavern: "the grotto caverns",
  hutlar_frozen_wa: "the frozen wastes",
  hutlar_outpost: "Hutlar Outpost",
  hutlar_qion: "the Qion hive",
  hutlar_smuggleba: "the smuggler bay",
  hutlar_testsite: "the cloning test site",
  hutlar_valley: "the Byysk valley",
  hutlar_wastes_ca: "the waste caverns",
  moncala_swamp: "the sunken swamps",
  moncalacifacilit: "the civic facility",
  moncalacorali001: "the outer coral isles",
  moncalacoralisle: "the inner coral isles",
  moncaladaccityex: "Dac City exterior",
  moncaladaccitysu: "Dac City surface",
  moncaladungeon1: "the lower sea caves",
  moncalajungelsu: "the southern jungle",
  moncalawildjungl: "the wild jungle",
  tat_anc_aridhill: "the arid hills",
  tat_anc_astropor: "Anchorhead astroport",
  tat_anc_cantina: "the Anchorhead cantina",
  tat_anc_droidshp: "the Anchorhead droid shop",
  tat_anc_flatlnd1: "the flatlands",
  tat_anc_gocorpst: "Go-Corp Station",
  tat_anc_hillydes: "the hilly desert",
  tat_anc_junix: "Junix's place",
  tat_anc_medical: "Anchorhead medical",
  tat_anc_nminecli: "the north mine cliffs",
  tat_anc_northdis: "the northern district",
  tat_anc_nthdunes: "the northern dunes",
  tat_anc_rckpass1: "Rocky Pass",
  tat_anc_rockdess: "the rocky desert",
  tat_anc_southdis: "the southern district",
  tat_anc_southent: "the southern entrance",
  tat_anc_southpas: "Southern Pass",
  tat_anc_tuskntnt: "the Tusken tents",
  tat_anc_verpexba: "Verpex Bazaar",
  tat_babysarlacc: "the baby sarlacc cave",
  tat_brokenjawa: "the broken Jawa camp",
  tat_chasmpass: "Chasm Pass",
  tat_elevagiifarm: "Elevagii Farm",
  tat_rancorcave: "the rancor cave",
  tat_rockypasslge: "the rocky passage",
  tat_smeskspalace: "Smesk's palace",
  tat_tocheemain: "Tochee Station",
  tat_tomoseisley1: "the road to Mos Eisley",
  tat_tuskcavemain: "the Tusken cave",
  tat_wormden: "the worm den",
  v_cox_base: "the Coxxion base",
  v_repubbase_ext: "the Republic base exterior",
  veles_cantina: "the Veles cantina",
  veles_cz_tower: "the Czerka tower",
  veles_exterior: "Veles Colony",
  veles_genstore: "the Veles general store",
  veles_sheriff: "the Veles sheriff's office",
  veles_shops: "the Veles market",
  velesinterior: "the Veles interior",
  velesrestgarden: "Rest's public gardens",
  viscara_archive: "the Viscara archive",
  viscara_jedigrou: "the Jedi grounds",
  viscara_lakegrou: "the lake grounds",
  viscaradeepmount: "the deep mountains",
  viscaradeepwo001: "the deep woods",
  viscaralake: "Viscara Lake",
  viscaranswamp: "the northern swamp",
  viscarawildwest: "the western wildlands",
  viscarawildwoods: "the wildwoods",
}));
const classFiles = {
  Viscara: "ViscaraQuestDefinition.cs",
  "Mon Cala": "MonCalaQuestDefinition.cs",
  Tatooine: "TatooineQuestDefinition.cs",
  Dantooine: "DantooineQuestDefinition.cs",
  Dathomir: "DathomirQuestDefinition.cs",
  Hutlar: "HutlarQuestDefinition.cs",
};
const questDir = `${root}/SWLOR.Game.Server/Feature/QuestDefinition`;
const pilotIds = new Set([
  "visc_route_ledger",
  "visc_marker_codes",
  "visc_runner_manifest",
  "visc_burrow_survey",
  "visc_field_dressings",
  "visc_cache_cipher",
]);

function parseRows(planet) {
  const start = planText.indexOf(`## ${planet} Batch`);
  const next = planText.indexOf("\n## ", start + 1);
  const section = planText.slice(start, next < 0 ? planText.length : next);
  const rows = [];

  for (const line of section.split(/\r?\n/)) {
    if (!line.startsWith("|")) continue;
    const cells = line.split("|").slice(1, -1).map((cell) => cell.trim());
    if (!/^\d+$/.test(cells[0] ?? "")) continue;
    rows.push({
      planet,
      n: Number(cells[0]),
      id: cells[1],
      name: cells[2],
      npc: cells[3],
      area: cells[4],
      obj: cells[5],
      repeat: cells[6].toLowerCase(),
    });
  }

  return rows;
}

const allRows = planets.flatMap(parseRows);
const generatedRows = allRows.filter((row) => !pilotIds.has(row.id));

const major = new Set([
  "visc_cache_cipher",
  "visc_fleshleader_report",
  "visc_jedi_records",
  "visc_republic_shortfall",
  "mon_leader_beacon",
  "mon_cave_rescue",
  "mon_echo_survey",
  "mon_hunter_jaws",
  "mon_coralisle_beacons",
  "tat_worm_vibrations",
  "tat_rancor_spoor",
  "tat_tusken_elite_orders",
  "tat_ancient_worm_tooth",
  "tat_moseisley_signals",
  "dan_queen_tracks",
  "dan_deserter_notes",
  "dan_smuggler_manifest",
  "dan_dantari_rites",
  "dan_mountain_crystals",
  "dath_dark_adept_signs",
  "dath_rancor_spoor",
  "dath_boss_trophies",
  "dath_ruin_base_keys",
  "dath_rancor_bone",
  "hut_broodmother_clutch",
  "hut_chieftain_challenge",
  "hut_champion_scars",
  "hut_black_ledger",
  "hut_clone_logs",
  "hut_broodmother_shell",
]);
const capstone = new Set([
  "visc_signal_mountain",
  "mon_tidewatch_rounds",
  "tat_ancient_husk",
  "dan_colony_circuit",
  "dath_dark_adept_relic",
  "dath_weathered_tablets",
  "hut_outpost_last_shift",
]);
const rewardMatrix = {
  Viscara: {
    minor: [2000, 1125],
    standard: [4000, 2625],
    repeat: [1500, 750],
    major: [6000, 6000],
    capstone: [12000, 11250],
  },
  "Mon Cala": {
    minor: [1000, 750],
    standard: [2000, 1500],
    repeat: [1000, 750],
    major: [4000, 4000],
    capstone: [7500, 7500],
  },
  Tatooine: {
    minor: [1000, 750],
    standard: [1750, 1500],
    repeat: [1750, 750],
    major: [4000, 4500],
    capstone: [7500, 11250],
  },
  Dantooine: {
    minor: [2000, 1500],
    standard: [4000, 3750],
    repeat: [600, 300],
    major: [6000, 7500],
    capstone: [12000, 11250],
  },
  Dathomir: {
    minor: [2500, 2500],
    standard: [5000, 5500],
    repeat: [2000, 2500],
    major: [8000, 7200],
    capstone: [12000, 11250],
  },
  Hutlar: {
    minor: [800, 825],
    standard: [1300, 1800],
    repeat: [800, 900],
    major: [5000, 6000],
    capstone: [15000, 22500],
  },
};

const uniqueRewards = new Map([
  [
    "visc_signal_mountain",
    {
      resref: "visc_sig_core",
      name: "Veles Signal Core",
      desc: "A rugged signal core recovered from Viscara mountain relay work. Its casing still carries Veles calibration marks and frontier repairs.",
      value: 22000,
    },
  ],
  [
    "visc_cache_cipher",
    {
      resref: "visc_kara_sig",
      name: "Ka'ra Cache Signet",
      desc: "A blackened field signet recovered from a Mandalorian cache on Viscara.",
      value: 12000,
    },
  ],
  [
    "visc_jedi_records",
    {
      resref: "visc_jedi_dat",
      name: "Veles Jedi Datacron",
      desc: "A recovered Viscara datacron containing damaged Jedi records and field annotations.",
      value: 22000,
    },
  ],
  [
    "mon_tidewatch_rounds",
    {
      resref: "mon_tide_lens",
      name: "Tidewatch Lens",
      desc: "A precision lens calibrated against Mon Cala current shifts and low-light survey readings.",
      value: 18000,
    },
  ],
  [
    "mon_leader_beacon",
    {
      resref: "mon_beac_core",
      name: "Dac Beacon Core",
      desc: "The recovered core from an eco-terrorist beacon, scrubbed and sealed for civic analysis.",
      value: 10000,
    },
  ],
  [
    "mon_hunter_jaws",
    {
      resref: "mon_jaw_charm",
      name: "Hunter's Jaw Charm",
      desc: "A Mon Cala predator trophy charm prepared from the most dangerous jungle work.",
      value: 10000,
    },
  ],
  [
    "tat_ancient_husk",
    {
      resref: "tat_husk_core",
      name: "Ancient Husk Core",
      desc: "A dense relic core recovered from ancient sand worm remains.",
      value: 18000,
    },
  ],
  [
    "tat_rancor_spoor",
    {
      resref: "tat_rancor_sp",
      name: "Rancor Tracker's Spur",
      desc: "A distinctive tracker keepsake from a rare Tatooine rancor spoor run.",
      value: 9000,
    },
  ],
  [
    "tat_tusken_elite_orders",
    {
      resref: "tat_tusk_blade",
      name: "Tusken Elite Blade",
      desc: "A named blade recovered from elite Tusken command work near Anchorhead.",
      value: 10000,
    },
  ],
  [
    "dan_colony_circuit",
    {
      resref: "dan_col_datapad",
      name: "Colony Circuit Datapad",
      desc: "A complete Dantooine colony utility archive with circuit notes, survey marks, and repair annotations.",
      value: 22000,
    },
  ],
  [
    "dan_queen_tracks",
    {
      resref: "dan_queen_chit",
      name: "Kinrath Queen Chitin",
      desc: "A durable chitin plate marked during the Kinrath Queen tracking work.",
      value: 12000,
    },
  ],
  [
    "dan_mountain_crystals",
    {
      resref: "dan_mtn_focus",
      name: "Dantooine Crystal Focus",
      desc: "A mountain crystal focus recovered from Dantooine highland survey work.",
      value: 12000,
    },
  ],
  [
    "dath_dark_adept_relic",
    {
      resref: "dath_adept_rel",
      name: "Dark Adept Relic",
      desc: "A Force-darkened relic recovered from Dathomir adept activity.",
      value: 28000,
    },
  ],
  [
    "dath_weathered_tablets",
    {
      resref: "dath_tab_frag",
      name: "Weathered Tablet Fragment",
      desc: "A preserved fragment from Dathomir weathered tablets, etched with old local markings.",
      value: 26000,
    },
  ],
  [
    "dath_boss_trophies",
    {
      resref: "dath_boss_fang",
      name: "Witchlands Trophy Fang",
      desc: "A named trophy fang from high-danger Dathomir grotto work.",
      value: 15000,
    },
  ],
  [
    "hut_outpost_last_shift",
    {
      resref: "hut_last_badge",
      name: "Last Shift Badge",
      desc: "A commemorative Hutlar outpost badge from the final perimeter shift.",
      value: 28000,
    },
  ],
  [
    "hut_champion_scars",
    {
      resref: "hut_champ_mark",
      name: "Champion's Scar Marker",
      desc: "A carved trophy marker recording combat scars from a Byysk champion encounter.",
      value: 12000,
    },
  ],
  [
    "hut_clone_logs",
    {
      resref: "hut_clone_chip",
      name: "Clone Log Cipher",
      desc: "A cipher chip containing indexed Hutlar clone experiment logs.",
      value: 12000,
    },
  ],
  [
    "hut_broodmother_shell",
    {
      resref: "hut_broodplate",
      name: "Broodmother Carapace Plate",
      desc: "A unique carapace plate recovered from Qion broodmother work.",
      value: 14000,
    },
  ],
]);

function cs(text) {
  return text.replace(/\\/g, "\\\\").replace(/"/g, '\\"');
}

function pascal(id) {
  return id
    .split("_")
    .map((part) => (part ? part[0].toUpperCase() + part.slice(1) : ""))
    .join("")
    .replace(/[^A-Za-z0-9]/g, "");
}

const planetMethodPrefix = {
  Viscara: "Viscara",
  "Mon Cala": "MonCala",
  Tatooine: "Tatooine",
  Dantooine: "Dantooine",
  Dathomir: "Dathomir",
  Hutlar: "Hutlar",
};
const questIdPrefix = {
  Viscara: "visc",
  "Mon Cala": "mon",
  Tatooine: "tat",
  Dantooine: "dan",
  Dathomir: "dath",
  Hutlar: "hut",
};
const questItemPlanetSlug = {
  Viscara: "viscara",
  "Mon Cala": "moncala",
  Tatooine: "tatooine",
  Dantooine: "dantooine",
  Dathomir: "dathomir",
  Hutlar: "hutlar",
};

function methodName(row) {
  const prefix = questIdPrefix[row.planet];
  const idWithoutPlanetPrefix = row.id.startsWith(`${prefix}_`)
    ? row.id.slice(prefix.length + 1)
    : row.id;

  return `${planetMethodPrefix[row.planet]}${pascal(idWithoutPlanetPrefix)}`;
}

function methodNameVariants(row) {
  return [
    pascal(row.id),
    `${planetMethodPrefix[row.planet]}${pascal(row.id)}`,
    methodName(row),
  ].filter((name, index, names) => names.indexOf(name) === index);
}

function areaName(area) {
  return areaDisplay.get(area) ?? area.replace(/_/g, " ");
}

function stripPeriod(text) {
  return text.replace(/[.\s]+$/, "");
}

function lowerFirst(text) {
  return text.charAt(0).toLowerCase() + text.slice(1);
}

function titleCase(text) {
  return text
    .split(/\s+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function sentence(text) {
  const trimmed = stripPeriod(text);
  return trimmed.charAt(0).toUpperCase() + trimmed.slice(1);
}

function objectiveText(row) {
  return lowerFirst(stripPeriod(row.obj))
    .replace(/^kill\b/i, "deal with")
    .replace(/^defeat\b/i, "defeat")
    .replace(/^collect\b/i, "gather")
    .replace(/^recover\b/i, "recover")
    .replace(/^repair\b/i, "repair")
    .replace(/^place\b/i, "place")
    .replace(/^scan\b/i, "survey")
    .replace(/\bin lower\b/gi, "in the lower")
    .replace(/\bin northern\b/gi, "in the northern")
    .replace(/\bin southern\b/gi, "in the southern")
    .replace(/\bin western\b/gi, "in the western")
    .replace(/\bin eastern\b/gi, "in the eastern")
    .replace(/\bin rocky\b/gi, "in the rocky")
    .replace(/\bin hilly\b/gi, "in the hilly")
    .replace(/\bin arid\b/gi, "in the arid")
    .replace(/\bin jungle\b/gi, "in the jungle");
}

function objectiveSentence(row) {
  return sentence(objectiveText(row));
}

function firstJournal(row) {
  const objective = objectiveText(row);
  const normalizedObjective = objective.toLowerCase().replace(/[^a-z0-9]/g, "");
  const normalizedArea = areaName(row.area).toLowerCase().replace(/^the\s+/, "").replace(/[^a-z0-9]/g, "");
  const placeHint = normalizedArea && normalizedObjective.includes(normalizedArea)
    ? ""
    : ` The trail points toward ${areaName(row.area)}.`;

  return `${row.npc} asked you to ${objective}.${placeHint} Return to ${row.npc} when it is done.`;
}

function returnJournal(row) {
  return `Return to ${row.npc} for your reward.`;
}

function isMinor(row) {
  const objective = row.obj.toLowerCase();
  return (
    /deliver|inspect|repair|activate|calibrate|place|use|scan|tune|restore|replace|visit|patrol|map|mark|align|deploy|plant|set|catalog|copy|open|locate|speak|complete|ask|take|bring/.test(
      objective,
    ) && !/kill|defeat|collect|clear|cull/.test(objective)
  );
}

function rewards(row) {
  const matrix = rewardMatrix[row.planet];
  if (row.repeat === "yes") return matrix.repeat;
  if (capstone.has(row.id)) return matrix.capstone;
  if (major.has(row.id)) return matrix.major;
  if (isMinor(row)) return matrix.minor;
  return matrix.standard;
}

function amountForKill(row, group) {
  const objective = row.obj.toLowerCase();
  if (/leader|queen|broodmother|chieftain|champion|dark adept|rancor|ancient|worm husk/.test(objective)) {
    return 1;
  }
  if (/long patrol/.test(objective)) return group.includes("Byysk") ? 10 : 5;
  return row.repeat === "yes" ? 8 : 6;
}

function amountForCollect(row, resref) {
  const objective = row.obj.toLowerCase();
  if (row.generatedCollectItem?.resref === resref) return 1;
  if (/ancient|leader|queen|broodmother|chieftain|champion|rancor|relic|tooth/.test(objective)) {
    return 1;
  }
  if (resref === "kolto_injection") return 10;
  return row.repeat === "yes" ? 5 : 3;
}

function shouldGenerateQuestItem(row) {
  if (row.collects.length) return false;
  const objective = row.obj.toLowerCase();
  return /\b(collect|gather|recover|retrieve|obtain)\b/.test(objective);
}

function generatedQuestItemName(row) {
  let name = objectiveText(row)
    .replace(/\b(?:collect|gather|recover|retrieve|obtain)\b/gi, "")
    .replace(/\b(?:deal with|defeat|clear|cull|open|repair|activate|place|use|survey|scan)\b/gi, "")
    .replace(/\b(?:and|then)\b/gi, " ")
    .replace(/\b(?:from|around|near|in|inside|at|for|to|after|before|across|through|toward)\b[\s\S]*$/i, "")
    .replace(/[^A-Za-z0-9' -]/g, " ")
    .replace(/\s+/g, " ")
    .trim();

  if (name.length < 4 || name.split(/\s+/).length < 2) {
    name = row.name;
  }

  return titleCase(name);
}

function classify(row) {
  const objective = row.obj.toLowerCase();
  const kills = [];
  const collects = [];
  const add = (group) => {
    if (!kills.includes(group)) kills.push(group);
  };
  const collect = (resref) => {
    if (!collects.includes(resref)) collects.push(resref);
  };

  if (row.planet === "Viscara") {
    if (/outlaw/.test(objective)) add("Viscara_WildwoodsOutlaws");
    if (/fleshleader/.test(objective)) add("Viscara_VellenFleshleader");
    else if (/flesheater/.test(objective)) add("Viscara_VellenFlesheater");
    if (/raivor/.test(objective)) add("Viscara_DeepMountainRaivors");
    if (/nashtah/.test(objective)) add("Viscara_ValleyNashtah");
    if (/crystal spider/.test(objective)) add("Viscara_CrystalSpider");
    if (/ranger tags/.test(objective)) collect("man_tags");
  }

  if (row.planet === "Mon Cala" && /viper/.test(objective)) {
    add("MonCala_Viper");
    if (/venom|sac/.test(objective)) collect("viper_bile");
  }
  if (row.planet === "Mon Cala" && /aradile/.test(objective)) {
    add("MonCala_Aradile");
    if (/shell|chip/.test(objective)) collect("aradile_tail");
  }
  if (row.planet === "Mon Cala" && /amphi|hydrus/.test(objective)) {
    add("MonCala_AmphiHydrus");
    if (/sample|tissue/.test(objective)) collect("amphi_blood");
  }
  if (row.planet === "Mon Cala" && /eco-terrorist|eco terrorist/.test(objective)) add("MonCala_EcoTerrorist");
  if (row.planet === "Mon Cala" && /octotench/.test(objective)) {
    add("MonCala_Octotench");
    if (/ink|nest/.test(objective)) collect("mtench_ink");
  }
  if (row.planet === "Mon Cala" && /microtench/.test(objective)) add("MonCala_Microtench");
  if (row.planet === "Mon Cala" && /scorchellus/.test(objective)) {
    add("MonCala_Scorchellus");
    if (/tissue|chitin|burn/.test(objective)) collect("scorch_chitin");
  }

  if (row.planet === "Tatooine" && /womprat/.test(objective)) {
    add("Tatooine_Womprat");
    if (/hide/.test(objective)) collect("womprathide");
  }
  if (row.planet === "Tatooine" && /sandswimmer/.test(objective)) add("Tatooine_Sandswimmer");
  if (row.planet === "Tatooine" && /sand beetle|beetle/.test(objective)) add("Tatooine_SandBeetle");
  if (row.planet === "Tatooine" && /sand demon/.test(objective)) {
    add("Tatooine_SandDemon");
    if (/mark/.test(objective)) collect("sand_demon_leg");
  }
  if (row.planet === "Tatooine" && /tusken elite/.test(objective)) add("Tatooine_TuskenElite");
  else if (row.planet === "Tatooine" && /tusken/.test(objective)) add("Tatooine_TuskenRaider");
  if (row.planet === "Tatooine" && /ancient sand worm|ancient worm/.test(objective)) add("Tatooine_AncientSandWorm");
  else if (row.planet === "Tatooine" && /sand worm|worm/.test(objective)) {
    add("Tatooine_SandWorm");
    if (/tooth|casting/.test(objective)) collect("sandwormtooth");
  }

  if (row.planet === "Dantooine") {
    if (/plains thune|thune/.test(objective)) add("Dantooine_PlainsThune");
    if (/gizka/.test(objective)) add("Dantooine_Gizka");
    if (/voritor/.test(objective)) add("Dantooine_VoritorLizard");
    if (/kinrath queen/.test(objective)) add("Dantooine_KinrathQueen");
    else if (/kinrath/.test(objective)) add("Dantooine_Kinrath");
    if (/\bbols?\b/.test(objective)) add("Dantooine_Bol");
    if (/iriaz/.test(objective)) add("Dantooine_Iriaz");
    if (/dantari hunter|dantari forces/.test(objective)) add("Dantooine_DantariHunter");
    if (/dantari shaman/.test(objective)) add("Dantooine_DantariShaman");
    if (/starwort|herbs/.test(objective)) collect("dant_starwort");
    if (/hay bale|hay/.test(objective)) collect("haybundle");
    if (/medi supplies|triage/.test(objective)) collect("kolto_injection");
  }

  if (row.planet === "Dathomir") {
    if (/swampland bug/.test(objective)) add("Dathomir_SwamplandBug");
    if (/shear mite|mite/.test(objective)) add("Dathomir_ShearMite");
    if (/kwi guardian|guardian/.test(objective)) add("Dathomir_KwiGuardian");
    if (/kwi shaman|shaman/.test(objective)) add("Dathomir_KwiShaman");
    if (/kwi tribal|kwi patrol|totem|tribal/.test(objective)) add("Dathomir_KwiTribal");
    if (/purbole/.test(objective)) add("Dathomir_Purbole");
    if (/dragon turtle|turtle/.test(objective)) add("Dathomir_DragonTurtle");
    if (/ssurian/.test(objective)) add("Dathomir_Ssurian");
    if (/squellbug/.test(objective)) add("Dathomir_Squellbug");
    if (/sprantal/.test(objective)) add("Dathomir_Sprantal");
    if (/chirodactyl/.test(objective)) add("Dathomir_Chirodactyl");
    if (/dark adept/.test(objective)) add("Dathomir_DarkAdept");
    if (/rancor/.test(objective)) add("Dathomir_Rancor");
    if (/spider/.test(objective)) add("Dathomir_GapingSpider");
    if (/sardine/.test(objective)) collect("dath_sardine");
    if (/spider egg|web/.test(objective)) collect("spider_guts");
    if (/squellbug chitin/.test(objective)) collect("wild_leg");
    if (/shaman ashes/.test(objective)) collect("wild_blood");
  }

  if (row.planet === "Hutlar" && /byysk shaman/.test(objective)) add("Hutlar_ByyskShaman");
  else if (row.planet === "Hutlar" && /byysk chieftain/.test(objective)) add("Hutlar_ByyskChieftain");
  else if (row.planet === "Hutlar" && /byysk champion/.test(objective)) add("Hutlar_ByyskChampion");
  else if (row.planet === "Hutlar" && /byysk guardian/.test(objective)) add("Byysk_Guardian");
  else if (row.planet === "Hutlar" && /byysk/.test(objective)) add("Hutlar_Byysk");
  if (row.planet === "Hutlar" && /qion hive tunneler|tunneler/.test(objective)) add("Hutlar_QionHiveTunneler");
  if (row.planet === "Hutlar" && /broodmother/.test(objective)) add("Hutlar_QionBroodmother");
  if (row.planet === "Hutlar" && /qion slug|slug/.test(objective)) {
    add("Hutlar_QionSlugs");
    if (/bile/.test(objective)) collect("slug_bile");
  }
  if (row.planet === "Hutlar" && /qion tiger|tiger/.test(objective)) {
    add("Hutlar_QionTigers");
    if (/pelt/.test(objective)) collect("qion_tiger_fang");
  }

  return {
    kills: kills.map((group) => ({ group, amount: amountForKill(row, group) })),
    collects: collects.map((resref) => ({ resref, amount: amountForCollect(row, resref) })),
  };
}

const prereqById = new Map();
const lastByPlanetNpc = new Map();
for (const row of allRows) {
  const key = `${row.planet}|${row.npc}`;
  if (row.repeat !== "yes" && lastByPlanetNpc.has(key)) {
    prereqById.set(row.id, lastByPlanetNpc.get(key).id);
  }
  if (row.repeat !== "yes") lastByPlanetNpc.set(key, row);
}

const questItemCounters = new Map();
for (const row of generatedRows) {
  const classification = classify(row);
  row.kills = classification.kills;
  row.collects = classification.collects;
  if (shouldGenerateQuestItem(row)) {
    const planetSlug = questItemPlanetSlug[row.planet];
    const next = (questItemCounters.get(row.planet) ?? 0) + 1;
    questItemCounters.set(row.planet, next);
    row.generatedCollectItem = {
      resref: `qi_${planetSlug}_${String(next).padStart(3, "0")}`,
      name: generatedQuestItemName(row),
    };
    row.collects.push({
      resref: row.generatedCollectItem.resref,
      amount: 1,
    });
  }
  [row.xp, row.gold] = rewards(row);
  row.prereq = prereqById.get(row.id);
}

let objectiveIndex = 1;
for (const row of generatedRows) {
  if (questKind(row) === "state" || row.generatedCollectItem) {
    row.objectiveResref = `qo_${planetPrefix[row.planet]}${String(objectiveIndex).padStart(3, "0")}`;
    objectiveIndex++;
  }
}

function methodFor(row, fieldBuilder) {
  const builder = fieldBuilder ? "_builder" : "builder";
  const lines = [];
  if (fieldBuilder) lines.push(`        private void ${methodName(row)}()`);
  else lines.push(`        private static void ${methodName(row)}(QuestBuilder builder)`);
  lines.push("        {");
  lines.push(`            ${builder}.Create("${cs(row.id)}", "${cs(row.name)}")`);
  if (row.repeat === "yes") lines.push("                .IsRepeatable()");
  if (row.prereq) lines.push(`                .PrerequisiteQuest("${cs(row.prereq)}")`);
  lines.push("");
  lines.push("                .AddState()");
  lines.push(`                .SetStateJournalText("${cs(firstJournal(row))}")`);
  for (const kill of row.kills) {
    lines.push(`                .AddKillObjective(NPCGroupType.${kill.group}, ${kill.amount})`);
  }
  for (const collect of row.collects) {
    lines.push(`                .AddCollectItemObjective("${cs(collect.resref)}", ${collect.amount})`);
  }
  lines.push("");
  lines.push("                .AddState()");
  lines.push(`                .SetStateJournalText("${cs(returnJournal(row))}")`);
  lines.push("");
  lines.push(`                .AddGoldReward(${row.gold})`);
  lines.push(`                .AddXPReward(${row.xp})`);
  const reward = uniqueRewards.get(row.id);
  if (reward) lines.push(`                .AddItemReward("${cs(reward.resref)}", 1)`);
  lines[lines.length - 1] += ";";
  lines.push("        }");
  return lines.join("\n");
}

function addUsing(source, usingLine) {
  if (source.includes(usingLine)) return source;
  const lines = source.split(/\r?\n/);
  let index = 0;
  while (index < lines.length && lines[index].startsWith("using ")) index++;
  lines.splice(index, 0, usingLine);
  return lines.join("\n");
}

function normalizeEol(source) {
  return source.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
}

function toCrlf(source) {
  return normalizeEol(source)
    .replace(/[ \t]+$/gm, "")
    .replace(/\n*$/, "\n")
    .replace(/\n/g, "\r\n");
}

function shouldKeepBlankLine(previous, next) {
  if (!previous || !next) return false;
  if (previous.startsWith("using ") && next.startsWith("using ")) return false;
  if (previous.startsWith("using ") && next.startsWith("namespace ")) return true;
  if (next === "{" || previous === "{") return false;
  if (next === "}") return false;
  if (next.startsWith(".")) {
    if (next.startsWith(".AddState()") && !previous.includes(".Create(") && !previous.startsWith(".IsRepeatable()") && !previous.startsWith(".Prerequisite")) return true;
    if (/^\.Add(?:Gold|XP|Item|KeyItem|Faction|GP)/.test(next)) {
      return !/^\.Add(?:Gold|XP|Item|KeyItem|Faction|GP)/.test(previous);
    }
    if (next.startsWith(".On")) return true;
    return false;
  }
  if (previous === "}" && /^(private|public|#region|#endregion)/.test(next)) return true;
  if (/^(private|public|protected|internal)\b/.test(next) && previous.endsWith(";")) return true;
  if (previous.startsWith("#endregion") && next === "}") return true;
  return false;
}

function compactCSharpWhitespace(source) {
  const lines = normalizeEol(source).split("\n").map((line) => line.replace(/[ \t]+$/, ""));
  const compacted = [];
  let sawBlank = false;

  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) {
      sawBlank = true;
      continue;
    }

    if (sawBlank && compacted.length) {
      const previous = compacted[compacted.length - 1].trim();
      if (shouldKeepBlankLine(previous, trimmed)) {
        compacted.push("");
      }
    }

    compacted.push(line);
    sawBlank = false;
  }

  return compacted.join("\n");
}

function escapeRegex(text) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function insertBeforeClassEnd(source, block) {
  const marker = /(\r?\n\s*}\s*\r?\n\s*}\s*)$/;
  const match = source.match(marker);
  if (!match) throw new Error("Could not find class end");
  const body = source.slice(0, match.index).replace(/\s*$/, "");
  const cleanBlock = block.replace(/^\n+|\n+$/g, "");
  return `${body}\n\n${cleanBlock}\n    }\n}`;
}

function removeGeneratedMethods(source, rows, fieldBuilder) {
  for (const row of rows) {
    for (const name of methodNameVariants(row)) {
      const signature = fieldBuilder
        ? `private\\s+void\\s+${escapeRegex(name)}\\s*\\(\\s*\\)`
        : `private\\s+static\\s+void\\s+${escapeRegex(name)}\\s*\\(\\s*QuestBuilder\\s+builder\\s*\\)`;
      source = source.replace(
        new RegExp(`\\n\\s*${signature}\\s*\\n\\s*\\{[\\s\\S]*?\\n\\s*\\}`, "g"),
        "\n",
      );
    }
  }
  return source;
}

function insertBuildCalls(source, planet, rows) {
  const fieldBuilder = planet !== "Tatooine";

  for (const row of rows) {
    for (const name of methodNameVariants(row)) {
      const call = fieldBuilder ? `${name}();` : `${name}(builder);`;
      source = source.replace(new RegExp(`\\n\\s*${escapeRegex(call)}`, "g"), "");
    }
  }

  const returnLine = fieldBuilder ? "return _builder.Build();" : "return builder.Build();";
  const expression = new RegExp(`\\n(\\s*)${escapeRegex(returnLine)}`);
  return source.replace(expression, (_match, indent) => {
    const calls = rows
      .map((row) => `${indent}${fieldBuilder ? `${methodName(row)}();` : `${methodName(row)}(builder);`}`)
      .join("\n");
    return `\n${calls}\n${indent}${returnLine}`;
  });
}

for (const planet of planets.filter((planet) => planet !== "Dathomir")) {
  const rows = generatedRows.filter((row) => row.planet === planet);
  if (!rows.length) continue;
  const file = `${questDir}/${classFiles[planet]}`;
  let source = normalizeEol(fs.readFileSync(file, "utf8"));
  if (rows.some((row) => row.kills.length)) {
    source = addUsing(source, "using SWLOR.Game.Server.Service.NPCService;");
  }
  const fieldBuilder = planet !== "Tatooine";
  source = removeGeneratedMethods(source, rows, fieldBuilder);
  source = insertBuildCalls(source, planet, rows);
  const methods = rows.map((row) => methodFor(row, fieldBuilder)).join("\n\n");
  source = insertBeforeClassEnd(source, `\n${methods}\n`);
  source = compactCSharpWhitespace(source);
  fs.writeFileSync(file, toCrlf(source));
}

const dathRows = generatedRows.filter((row) => row.planet === "Dathomir");
const dathCalls = dathRows.map((row) => `            ${methodName(row)}();`).join("\n");
const dathMethods = dathRows.map((row) => methodFor(row, true)).join("\n\n");
const dathSource = `using System.Collections.Generic;\nusing SWLOR.Game.Server.Service.NPCService;\nusing SWLOR.Game.Server.Service.QuestService;\n\nnamespace SWLOR.Game.Server.Feature.QuestDefinition\n{\n    public class DathomirQuestDefinition : IQuestListDefinition\n    {\n        private readonly QuestBuilder _builder = new();\n\n        public Dictionary<string, QuestDetail> BuildQuests()\n        {\n${dathCalls}\n\n            return _builder.Build();\n        }\n\n${dathMethods}\n    }\n}\n`;
fs.writeFileSync(`${questDir}/DathomirQuestDefinition.cs`, toCrlf(compactCSharpWhitespace(dathSource)));

const value = (type, v) => ({ type, value: v });
const cexostring = (text) => value("cexostring", text);
const resref = (text) => value("resref", text);
const list = (items) => value("list", items);
const loc = (text) => value("cexolocstring", { 0: text });
const param = (key, v) => ({ __struct_id: 0, Key: cexostring(key), Value: cexostring(v) });

function writeJsonFile(file, object) {
  fs.writeFileSync(file, toCrlf(`${JSON.stringify(object, null, 2)}\n`));
}

const link = (index) => ({
  __struct_id: 0,
  Active: resref(""),
  ConditionParams: list([]),
  Index: value("dword", index),
  IsChild: value("byte", 0),
});

function entry(text, replyIndexes = []) {
  return {
    __struct_id: 0,
    ActionParams: list([]),
    Animation: value("dword", 0),
    AnimLoop: value("byte", 1),
    Comment: cexostring(""),
    Delay: value("dword", 4294967295),
    Quest: cexostring(""),
    RepliesList: list(replyIndexes.map(link)),
    Script: resref(""),
    Sound: resref(""),
    Speaker: cexostring(""),
    Text: loc(text),
  };
}

function reply(text, actionKey = "", actionValue = "", childIndexes = []) {
  return {
    __struct_id: 0,
    ActionParams: list(actionKey ? [param(actionKey, actionValue)] : []),
    Animation: value("dword", 0),
    AnimLoop: value("byte", 1),
    Comment: cexostring(""),
    Delay: value("dword", 4294967295),
    EntriesList: list(childIndexes.map(link)),
    Quest: cexostring(""),
    Script: resref(actionKey ? "action" : ""),
    Sound: resref(""),
    Text: loc(text),
  };
}

function start(index, conditions = []) {
  return {
    __struct_id: 0,
    Active: resref(conditions.length ? "condition" : ""),
    ConditionParams: list(conditions.map(([key, v]) => param(key, v))),
    Index: value("dword", index),
  };
}

function planetTone(planet) {
  return {
    Viscara: "Keep it quiet and practical; Veles has enough trouble without turning every errand into a speech.",
    "Mon Cala": "Dac City runs on civic trust, careful surveys, and people doing small jobs before they become emergencies.",
    Tatooine: "Anchorhead survives by confirming the work, paying the debt, and not pretending the desert is merciful.",
    Dantooine: "The colony needs clean records and steady hands more than grand declarations.",
    Dathomir: "On Dathomir, a careful report is often the line between a rumor and a grave marker.",
    Hutlar: "Hutlar work is measured by what still functions after the cold and the Byysk are done with it.",
  }[planet];
}

function questKind(row) {
  if (row.collects.length) return "collect";
  if (row.kills.length) return "kill";
  return "state";
}

function buildDlg(npcName, rows) {
  const entries = [];
  const replies = [];
  const starts = [];
  const addReply = (text, actionKey = "", actionValue = "", childText = "") => {
    const childIndexes = [];
    if (childText) {
      const childIndex = entries.length;
      entries.push(entry(childText, []));
      childIndexes.push(childIndex);
    }
    const index = replies.length;
    replies.push(reply(text, actionKey, actionValue, childIndexes));
    return index;
  };
  const addEntry = (text, replyIndexes, conditions) => {
    const index = entries.length;
    entries.push(entry(text, replyIndexes));
    starts.push(start(index, conditions));
    return index;
  };

  for (const row of rows) {
    const completeReply = addReply(
      "Here is what I found.",
      "action-advance-quest",
      row.id,
      "That is exactly what I needed. I will see that your payment is released.",
    );
    addEntry(`You finished ${row.name}? Good. Tell me what happened and I can pay you.`, [
      completeReply,
      addReply("Not yet."),
    ], [["condition-on-quest-state", `${row.id} 2`]]);

    let activeText = `${row.name}: ${objectiveSentence(row)}.`;
    let actionKey = "action-advance-quest";
    let actionText = "The work is done.";
    let childText = "I will check the report and mark the job forward.";
    if (questKind(row) === "collect") {
      activeText += " Bring the materials back here once you have them.";
      actionKey = "action-request-quest-items";
      actionText = "I have the requested items.";
      childText = "Place the materials here and I will make sure everything is accounted for.";
    } else if (questKind(row) === "kill") {
      activeText += " Bring me word when the danger has passed.";
    } else {
      activeText += ` I marked the place in ${areaName(row.area)}. Return once it is handled.`;
    }
    const activeReplies = [];
    if (questKind(row) === "state") {
      activeReplies.push(addReply("I will see to it."));
    } else {
      activeReplies.push(addReply(actionText, actionKey, row.id, childText));
    }
    activeReplies.push(
      addReply(
        "Remind me what this was about.",
        "",
        "",
        `I asked you to ${objectiveText(row)}. Look in ${areaName(row.area)} and come back when it is handled.`,
      ),
      addReply("[Leave]"),
    );
    addEntry(activeText, activeReplies, [["condition-on-quest-state", `${row.id} 1`]]);
  }

  for (const row of rows.filter((row) => row.prereq)) {
    const previous = allRows.find((candidate) => candidate.id === row.prereq);
    addEntry(`Not yet. Finish ${previous?.name ?? row.prereq} first, then I can trust the next step: ${row.name}.`, [
      addReply("[Leave]"),
    ], [["!condition-completed-quest", row.prereq], ["!condition-has-quest", row.id]]);
  }

  for (const row of rows) {
    const conditions = [["!condition-has-quest", row.id]];
    if (row.repeat !== "yes") conditions.push(["!condition-completed-quest", row.id]);
    if (row.prereq) conditions.push(["condition-completed-quest", row.prereq]);
    const detailIndex = entries.length;
    const acceptFromDetail = addReply(
      "I will handle it.",
      "action-accept-quest",
      row.id,
      "Good. I will mark the details on your datapad. Bring the result back to me.",
    );
    entries.push(entry(`${planetTone(row.planet)} I need you to ${objectiveText(row)}. You will want to start in ${areaName(row.area)}.`, [
      acceptFromDetail,
      addReply("I need more time."),
    ]));
    addEntry(`I have work for you: ${objectiveSentence(row)}. Are you available?`, [
      addReply("Tell me the details."),
      addReply(
        "I will handle it.",
        "action-accept-quest",
        row.id,
        "Good. I will mark the details on your datapad. Bring the result back to me.",
      ),
      addReply("Not right now."),
    ], conditions);
    const offerEntry = entries[entries.length - 1];
    const tellReplyIndex = offerEntry.RepliesList.value[0].Index.value;
    replies[tellReplyIndex].EntriesList.value.push(link(detailIndex));
  }

  const nonRepeatable = rows.filter((row) => row.repeat !== "yes");
  if (nonRepeatable.length) {
    addEntry("Nothing more on that work for now. If something changes, I will make sure it is posted through the proper channels.", [
      addReply("[Leave]"),
    ], [["condition-completed-quest", nonRepeatable.map((row) => row.id).join(" ")]]);
  }
  addEntry("I have nothing else that needs your help right now.", [
    addReply("[Leave]"),
  ], []);

  return {
    __data_type: "DLG ",
    DelayEntry: value("dword", 0),
    DelayReply: value("dword", 0),
    EndConverAbort: resref("nw_walk_wp"),
    EndConversation: resref("nw_walk_wp"),
    EntryList: list(entries),
    NumWords: value("dword", 0),
    PreventZoomIn: value("byte", 0),
    ReplyList: list(replies),
    StartingList: list(starts),
  };
}

function buildObjectiveDlg(row) {
  const isPickup = Boolean(row.generatedCollectItem);
  const actionKey = isPickup ? "action-give-quest-item" : "action-advance-quest";
  const actionValue = isPickup
    ? `${row.id} ${row.generatedCollectItem.resref} 1`
    : row.id;
  const completeReply = reply(isPickup ? "Recover it." : "Record the result.", actionKey, actionValue, [3]);
  const entries = [
    entry(
      isPickup
        ? `This is the place ${row.npc} described for ${row.name}. You find ${row.generatedCollectItem.name.toLowerCase()} here.`
        : `This is the place ${row.npc} described for ${row.name}. You can ${objectiveText(row)} here.`,
      [0, 1],
    ),
    entry(isPickup ? `You have what ${row.npc} needs. Return with it.` : `The work here is finished. Return to ${row.npc}.`, [1]),
    entry("There is nothing here requiring your attention.", [1]),
    entry(isPickup ? `You secure it. Better get back to ${row.npc}.` : `Done. Better get back to ${row.npc}.`, []),
  ];
  const replies = [
    completeReply,
    reply("[Leave]"),
  ];

  return {
    __data_type: "DLG ",
    DelayEntry: value("dword", 0),
    DelayReply: value("dword", 0),
    EndConverAbort: resref("nw_walk_wp"),
    EndConversation: resref("nw_walk_wp"),
    EntryList: list(entries),
    NumWords: value("dword", 0),
    PreventZoomIn: value("byte", 0),
    ReplyList: list(replies),
    StartingList: list([
      start(0, [["condition-on-quest-state", `${row.id} 1`]]),
      start(1, [["condition-on-quest-state", `${row.id} 2`]]),
      start(2, []),
    ]),
  };
}

function splitName(name) {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return [parts[0], ""];
  return [parts[0], parts.slice(1).join(" ")];
}

function clone(object) {
  return JSON.parse(JSON.stringify(object));
}

function cleanupGeneratedFiles(relativeDir, pattern, keepFiles) {
  const dir = `${root}/${relativeDir}`;
  for (const name of fs.readdirSync(dir)) {
    if (pattern.test(name) && !keepFiles.has(name)) {
      fs.unlinkSync(`${dir}/${name}`);
    }
  }
}

const generatedQuestGiverPattern = /^qg_(?:vis|mon|tat|dan|dat|hut)\d{3}$/;
const generatedObjectivePattern = /^qo_(?:vis|mon|tat|dan|dat|hut)\d{3}$/;

function cleanupGeneratedPlacements() {
  const gitDir = `${root}/Module/git`;
  for (const name of fs.readdirSync(gitDir)) {
    if (!name.endsWith(".git.json")) continue;

    const file = `${gitDir}/${name}`;
    const git = JSON.parse(fs.readFileSync(file, "utf8"));
    let changed = false;

    if (Array.isArray(git["Creature List"]?.value)) {
      const before = git["Creature List"].value.length;
      git["Creature List"].value = git["Creature List"].value.filter((creature) => {
        const resref = creature.TemplateResRef?.value ?? "";
        return !generatedQuestGiverPattern.test(resref);
      });
      changed ||= git["Creature List"].value.length !== before;
    }

    if (Array.isArray(git["Placeable List"]?.value)) {
      const before = git["Placeable List"].value.length;
      git["Placeable List"].value = git["Placeable List"].value.filter((placeable) => {
        const tag = placeable.Tag?.value ?? "";
        const template = placeable.TemplateResRef?.value ?? "";
        return template !== "qst_obj_marker" || !generatedObjectivePattern.test(tag);
      });
      changed ||= git["Placeable List"].value.length !== before;
    }

    if (changed) {
      writeJsonFile(file, git);
    }
  }
}

const utcBase = JSON.parse(fs.readFileSync(`${root}/Module/utc/visc_lysa_harn.utc.json`, "utf8"));
function makeUtc(res, npcName) {
  const utc = clone(utcBase);
  const [first, last] = splitName(npcName);
  utc.FirstName.value = { 0: first };
  utc.LastName.value = last ? { 0: last } : {};
  utc.Conversation.value = res;
  utc.Tag.value = res;
  utc.TemplateResRef.value = res;
  utc.Comment.value = "Generated planet quest giver.";
  return utc;
}

function makePlaced(utc, x, y, z, index) {
  const placed = clone(utc);
  placed.XPosition = value("float", Number(x.toFixed(3)));
  placed.YPosition = value("float", Number(y.toFixed(3)));
  placed.ZPosition = value("float", Number(z.toFixed(6)));
  const angle = (index % 8) * Math.PI / 4;
  placed.XOrientation = value("float", Number(Math.sin(angle).toFixed(9)));
  placed.YOrientation = value("float", Number(Math.cos(angle).toFixed(9)));
  return placed;
}

function makeObjectiveTemplate() {
  const marker = clone(JSON.parse(fs.readFileSync(`${root}/Module/utp/qst_item_collect.utp.json`, "utf8")));
  marker.Comment.value = "Generated planet quest objective marker.";
  marker.Conversation.value = "";
  marker.Description.value = { 0: "A small marker with fresh handling marks." };
  marker.HasInventory.value = 0;
  marker.Lockable.value = 0;
  marker.Locked.value = 0;
  marker.LocName.value = { 0: "Marked Site" };
  marker.Plot.value = 1;
  marker.Static.value = 0;
  marker.Tag.value = "qst_obj_marker";
  marker.TemplateResRef.value = "qst_obj_marker";
  marker.Useable.value = 1;
  return marker;
}

function makePlacedObjective(template, row, x, y, z, index) {
  const placed = clone(template);
  delete placed.__data_type;
  placed.__struct_id = 9;
  placed.Bearing = value("float", Number((((index % 16) / 16) * Math.PI * 2).toFixed(9)));
  placed.Conversation.value = row.objectiveResref;
  placed.Description.value = { 0: `${row.npc} asked you to ${objectiveText(row)}.` };
  placed.LocName.value = { 0: `${row.name} Site` };
  placed.Tag.value = row.objectiveResref;
  placed.TemplateResRef.value = "qst_obj_marker";
  placed.X = value("float", Number(x.toFixed(3)));
  placed.Y = value("float", Number(y.toFixed(3)));
  placed.Z = value("float", Number(z.toFixed(6)));
  return placed;
}

function coordinate(entry, field) {
  const number = Number(entry?.[field]?.value);
  return Number.isFinite(number) ? number : null;
}

function anchorName(entry) {
  return entry.TemplateResRef?.value ?? entry.Tag?.value ?? entry.LocalizedName?.value?.[0] ?? "placed object";
}

function positionedAnchor(entry, kind) {
  const x = coordinate(entry, "XPosition") ?? coordinate(entry, "X");
  const y = coordinate(entry, "YPosition") ?? coordinate(entry, "Y");
  if (x === null || y === null) return null;

  return {
    kind,
    label: anchorName(entry),
    x,
    y,
    z: coordinate(entry, "ZPosition") ?? coordinate(entry, "Z") ?? 0,
  };
}

function generatedObjectiveMarker(placeable) {
  const tag = placeable.Tag?.value ?? "";
  const template = placeable.TemplateResRef?.value ?? "";
  return template === "qst_obj_marker" && generatedObjectivePattern.test(tag);
}

function anchorsFrom(list, kind, predicate = () => true) {
  return list
    .filter(predicate)
    .map((entry) => positionedAnchor(entry, kind))
    .filter(Boolean);
}

function placementAnchors(git, area) {
  const creatures = anchorsFrom(
    git["Creature List"]?.value ?? [],
    "creature",
    (creature) => !generatedQuestGiverPattern.test(creature.TemplateResRef?.value ?? ""),
  );
  const waypoints = anchorsFrom(git.WaypointList?.value ?? [], "waypoint");
  const triggers = anchorsFrom(git.TriggerList?.value ?? [], "trigger");
  const doors = anchorsFrom(git["Door List"]?.value ?? [], "door");
  const placeables = anchorsFrom(
    git["Placeable List"]?.value ?? [],
    "placeable",
    (placeable) => !generatedObjectiveMarker(placeable),
  );
  const anchors = [...creatures, ...waypoints, ...triggers, ...doors, ...placeables];
  if (!anchors.length) {
    throw new Error(`No placed anchor found for generated quest placement in ${area}`);
  }

  return anchors;
}

function anchoredPosition(anchors, index, radius) {
  const anchor = anchors[index % anchors.length];
  const pass = Math.floor(index / anchors.length);
  const angle = (((index * 137.50776405) % 360) * Math.PI) / 180;
  const adjustedRadius = radius + Math.min(pass, 2) * 0.35;
  return {
    x: anchor.x + Math.cos(angle) * adjustedRadius,
    y: anchor.y + Math.sin(angle) * adjustedRadius,
    z: anchor.z,
  };
}

const generatedCombos = new Map();
let comboIndex = 1;
for (const row of generatedRows) {
  const key = `${row.planet}|${row.npc}|${row.area}`;
  if (!generatedCombos.has(key)) {
    generatedCombos.set(key, {
      planet: row.planet,
      npc: row.npc,
      area: row.area,
      rows: [],
      resref: `qg_${planetPrefix[row.planet]}${String(comboIndex).padStart(3, "0")}`,
    });
    comboIndex++;
  }
  generatedCombos.get(key).rows.push(row);
}

for (const combo of generatedCombos.values()) {
  const utc = makeUtc(combo.resref, combo.npc);
  writeJsonFile(`${root}/Module/utc/${combo.resref}.utc.json`, utc);
  const dlg = buildDlg(combo.npc, combo.rows);
  writeJsonFile(`${root}/Module/dlg/${combo.resref}.dlg.json`, dlg);
}

const objectiveRows = generatedRows.filter((row) => row.objectiveResref);
const generatedQuestItems = generatedRows
  .filter((row) => row.generatedCollectItem)
  .map((row) => ({
    ...row.generatedCollectItem,
    desc: `Recovered for ${row.npc}. It should be returned before anyone else decides it is useful.`,
  }));
const keepGeneratedDlgFiles = new Set([
  ...[...generatedCombos.values()].map((combo) => `${combo.resref}.dlg.json`),
  ...objectiveRows.map((row) => `${row.objectiveResref}.dlg.json`),
]);
const keepGeneratedUtcFiles = new Set([...generatedCombos.values()].map((combo) => `${combo.resref}.utc.json`));
const keepGeneratedQuestItemFiles = new Set(generatedQuestItems.map((item) => `${item.resref}.uti.json`));
cleanupGeneratedFiles("Module/dlg", /^(?:qg|qo)_(?:vis|mon|tat|dan|dat|hut)\d{3}\.dlg\.json$/, keepGeneratedDlgFiles);
cleanupGeneratedFiles("Module/utc", /^qg_(?:vis|mon|tat|dan|dat|hut)\d{3}\.utc\.json$/, keepGeneratedUtcFiles);
cleanupGeneratedFiles("Module/uti", /^qi_(?:viscara|moncala|tatooine|dantooine|dathomir|hutlar)_\d{3}\.uti\.json$/, keepGeneratedQuestItemFiles);
cleanupGeneratedPlacements();

for (const row of objectiveRows) {
  const dlg = buildObjectiveDlg(row);
  writeJsonFile(`${root}/Module/dlg/${row.objectiveResref}.dlg.json`, dlg);
}

const objectiveTemplate = makeObjectiveTemplate();
writeJsonFile(`${root}/Module/utp/qst_obj_marker.utp.json`, objectiveTemplate);

const combosByArea = new Map();
for (const combo of generatedCombos.values()) {
  if (!combosByArea.has(combo.area)) combosByArea.set(combo.area, []);
  combosByArea.get(combo.area).push(combo);
}

const objectivesByArea = new Map();
for (const row of objectiveRows) {
  if (!objectivesByArea.has(row.area)) objectivesByArea.set(row.area, []);
  objectivesByArea.get(row.area).push(row);
}

for (const [area, combos] of combosByArea) {
  const gitPath = `${root}/Module/git/${area}.git.json`;
  if (!fs.existsSync(gitPath)) throw new Error(`Missing GIT for ${area}`);
  const git = JSON.parse(fs.readFileSync(gitPath, "utf8"));
  if (!git["Creature List"]) git["Creature List"] = list([]);
  if (!Array.isArray(git["Creature List"].value)) git["Creature List"].value = [];
  const anchors = placementAnchors(git, area);
  const startingIndex = git["Creature List"].value.length;
  let changed = false;
  combos.forEach((combo, index) => {
    const utc = JSON.parse(fs.readFileSync(`${root}/Module/utc/${combo.resref}.utc.json`, "utf8"));
    const position = anchoredPosition(anchors, index, 1.25);
    git["Creature List"].value.push(makePlaced(utc, position.x, position.y, position.z, startingIndex + index));
    changed = true;
  });
  if (changed) {
    writeJsonFile(gitPath, git);
  }
}

for (const [area, rows] of objectivesByArea) {
  const gitPath = `${root}/Module/git/${area}.git.json`;
  if (!fs.existsSync(gitPath)) throw new Error(`Missing GIT for objective marker ${area}`);
  const git = JSON.parse(fs.readFileSync(gitPath, "utf8"));
  if (!git["Placeable List"]) git["Placeable List"] = list([]);
  if (!Array.isArray(git["Placeable List"].value)) git["Placeable List"].value = [];
  const existingTags = new Set(
    git["Placeable List"].value.map((placeable) => placeable.Tag?.value).filter(Boolean),
  );
  const anchors = placementAnchors(git, area);
  const startingIndex = git["Placeable List"].value.length;
  let changed = false;
  rows.forEach((row, index) => {
    if (existingTags.has(row.objectiveResref)) {
      throw new Error(`Duplicate objective marker tag ${row.objectiveResref} in ${area}`);
    }

    const position = anchoredPosition(anchors, index, 0.9);
    git["Placeable List"].value.push(
      makePlacedObjective(objectiveTemplate, row, position.x, position.y, position.z, startingIndex + index),
    );
    existingTags.add(row.objectiveResref);
    changed = true;
  });
  if (changed) {
    writeJsonFile(gitPath, git);
  }
}

const utiTemplate = JSON.parse(fs.readFileSync(`${root}/Module/uti/visc_kara_sig.uti.json`, "utf8"));
for (const item of generatedQuestItems) {
  const file = `${root}/Module/uti/${item.resref}.uti.json`;
  const uti = clone(utiTemplate);
  uti.AddCost.value = 0;
  uti.Cost.value = 0;
  uti.Description.value = { 0: item.desc };
  uti.DescIdentified.value = {};
  uti.LocalizedName.value = { 0: item.name };
  uti.Plot.value = 0;
  uti.StackSize.value = 1;
  uti.Tag.value = item.resref;
  uti.TemplateResRef.value = item.resref;
  writeJsonFile(file, uti);
}

for (const reward of uniqueRewards.values()) {
  const file = `${root}/Module/uti/${reward.resref}.uti.json`;
  if (fs.existsSync(file)) continue;
  const uti = clone(utiTemplate);
  uti.AddCost.value = reward.value;
  uti.Cost.value = reward.value;
  uti.Description.value = { 0: reward.desc };
  uti.LocalizedName.value = { 0: reward.name };
  uti.Tag.value = reward.resref;
  uti.TemplateResRef.value = reward.resref;
  writeJsonFile(file, uti);
}

const groupUpdates = new Map(
  Object.entries({
    hkinrath: 61,
    dantarihunter: 62,
    tusken_elite1: 63,
    tusken_elite2: 63,
    sandworm: 64,
    ancientsandwor: 65,
    vdathdarkadept: 67,
    vdatthrancor: 68,
    vgapingspider: 69,
    byysk_shaman: 70,
    byysk_chieftain: 71,
    byysk_champion: 72,
    qion_hive_tunnel: 73,
    huthivebroodmoth: 74,
  }),
);

function ensureGroupVar(utc, groupId) {
  if (!utc.VarTable) utc.VarTable = list([]);
  if (!Array.isArray(utc.VarTable.value)) utc.VarTable.value = [];
  let variable = utc.VarTable.value.find((candidate) => candidate.Name?.value === "QUEST_NPC_GROUP_ID");
  if (!variable) {
    variable = {
      __struct_id: 0,
      Name: cexostring("QUEST_NPC_GROUP_ID"),
      Type: value("dword", 1),
      Value: value("int", groupId),
    };
    utc.VarTable.value.push(variable);
  }
  variable.Value.value = groupId;
}

for (const [res, groupId] of groupUpdates) {
  const file = `${root}/Module/utc/${res}.utc.json`;
  if (!fs.existsSync(file)) throw new Error(`Missing UTC for group update ${res}`);
  const utc = JSON.parse(fs.readFileSync(file, "utf8"));
  ensureGroupVar(utc, groupId);
  writeJsonFile(file, utc);
}

console.log(
  JSON.stringify(
    {
      generatedQuests: generatedRows.length,
      generatedNpcCombos: generatedCombos.size,
      touchedAreas: combosByArea.size,
      objectiveMarkers: objectiveRows.length,
      generatedQuestItems: generatedQuestItems.length,
      uniqueRewardItems: uniqueRewards.size,
    },
    null,
    2,
  ),
);
