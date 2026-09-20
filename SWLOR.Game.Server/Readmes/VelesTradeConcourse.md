# Veles Trade Concourse recovery

Recovered from [PR #2051](https://github.com/zunath/SWLOR_NWN/pull/2051),
commit `b6a56eda752eb6f129c83d16c9eec152ec114567`, without merging its history.

The recovered map is `veles_tradecon`, named **Viscara - Veles - Trade
Concourse**. Its warehouse is an internal section of the same area. The area
contains ten creatures, five stores, three doors, and 266 placeables. Its
`udp2` tileset is already used by the current module's `velesinterior` area.

Only the concourse ARE/GIT/GIC and two exterior travel objects were extracted.
World content is preserved except for the original placements of the named
NPCs relocated below. Scripts and submodule revisions are unchanged. The area is registered in the module and the Toolset's Veles folder.

Integration corrections:

- The area declares `MAP_KEY_ITEM_ID=40` and `PLANET_TYPE_ID=1`, matching
  Veles Shops. Owning the Veles Colony Map reveals the concourse and its map note.
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
- NPC conversations route through `dialog_start`. The welcome droid uses a new
  SWLOR graph; relocated merchants retain their canonical conversation IDs.
  No legacy DLGs were added.
- Concourse stores have distinct tags so module-wide store lookup cannot select
  existing Veles shops. Adega, Volnatu, and Hana use `vendor_merchant`,
  `veles_volnatu`, and `night_viscflower`, with their existing store actions
  redirected to the concourse stores. Oomog's graph offers access to `DataStore`.
- Obsolete `SCRIPT_1` dispatch locals were removed from recovered teleporters;
  their `teleport` event uses the current handler directly.

## Current-content audit

Compared the current `veles_shops` and `veles_exterior` placed-object inventories
against those files at the source commit. Existing service stations remain
available. Named NPCs are relocated; their original GIT and matching GIC entries
are removed so each has exactly one placement across Veles:

| Current addition | Concourse placement |
| --- | --- |
| Sera, including her current quest conversation | West aisle, (8, 12) |
| Renna, including her current conversation and quest local | East aisle, (23, 14) |
| Espionage Workbench, crafting skill 49 | West aisle, (7, 18), with its map note |
| Contract Board | East aisle, (23, 11) |
| Training Terminal | East aisle, (23, 17); one copy provides the service of both exterior terminals |
| Viscara Market Terminal | West aisle, (7, 21), beside the workbench |

The market terminal is included to preserve the workbench's nearby market access.
Adega, Volnatu, and Hana were already represented by the recovered map; their
duplicate exterior placements are removed. Sera and Renna are removed from
Shops and the exterior and retain their canonical tags in the concourse.
Adega, Volnatu, and Hana retain their complete current exterior creature data,
including appearance, equipment, stats, feats, and canonical dialogue. Only their
position and orientation change; checksums guard the original creature data.
Repeated ambient guards, patrons, employees, and chefs remain unchanged. Merchant
stores and the welcome droid's goods store now use the current exterior's full
store data, retaining separate concourse tags and placement coordinates. The
exterior fence, invisible wall, unstuck waypoint, and travel objects are location
infrastructure and are not duplicated inside the concourse.

`tools/VelesConcourseContent.json` records each source and destination. Validation
compares copied services and stores against their current sources. Relocated
NPCs retain checksums of their original placed gameplay data, including scripts,
conversations, locals, inventory, and stats, ignoring only placement and tag
fields. All concourse NPC names, including Hana's former Flower Shop prefix, are
checked for duplicates across Veles. It also checks GIC alignment and minimum
origin spacing around
new interactions; origin spacing does not replace an engine walkmesh check.

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
