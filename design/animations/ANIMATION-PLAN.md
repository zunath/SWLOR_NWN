# Animation production plan

The current player perks have 214 animation requirements across all ranks: 7 installed custom clips, 206 active perks without an authored clip, and 1 native action-mode animation.

Of the 206 missing clips, 112 have a matching Bible image reference and 94 need a reference or motion brief. Rows represent perk requirements; a shared motion can satisfy multiple compatible perks. Passive traits and NPC-only abilities are excluded.

Beast abilities are excluded: their model-specific rigs and native animations are outside this plan. Player-performed Beast Mastery actions remain included: Tame (including Call Beast), Revive Beast, Reward, Soothe Pet, Guarding Bond, and Predatory Bond.

The existing NWN playback is recorded in [animation-plan.csv](animation-plan.csv), along with perk identifiers, current gameplay descriptions, and ability source files. An existing native gesture does not mean a custom clip has been authored. Stances, auras, and toggles are marked as shared activation candidates; Stealth uses the native action mode.

The reference links come from the cleaned Animations tab in [the Design Bible](../bible/SWLOR%20Design%20Bible%20-%20Combat%20Upgrade.xlsx). Keep the actual perk behavior as the motion brief when an older image depicts a different effect.

| Category | Installed | Needed | Existing native |
|---|---:|---:|---:|
| Beast Mastery | 0 | 6 | 0 |
| Devices | 0 | 27 | 0 |
| Espionage | 0 | 5 | 1 |
| First Aid | 0 | 12 | 0 |
| Force | 0 | 25 | 0 |
| General | 0 | 1 | 0 |
| Heavy Vibroblade | 0 | 12 | 0 |
| Katar | 0 | 11 | 0 |
| Leadership | 0 | 12 | 0 |
| Lightsaber | 0 | 11 | 0 |
| Mimicry | 0 | 1 | 0 |
| Pistol | 0 | 10 | 0 |
| Rifle | 0 | 10 | 0 |
| Saberstaff | 0 | 10 | 0 |
| Spear | 0 | 10 | 0 |
| Staff | 0 | 11 | 0 |
| Throwing | 0 | 10 | 0 |
| Twin Blade | 0 | 10 | 0 |
| Vibroblade | 7 | 2 | 0 |
| Vibroknife | 0 | 10 | 0 |

## Beast Mastery

| Perk | Work remaining | Reference |
|---|---|---|
| Guarding Bond | Create clip; shared activation candidate | Motion brief needed |
| Predatory Bond | Create clip; shared activation candidate | Motion brief needed |
| Revive Beast | Create clip | [Bible image](https://chatgpt.com/s/m_6a4015e9342c81918df41e2ad4667952) |
| Reward | Create clip | [Bible image](https://chatgpt.com/s/m_6a401601906c8191a1857b907fdd51e6) |
| Soothe Pet | Create clip | [Bible image](https://chatgpt.com/s/m_6a401621c730819199d06dd7f9d9ebfa) |
| Tame | Create clip | [Bible image](https://chatgpt.com/s/m_6a4015d8dfa8819192c3e4a893e20446) |

## Devices

| Perk | Work remaining | Reference |
|---|---|---|
| Adhesive Grenade | Create clip | [Bible image](https://chatgpt.com/s/m_6a400e9c692881918417ed815afebbf0) |
| Arc Projector | Create clip | Motion brief needed |
| Blaster Beacon | Create clip | [Bible image](https://chatgpt.com/s/m_6a401359343c81918ce3d5d9dc91bb4e) |
| Cluster Grenade | Create clip | [Bible image](https://chatgpt.com/s/m_6a400f1f20b08191b192bfb65246ba94) |
| Concussion Grenade | Create clip | [Bible image](https://chatgpt.com/s/m_6a400ceec88c8191917b475a5abeeb8c) |
| Cryo Sprayer | Create clip | Motion brief needed |
| Deflector Shield | Create clip | Motion brief needed |
| Disruption Pulse | Create clip | [Bible image](https://chatgpt.com/s/m_6a4011a0c1e881919338e80d284504bd) |
| Emergency Bunker | Create clip | Motion brief needed |
| Flamethrower | Create clip | Motion brief needed |
| Flash Grenade | Create clip | [Bible image](https://chatgpt.com/s/m_6a400d4bfca8819183e54a057e874df2) |
| Frag Grenade | Create clip | [Bible image](https://chatgpt.com/s/m_6a400b5991b481918ca4b286177de545) |
| Group Deflector | Create clip | Motion brief needed |
| Incendiary Field | Create clip | [Bible image](https://chatgpt.com/s/m_6a4013a2d2688191aa52829d0d598e03) |
| Ion Grenade | Create clip | [Bible image](https://chatgpt.com/s/m_6a400de548c08191a2e52e433e30159e) |
| Ion Lance | Create clip | Motion brief needed |
| Killzone Beacon | Create clip | [Bible image](https://chatgpt.com/s/m_6a40150693908191b2415f963e63031b) |
| Overload Barrage | Create clip | Motion brief needed |
| Power Cell | Create clip | Motion brief needed |
| Rail Dart | Create clip | Motion brief needed |
| Remote Charge | Create clip | [Bible image](https://chatgpt.com/s/m_6a40142040b88191a83f31accd0f22a5) |
| Shock Beacon | Create clip | [Bible image](https://chatgpt.com/s/m_6a4014c3a4888191b4342b40a181f3ec) |
| Signal Jammer | Create clip | [Bible image](https://chatgpt.com/s/m_6a401463a6508191ace7b88bbf18e6de) |
| Sonic Burst | Create clip | Motion brief needed |
| Thermal Detonator | Create clip | [Bible image](https://chatgpt.com/s/m_6a4013144efc8191b0243d89d47fe085) |
| Weapon Jam | Create clip | Motion brief needed |
| Wrist Rocket | Create clip | Motion brief needed |

## Espionage

| Perk | Work remaining | Reference |
|---|---|---|
| Ghost Protocol | Create clip | Motion brief needed |
| Razor Trap | Create clip | Motion brief needed |
| Shadow Step | Create clip | Motion brief needed |
| Shock Trap | Create clip | Motion brief needed |
| Stealth | Use native action mode | — |
| Tactical Escape | Create clip | Motion brief needed |

## First Aid

| Perk | Work remaining | Reference |
|---|---|---|
| Adrenal Stim | Create clip | [Bible image](https://chatgpt.com/s/m_6a401866bc6881918bc72a506660384b) |
| Antitoxin | Create clip | [Bible image](https://chatgpt.com/s/m_6a4018798aa08191b374588b7168c0e5) |
| Emergency Cocktail | Create clip | [Bible image](https://chatgpt.com/s/m_6a4019777fb481918d23999e264a87a2) |
| Emergency Triage | Create clip | [Bible image](https://chatgpt.com/s/m_6a40186099688191ae8ad66a6cb03015) |
| Focus Stim | Create clip | [Bible image](https://chatgpt.com/s/m_6a4019135b0081919a93ee984f30d508) |
| Infusion | Create clip | [Bible image](https://chatgpt.com/s/m_6a40185950b481919ddbdabbcd190525) |
| Kolto Mist | Create clip | [Bible image](https://chatgpt.com/s/m_6a40184cfbfc8191b803f7cff849403c) |
| Med Kit | Create clip | [Bible image](https://chatgpt.com/s/m_6a40183d93a48191a3929bcfd7344425) |
| Pain Suppressant | Create clip | [Bible image](https://chatgpt.com/s/m_6a40187244bc819198879a733c235565) |
| Resuscitation | Create clip | [Bible image](https://chatgpt.com/s/m_6a401852c2508191a4d7763de334d77d) |
| Shielding | Create clip | [Bible image](https://chatgpt.com/s/m_6a40186c9484819189f52f8d77a689ce) |
| Treatment Kit | Create clip | [Bible image](https://chatgpt.com/s/m_6a4018460ec08191b59719a93f7c4dea) |

## Force

| Perk | Work remaining | Reference |
|---|---|---|
| Benevolence | Create clip | [Bible image](https://chatgpt.com/s/m_6a40098b10d4819189fab42aad45880a) |
| Creeping Terror | Create clip | [Bible image](https://chatgpt.com/s/m_6a40095b45008191bc0fbd4ed33c7005) |
| Eclipse of Resolve | Create clip | [Bible image](https://chatgpt.com/s/m_6a400af972b8819188653ba7aa1d6ebe) |
| Force Burst | Create clip | Motion brief needed |
| Force Choke | Create clip | [Bible image](https://chatgpt.com/s/m_6a400968f6f081918c07dd5e20ea7419) |
| Force Drain | Create clip | [Bible image](https://chatgpt.com/s/m_6a4009776b308191b12aaa5344de7739) |
| Force Intercept | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a8471988191b2d8af89668469cd) |
| Force Judgment | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a693b108191a1b9976451674119) |
| Force Leap | Create clip | Motion brief needed |
| Force Lightning | Create clip | [Bible image](https://chatgpt.com/s/m_6a400970d3688191845ccb493ec939ba) |
| Force Push | Create clip | [Bible image](https://chatgpt.com/s/m_6a400962514c819182f205b88f37322b) |
| Force Sanctuary | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a574ab48191b2b07106e8276349) |
| Force Spark | Create clip | [Bible image](https://chatgpt.com/s/m_6a40094c0e908191b8a8f1febc24c55c) |
| Fury Stance | Create clip; shared activation candidate | Motion brief needed |
| Guardian Ward | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a49ceb08191aee9fed87c0b0c30) |
| Hunger of the Dark | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a5d5ad4819198dfc336049032db) |
| Last Stand of the Light | Create clip | [Bible image](https://chatgpt.com/s/m_6a400984230c81919d6e8c836dc07f32) |
| Mind Trick | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a7614fc8191a6f932c5661cc4c0) |
| Nightmare Field | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a7d61388191926547ac4ceab3ad) |
| Purifying Wave | Create clip | [Bible image](https://chatgpt.com/s/m_6a40097daae881919bdf9917c345ae1e) |
| Radiant Lance | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a6ed4e48191a8e11a2bfe1b2464) |
| Renewal | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a514d7c819181e47316026a28d0) |
| Throw Lightsaber | Create clip | Motion brief needed |
| Throw Rock | Create clip | [Bible image](https://chatgpt.com/s/m_6a400953c9188191a5966bb5d3cbae36) |
| Weaken Resolve | Create clip | [Bible image](https://chatgpt.com/s/m_6a400a639be881918c9e1a55d8896cbb) |

## General

| Perk | Work remaining | Reference |
|---|---|---|
| Provoke | Create clip | Motion brief needed |

## Heavy Vibroblade

| Perk | Work remaining | Reference |
|---|---|---|
| Absolute Defense | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe7c6a37081919d42f7a0d219ff19) |
| Bastion Stance | Create clip; shared activation candidate | Motion brief needed |
| Blazing Spikes | Create clip; shared activation candidate | Motion brief needed |
| Earthshatter | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe7bd01f0819198b545b822a5f39c) |
| Flash | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe7aa0b688191bc1ffbae3f88fc70) |
| Fortress Strike | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe7a1be3c819197e428ac4ec63190) |
| Rampart | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe7b4d5f48191a3a293ad2a04bb2e) |
| Sacrificial Blade | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe78ce94c8191923c385dce69df01) |
| Soul Burst | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe7935e0881919f9a8977f0355c6d) |
| Soul Devourer | Create clip; shared activation candidate | Motion brief needed |
| Soul Storm | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe79af88881919763eacb51c9ec26) |
| Soul Strike | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe784c430819197b751a58dd7821e) |

## Katar

| Perk | Work remaining | Reference |
|---|---|---|
| Adamantine Guard | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feed7eb30819199286c5ceb4124bc) |
| Guard Counter | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feebf76d8819188bcb73a39478976) |
| Hooking Strike | Create clip | Motion brief needed |
| Interrupting Sweep | Create clip | Motion brief needed |
| Iron Wall Stance | Create clip; shared activation candidate | Motion brief needed |
| Joint Lock | Create clip | Motion brief needed |
| Scrapheap Lockdown | Create clip | Motion brief needed |
| Scrapper Stance | Create clip; shared activation candidate | Motion brief needed |
| Steel Shoulder | Create clip | Motion brief needed |
| Tag In | Create clip | Motion brief needed |
| Whirling Guard | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feed1190c81918f2c0d047cdf6d64) |

## Leadership

| Perk | Work remaining | Reference |
|---|---|---|
| Break Morale | Create clip | Motion brief needed |
| Charge Order | Create clip; shared activation candidate | Motion brief needed |
| Cleanse Order | Create clip | Motion brief needed |
| Coordinated Focus | Create clip; shared activation candidate | Motion brief needed |
| Decisive Command | Create clip | Motion brief needed |
| Field Recovery | Create clip; shared activation candidate | Motion brief needed |
| Hold the Line | Create clip | Motion brief needed |
| Press the Attack | Create clip | Motion brief needed |
| Rallying Standard | Create clip; shared activation candidate | Motion brief needed |
| Rousing Shout | Create clip | Motion brief needed |
| Steady Formation | Create clip; shared activation candidate | Motion brief needed |
| Watchful Presence | Create clip; shared activation candidate | Motion brief needed |

## Lightsaber

| Perk | Work remaining | Reference |
|---|---|---|
| Aegis Eternal | Create clip | Motion brief needed |
| Epicenter | Create clip | Motion brief needed |
| Force Link | Create clip | Motion brief needed |
| Force Sheath | Create clip | Motion brief needed |
| Guardian's Challenge | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe5dbd65c8191bccdb46f63441053) |
| Imbuement Stance | Create clip; shared activation candidate | Motion brief needed |
| Immovable Stance | Create clip; shared activation candidate | Motion brief needed |
| Reprisal | Create clip | Motion brief needed |
| Saber Ward | Create clip | Motion brief needed |
| Shattering Strike | Create clip | Motion brief needed |
| Sundering Sweep | Create clip | Motion brief needed |

## Mimicry

| Perk | Work remaining | Reference |
|---|---|---|
| Overclocked Analyzer | Create clip | Motion brief needed |

## Pistol

| Perk | Work remaining | Reference |
|---|---|---|
| Dead Man's Hand | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff489b4948191ae682f2f17d1ba23) |
| Disarming Shot | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff5571f5881918a8f6b9b5378bec4) |
| Double Shot | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff37a1db08191bae5a69dbfa57476) |
| Fan the Hammer | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff3f1963c8191a6a7e92cc6bd1d19) |
| Gambler Stance | Create clip; shared activation candidate | Motion brief needed |
| Interrupting Shot | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff55ebe0c81918e686b927544c95e) |
| Last Word | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff7d3ae4c81918c21c16da3970181) |
| Point Blank Burst | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff56613808191a298ad6413e3e2f5) |
| Quick Draw | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff2c1b5c481919e430baf9844a155) |
| Skirmisher Stance | Create clip; shared activation candidate | Motion brief needed |

## Rifle

| Perk | Work remaining | Reference |
|---|---|---|
| Aimed Shot | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff929e9f081919edfe50f6a9bc5bd) |
| Crippling Shot | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff959cff8819195f3e521c2094032) |
| Headshot | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff9421f988191a70ab53a7cecaebc) |
| Kill Box | Create clip | Motion brief needed |
| One Shot | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff9494ef08191b725662a6b7db435) |
| Piercing Round | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff93387f88191ba24c62c9810a099) |
| Sniper Stance | Create clip; shared activation candidate | Motion brief needed |
| Suppressing Shot | Create clip | Motion brief needed |
| Suppression Stance | Create clip; shared activation candidate | Motion brief needed |
| Suppressive Line | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff93ab4f08191aca5d7136d5b2830) |

## Saberstaff

| Perk | Work remaining | Reference |
|---|---|---|
| Circle Slash | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fed315eb4819195d121b9e931ee33) |
| Conduit Stance | Create clip; shared activation candidate | Motion brief needed |
| Double Strike | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fecf4fa2c81918c225fa425db24f2) |
| Focused Arc | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fed1db99c81918c206e57f8d9278d) |
| Guarded Channel | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fed2441348191876b215fa365c80b) |
| Infinite Conduit | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fed0138f88191bce1e1ce63928598) |
| Maelstrom Arc | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fed10e6e481919b13b472c15510a7) |
| Saber Cyclone | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fed37bd988191b63f6fb90f4504b4) |
| Sever Focus | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fed2c09788191a4341f85af19a06f) |
| Tempest Stance | Create clip; shared activation candidate | Motion brief needed |

## Spear

| Perk | Work remaining | Reference |
|---|---|---|
| Crippling Defense | Create clip | Motion brief needed |
| Disabling Strike | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe871140081918c87ef4e2429460a) |
| Disruption Field | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe8905f688191b14ae04d2c3db16e) |
| Forcebane | Create clip | Motion brief needed |
| Hampering Barrage | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe8b8c3848191a3aac508f6c58844) |
| Interruption Strike | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe88072d48191a75a90cd774757b6) |
| Perceptive Stance | Create clip; shared activation candidate | Motion brief needed |
| Sweeping Flank | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe8b1d0c88191ab583f1203b76d96) |
| Vigor Stance | Create clip; shared activation candidate | Motion brief needed |
| Vigor Thrust | Create clip | Motion brief needed |

## Staff

| Perk | Work remaining | Reference |
|---|---|---|
| Crusher Stance | Create clip; shared activation candidate | Motion brief needed |
| Ground Quake | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff0488754819184e0403c1c74ae77) |
| Leg Sweep | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fefe794008191b8615bb2e87149ad) |
| Line Breaker | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff00a3b6481918127ff80a66682d3) |
| Rib Breaker | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff051ae6881918e2ad558fedf815a) |
| Sentinel Stance | Create clip; shared activation candidate | Motion brief needed |
| Shelter Circle | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff0114d788191a14c2825cb423b4b) |
| Slam | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff0290324819199c87607d710dd83) |
| Sweeping Guard | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feff0f6c08191ab91239ef0a61bd1) |
| Unmoving Center | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feff740e081918630adc8a76cad6f) |
| Worldbreaker | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ff01958588191844f832d8d688922) |

## Throwing

| Perk | Work remaining | Reference |
|---|---|---|
| Concussive Toss | Create clip | [Bible image](https://chatgpt.com/s/m_6a40070080208191a579612616aaff9c) |
| Explosive Toss | Create clip | [Bible image](https://chatgpt.com/s/m_6a3ffe6945a08191bcf51a9e8427fd41) |
| Flash Toss | Create clip | [Bible image](https://chatgpt.com/s/m_6a4007412ea48191b692312556342a6e) |
| Flurry Stance | Create clip; shared activation candidate | Motion brief needed |
| Ordnance Stance | Create clip; shared activation candidate | Motion brief needed |
| Perfect Flurry | Create clip | Motion brief needed |
| Piercing Toss | Create clip | [Bible image](https://chatgpt.com/s/m_6a40073b0444819197ab073f888024dd) |
| Pinning Toss | Create clip | [Bible image](https://chatgpt.com/s/m_6a40072b849c819185dda0a37c68b941) |
| Rain of Steel | Create clip | [Bible image](https://chatgpt.com/s/m_6a40071269fc8191bcfe7a571de155ea) |
| Severing Toss | Create clip | [Bible image](https://chatgpt.com/s/m_6a400733b3b0819183b7edfbb348bc2d) |

## Twin Blade

| Perk | Work remaining | Reference |
|---|---|---|
| Blade Vortex | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feb60a5f48191ab43313be342cfb3) |
| Cross Cut | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feb52ed80819195c17d3ba5a7dab7) |
| Cyclone Stance | Create clip; shared activation candidate | Motion brief needed |
| Lacerating Twin Cut | Create clip | Motion brief needed |
| Lacerator Stance | Create clip; shared activation candidate | Motion brief needed |
| Red Bloom | Create clip | Motion brief needed |
| Serrated Arc | Create clip | Motion brief needed |
| Spinning Whirl | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feb595e8881919865af1495a6aadf) |
| Tempest Bloom | Create clip | [Bible image](https://chatgpt.com/s/m_6a3feb6f01608191afeabf6292bfb388) |
| Twin Rupture | Create clip | Motion brief needed |

## Vibroblade

| Perk | Work remaining | Reference |
|---|---|---|
| Berserker Stance | Create clip; shared activation candidate | Motion brief needed |
| Covering Strike | Installed | [Bible image](https://chatgpt.com/s/m_6a3fde0e8a34819181290c21cdf5dc91) |
| Defensive Stance | Create clip; shared activation candidate | Motion brief needed |
| Invincible | Installed | [Bible image](https://chatgpt.com/s/m_6a3fde75a0d48191afd875e8ee2478e4) |
| Rending Strike | Installed | [Bible image](https://chatgpt.com/s/m_6a3fdf4096e48191a17110bd8b912105) |
| Riot Blade | Installed | [Bible image](https://chatgpt.com/s/m_6a3fdf350b2c8191afe4955dd4f3799e) |
| Savage Cleave | Installed | [Bible image](https://chatgpt.com/s/m_6a3fdf48482c81918a027a9a66430b3c) |
| Shield Bash | Installed | [Bible image](https://chatgpt.com/s/m_6a3fdd47b8f88191a44d7c29b566f837) |
| Shield Wall | Installed | [Bible image](https://chatgpt.com/s/m_6a3fdd84a41c8191ab3fcad9d97786f7) |

## Vibroknife

| Perk | Work remaining | Reference |
|---|---|---|
| Assassin's Stance | Create clip; shared activation candidate | Motion brief needed |
| Backstab | Create clip | [Bible image](https://chatgpt.com/s/m_6a3fe24b167c8191bd0127d264800c95) |
| Crippling Slice | Create clip | Motion brief needed |
| Escape Artist | Create clip | Motion brief needed |
| Pathogen Strike | Create clip | Motion brief needed |
| Shadowflow Stance | Create clip; shared activation candidate | Motion brief needed |
| Veiled Strike | Create clip | Motion brief needed |
| Viral Cascade | Create clip | Motion brief needed |
| Virulent Blade | Create clip | Motion brief needed |
| Volatile Compound | Create clip | Motion brief needed |

## Removed Bible plans

47 unused plans were removed from the Animations tab. The other workbook tabs and their formula caches are preserved.

| Category | Removed plan | Reason |
|---|---|---|
| Vibroblade | Hacking Blade | No matching current perk |
| Vibroblade | Carve | No matching current perk |
| Vibroknife | Cheap Shot | Now a passive trait |
| Vibroknife | Smoke Bomb | No matching current perk |
| Vibroknife | Vital Strike | No matching current perk |
| Vibroknife | Enfeebling Strike | No matching current perk |
| Vibroknife | Hamstring | No matching current perk |
| Vibroknife | Nerve Strike | No matching current perk |
| Vibroknife | Incapacitate | No matching current perk |
| Vibroknife | Systemic Shutdown | No matching current perk |
| Lightsaber | Taunting Deflection | No matching current perk |
| Lightsaber | Punishing Strike | No matching current perk |
| Lightsaber | Guardian Master | No matching current perk |
| Lightsaber | Versatile Strike | No matching current perk |
| Lightsaber | Leg Slash | No matching current perk |
| Lightsaber | Brutal Assault | No matching current perk |
| Lightsaber | Saber Storm | No matching current perk |
| Spear | Force Suppression | No matching current perk |
| Spear | Total Force Denial | No matching current perk |
| Spear | Side Assault | No matching current perk |
| Spear | Flanking Barrage | No matching current perk |
| Twin Blade | Storm Release | No matching current perk |
| Twin Blade | Split Guard Strike | No matching current perk |
| Twin Blade | Feinting Cut | No matching current perk |
| Twin Blade | Binding Cross | No matching current perk |
| Twin Blade | Duelist's Challenge | No matching current perk |
| Twin Blade | Final Form | No matching current perk |
| Saberstaff | Tempest Release | No matching current perk |
| Saberstaff | Force Capacitor | No matching current perk |
| Katar | Twin Intercept | No matching current perk |
| Katar | Striking Cobra | No matching current perk |
| Katar | Static Palm | No matching current perk |
| Katar | Neural Shock | No matching current perk |
| Katar | Current Overload | No matching current perk |
| Katar | Serpent's Eclipse | No matching current perk |
| Staff | Bonecrusher | No matching current perk |
| Pistol | Smoke Round | No matching current perk |
| Rifle | Tranquilizer Shot | No matching current perk |
| Rifle | Pinning Fire | Now a passive trait |
| Rifle | Tranq Cone | No matching current perk |
| Rifle | Pacification Field | No matching current perk |
| Rifle | Stasis Volley | No matching current perk |
| Throwing | Fireburst Toss | No matching current perk |
| Throwing | Finishing Toss | No matching current perk |
| Throwing | Perfect Throw | No matching current perk |
| Beast Mastery | Snarl | No matching current perk |
| Beast Mastery | Growl | No matching current perk |
