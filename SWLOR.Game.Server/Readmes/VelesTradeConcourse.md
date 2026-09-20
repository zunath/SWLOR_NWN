# Veles Trade Concourse recovery

Recovered from [PR #2051](https://github.com/zunath/SWLOR_NWN/pull/2051),
commit `b6a56eda752eb6f129c83d16c9eec152ec114567`, without merging its history.

The recovered map is `veles_tradecon`, named **Viscara - Veles - Trade
Concourse**. Its warehouse is an internal section of the same area. The area
contains eight creatures, five stores, three doors, and 262 placeables. Its
`udp2` tileset is already used by the current module's `velesinterior` area.

Only the concourse ARE/GIT/GIC and two exterior travel objects were extracted.
Current exterior objects, other areas, scripts, and submodule revisions were
preserved. The area is registered in the module and the Toolset's Veles folder.

Integration corrections:

- The exterior entrance at (177.00, 22.17) targets
  `V_Veles_To_Concourse`, the main concourse entrance. The old PR incorrectly
  targeted the warehouse landing. The return door targets the restored exterior
  waypoint `V_Concourse_To_Veles` at (175.50, 22.17). Both exterior travel
  points are on the street side of the current fence and invisible wall; the
  PR's original positions were behind or overlapping those barriers.
- Internal warehouse travel retains the original two waypoints. Each landing
  is within three metres of its corresponding return interaction.
- Usable transition doors are unlocked, un-lockable, and plot protected.
  Entrances and exits have destination labels. The unused third elevator is
  locked, requires an unavailable key, is plot protected, and is explicitly
  labelled **Out of Service** rather than presented as a usable exit.
- NPC conversations route through `dialog_start`. The new welcome-droid and
  flower-shop dialogue is authored as SWLOR graphs; no legacy DLGs were added.
- Concourse stores have distinct tags so module-wide store lookup cannot select
  existing Veles shops. The food merchants use separate graphs preserving their
  existing dialogue. Oomog's existing graph now offers access to `DataStore`.
- Obsolete `SCRIPT_1` dispatch locals were removed from recovered teleporters;
  their `teleport` event uses the current handler directly.

Run `python tools/ValidateVelesConcourse.py` to check registration, tile count,
paired travel destinations and nearby return interactions, street-side exterior
placement, door usability, global waypoint uniqueness, merchant routing, unique
store tags, and inventory resources.
Relevant .NET coverage is `ConversationGraphCorpusTests`,
`ConversationArchitectureTests`, `VelesMilitiaAnnexPlacementTests`, and
`NPCEnemyBalanceAuditTests.AllNpcHpBudgets_AccountForNativeVitalityAndToughnessRules`.

Deployment requires a server build for the embedded conversations and a module
repack. An in-game walkthrough remains necessary to confirm appearance,
walkability, entrance placement, both warehouse directions, and merchant behavior.
