# Weather system

Weather is managed by `Service/Weather.cs`. `WeatherPattern`, `WeatherAreaState`,
`WeatherConditions`, and `WeatherExposure` contain the independently testable timing,
climate, storm, feedback, and damage rules.

## Timing and conditions

- A shared weather front advances no more than once per **60 real minutes**.
  The six-second SWLOR heartbeat checks its UTC deadline. Game hours, midnight,
  dawn/dusk, calendar changes, and area traffic cannot shorten that interval.
  Restarting the module initializes a new front. A late heartbeat advances once
  and schedules the next update one hour later; it does not replay missed updates.
- Initial temperature retains the seasonal formula and day/night adjustment.
  Subsequent updates move temperature at most one point toward that target,
  including at dawn/dusk. Humidity also moves at most one point per update.
  Wind retains the existing two-d10 distribution, bounded from 1 to 10.
- Each area retains one condition snapshot per front. Entering creatures and
  multiple players do not reroll storms. All outdoor, above-ground areas receive
  scripted precipitation at initialization and on each update when a climate is
  configured. Interior, underground, sealed station, and space areas are excluded.
- The shared `Planet.GetPlanetType` lookup supplies the climate, including
  authored planet IDs and resref fallbacks. `VAR_WEATHER_CLIMATE` (area string)
  overrides that lookup with a name from `WeatherPlanetDefinitions.GetNamedClimates`.
  Unknown areas/invalid overrides retain native authored ambience and receive no
  scripted hazards. `None` explicitly disables scripted weather. Generic prefabs
  need an explicit climate when builders want scripted weather in their instances.
- Hutlar has humidity +2 and maximum heat 3, allowing ordinary snowfall without
  rain or thunderstorms. Frozen Wastes has local humidity +9 to preserve its
  authored permanent snowfall. Its blizzards still require strong wind.
- Kashyyyk uses heat +3, humidity +3, and minimum heat 4; Ossus uses heat +2,
  humidity -1, and minimum heat 4. Their nine outdoor areas explicitly select
  those profiles. These weather profiles do not register new galaxy-map planets.
  CZ-220 and Smuggler's Moon Station always use sheltered profiles.
- Local `VAR_WEATHER_HEAT`, `VAR_WEATHER_HUMIDITY`, and `VAR_WEATHER_WIND`
  modifiers are combined with the planet climate. Artificial areas retain their
  one-point wind shelter bonus. Final indices are clamped to 1–10; heat also
  respects the profile's minimum/maximum.
  `SetAreaHeatModifier`, `SetAreaHumidityModifier`, and `SetAreaWindModifier`
  immediately refresh the edited area; these explicit builder changes bypass
  the ordinary hourly cadence.

Humidity above 7 produces snow at heat 1–3, mist at heat 4–5 with wind below 3,
and rain otherwise. Lower humidity produces clear weather. Mist uses native clear
precipitation plus grounded mist placeables. Placeables are created only in occupied
areas, at up to one per eight tiles, and removed as soon as mist ends. Empty instance
templates are not populated with generated mist. Sand/snow storms restore the
area's original sun/moon fog colors, fog amounts, and skybox when they end.

During warm rain (heat above 4), a new thunderstorm has a wind-in-20 chance to
start. An existing thunderstorm has a one-in-three chance to continue. Strong wind
(9–10) can instead trigger the planet's sand or snow storm with a one-in-three
chance. These are mutually exclusive storm states. Lightning attempts occur once
per minute per occupied area during a thunderstorm, with a one-in-three strike
chance; there are no delayed strikes that can fire after the storm ends. Damage
falloff is nonnegative, and zero-damage targets are not knocked down.

## Exposure and feedback

Acid rain requires actual native rain (including a DM's manual override) plus the
area's `VAR_WEATHER_ACID_RAIN = 1` flag. Sand and snow storms use their current
condition snapshot. Living ordinary players take the existing damage amounts:
acid rain starts with 2d6 and continues at 1d6; sand/snow storms deal 2d6
bludgeoning/cold damage. Each player has one six-second damage clock shared by
entry and heartbeat. Shelter, death, staff possession, or the hazard ending stops
damage. Re-entry cannot stack pulses, and disconnected-player clocks are removed.
There is no passive damage from a low temperature alone.

Before native damage is applied, creature targets now use the shared resistance
and damage-taken services. Blizzard damage uses **Ice resistance**, acid rain uses
**Poison resistance** (SWLOR maps that element to native acid), and lightning uses
**Electrical resistance**. Sandstorms respect physical-damage percent reductions
and physical immunity. Generic damage reductions, Leadership reductions,
damage-sharing/protection effects, and fatal-damage prevention also apply through
the shared service. Doors/placeables struck by lightning bypass character stats.

Protection comes from items, food, or active effects that actually grant these
stats. Clothing appearance, armor's ordinary Physical Defense rating, provisions,
and decorative roofs/trees do not provide weather protection on their own. Entering
an area marked interior/underground stops exposure. This is an area-wide shelter
rule; there is no geometric cover test inside an outdoor area.

Unprotected sandstorms/blizzards average 7 HP per pulse (about 70 HP/minute); ongoing
acid rain averages 3.5 HP per pulse (about 35 HP/minute after the initial hit).
These are the existing dice, not newly increased damage. Resistance follows the
shared rounding/minimum-damage rules: positive damage normally stays at least 1,
while temporary immunity can reduce it to zero. Lightning starts at 11–110 damage,
loses 10 per metre, and has a 3–6m radius; Mobility resistance shortens its knockdown,
and knockdown immunity prevents it. Fully prevented lightning hits do not knock down.
Ordinary rain, snow, fog, heat and cold do not deal damage or slow travel. No authored
area currently enables acid rain; it remains an explicit builder opt-in.

Entry and hourly updates deliver the authored climate description or an explicit
acid/sand/snow hazard warning. Routine damage pulses do not repeat the weather
paragraph. Snow impacts use cold feedback; sand damage no longer displays flames.
Descriptions no longer promise protection from winter clothes, boots, or provisions,
or imply movement penalties that the weather system does not implement.

## Area configuration audit

[WeatherAreaAudit.md](WeatherAreaAudit.md) records all 453 authored areas, including
prefabs, their shelter flags, and their selected climate or native-only policy.
The review corrected 26 exposed interiors: shops, labs, ship/station corridors,
crypts, caves, offices, warehouses, and the forgeworks. It deliberately preserves
the open Canyon Dueling Pit, Sith Fortress Courtyard, and Academy Rooftop despite
their interior tilesets. The fixes change ARE flags and ten GIT local-variable
tables; repack and deploy the module together with the server assembly.

`WeatherAreaConfigurationTests` checks the complete ARE/GIT corpus for exposed
interior tilesets, missing live-planet climates, invalid profile names, station
shelter, and permanent Frozen Wastes snowfall. The tileset shelter metadata was
checked against the existing HAK `.set` files; no HAK resources changed. Area flags
and authored content support these classifications; in-client roof rendering and
walkable layouts still require the checks below.

## Review findings repaired

The previous implementation could update every six real minutes (the module has
six minutes per game hour), rerolled temperature by four points, and applied another
four-point dawn/dusk jump. It read the module's humidity and wind as both the base
and the area modifier, doubling them. Every area entry rerolled storm state and
every occupant caused another area update. The thunderstorm start branch required
an already active thunderstorm, making ordinary starts impossible; its continuation
probability also differed from the documented one-in-three rule.

Weather separately parsed area names using enum descriptions, so `Mon Cala` missed
its `MonCala` climate and names containing hyphens were split incorrectly. The
hour-only scheduler could miss a scheduled update or skip initialization at hour
zero. Fog creation/cleanup depended on another area entry, used an entering
creature's elevation for unrelated tiles, and started its placement counter at a
random d6 instead of zero.

Repeating hazard callbacks rejected normal players because their master was not
a player. Their remaining guards never checked whether the hazard still existed,
and repeated starts could stack callbacks. An unused cold callback dealt acid
damage. Hazard branches also bypassed the existing sand/snow warning text.
Lightning could compute negative damage near the edge of a weak strike's radius.

## Verification and in-engine checks

`WeatherTests` covers hourly timing, missed deadlines, game-clock independence,
10,000 simulated updates, climate/local modifiers, precipitation boundaries,
exact storm probabilities, repeated area entries, hazard cessation and duplicate
pulses, lightning falloff, and runtime event/visual-cleanup wiring. The player
message audit is refreshed for the retained weather messages.

After repacking/deploying the module and server assembly, verify in NWN:

1. Stay in one outdoor area across dawn/dusk and repeated transitions; precipitation
   must remain stable until the next real-hour update. Crossing different planet
   climates can still show different weather.
2. Observe Mon Cala, Tatooine, Hutlar, and Viscara climate text and precipitation.
   Check an interior, an underground area, and space for unwanted effects.
3. Force mist using the area modifier helpers; verify ground placement, no duplicate
   mist on re-entry, and cleanup when humidity is reduced. Create an outdoor
   instance from an empty template and check for copied mist.
4. During acid rain or a regional storm, verify damage continues every six seconds,
   then stops on shelter, death, or clearing conditions. Re-enter rapidly and verify
   the pulse count does not increase. Staff avatars/possessed creatures are exempt.
5. Verify storm skies and regional fog restore their original values, lightning
   stops when the storm ends, and cold damage uses the frost impact effect.
6. Compare Ice/Poison/Electrical resistance 0 and 50 during the matching hazard,
   then test temporary immunity and physical-damage reduction during a sandstorm.
   Confirm protected hits use the shared rounding rules and fully prevented
   lightning does not knock down.
7. Check the corrected shops, outpost, station and caves for shelter, and the
   three deliberately open layouts for outdoor weather. Verify Hutlar snowfall,
   persistent Frozen Wastes snow, and rain without snow on Kashyyyk/Ossus.

Native client rendering and live NWNX event behavior require these in-engine
checks; the unit tests do not emulate an NWN server. Native DMFI weather buttons
can override precipitation visually until the next scripted front; they do not
edit the scripted climate or storm state.
