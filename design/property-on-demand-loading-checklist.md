# Property On-Demand Loading Checklist

## Purpose

Reduce server boot time and resource usage by avoiding boot-time loading of private player properties, while preserving production safety for public city/building gameplay and every existing assumption that a live property is fully loaded.

## Agreed Decisions

- Apartments, starships, and private adjustable building interiors should load on demand.
- Public and publicly accessible building interiors should still load at boot time, but their loading should be batched.
- All property entry paths must be gated by load state. A property is enterable only when it is fully loaded.
- A property area may exist before loading is complete, but it must not be considered usable until all required child structures and hooks are complete.
- If a player tries to enter a property that is loading, show an error telling them to try again shortly.
- Notify waiting players when the property finishes loading, but do not auto-enter them.
- Player-requested on-demand loads should have priority over remaining startup-load jobs, while still obeying the same batch throttle.
- Public building doors should not be exposed until their interior property is fully loaded.
- Load failures must fail closed. Do not mark a partial load as live.
- Runtime recovery should be available through an admin-only NUI diagnostic window opened with `/propertydiagnostics`; repair actions should remain in the window rather than becoming chat-command verbs.
- First-pass runtime recovery should be non-destructive only: status, retry, abort, and notify waiters.
- Do not include destructive repair/quarantine tools in the first pass.
- Defer idle property unloading to a later, separately reviewed change.

## Core Invariants

- `Loaded` means the area exists, has the property id assigned, has the correct name, all child structures are spawned, layout spawn hooks have run, and any required building exit/dock metadata is ready.
- `Loading` is not enterable, even if an area object has already been created.
- `Failed` is not enterable and should require staff action or a later server restart/repair path.
- A missing registered instance can mean either `Unloaded`, `Queued`, `Loading`, or `Failed`; callers must not treat it as an impossible state.
- Public startup properties may still be unavailable briefly after boot because they are batched.
- Entry permission must be rechecked on each retry after load completion.
- Display-only flows should not force-load a property.
- Rename/manage flows should update the DB and only update an area object when the property is already loaded.
- Public dock points, city behavior, and building entry points must remain available after their startup load job completes.

## Proposed Load States

- `Unloaded`: No live area has been created for this property in the current server process.
- `Queued`: A load job exists but has not begun.
- `Loading`: The load job is actively creating the area and/or spawning child content.
- `Loaded`: The property is fully live and enterable.
- `Failed`: A load job failed and entry is refused until staff/admin recovery.

## Proposed Public APIs

- `TryGetLoadedInstance(propertyId, out PropertyInstance instance)`
- `GetPropertyLoadState(propertyId)`
- `QueuePropertyLoad(propertyId, priority, requester, notifyOnComplete)`
- `TryResolveEnterableInstance(player, propertyId, out PropertyInstance instance)`
- `IsPropertyLoading(propertyId)`
- `IsPropertyLoadFailed(propertyId)`
- `CanPropertyLoadOnDemand(WorldProperty property)`
- `CanPropertyLoadAtStartup(WorldProperty property)`

Do not make raw dictionary access create areas implicitly.

## Load Queue Rules

- Use one shared property load queue.
- Process jobs on the main server thread through the existing scheduler pattern.
- Use a bounded per-tick budget for structure spawning.
- Recommended initial batch size: 5 child structures per tick, adjustable after profiling.
- Priority order:
  1. Current in-progress job batch.
  2. Player-requested on-demand jobs.
  3. Startup public property jobs.
- Do not allow duplicate jobs for the same property.
- If multiple players request the same loading property, attach them to the existing job's waiter list.

## Startup Loading Checklist

- Run existing cleanup and permission refresh first.
- Load area-backed city properties.
- Queue startup-loaded instance properties in batches.
- Queue startup world structures only after their parent area/interior is loaded.
- Skip apartment, starship, and private adjustable instance properties at boot.
- Still spawn exterior structures for private adjustable buildings so the door can request the interior load.
- Skip child structures whose parent is an unloaded apartment or starship.
- Remove all property instance template resrefs from the persistent area cache, not only apartment templates.
- Log loaded counts separately:
  - startup instances queued
  - startup instances loaded
  - on-demand-capable instances skipped
  - world structures loaded
  - failed loads

## On-Demand Loading Checklist

- Trigger on first entry attempt for apartments, starships, and private adjustable building interiors.
- If state is `Unloaded`, queue a high-priority load and tell the player to try again shortly.
- If state is `Queued` or `Loading`, attach the player to the waiter list and tell them to try again shortly.
- If state is `Loaded`, continue through normal permission and jump logic.
- If state is `Failed`, tell the player the property failed to load and to notify staff.
- On completion, notify waiting players that the property is ready.
- Do not auto-enter waiting players.

## Player-Facing Messages

Use short, non-technical messages:

- Loading: `This property is still loading. Please try again shortly.`
- Ready: `Your property is ready. You may try entering again.`
- Failed: `This property failed to load. Please notify staff.`
- No permission: keep existing permission messaging.

## Public Building Door Checklist

- Do not spawn or expose an exterior entry door until the interior property is fully loaded.
- When a startup building structure loads:
  - Load its child interior first.
  - Spawn the exterior structure.
  - Run the structure changed action only after the interior is `Loaded`.
  - Assign the building exit location to the loaded interior.
- Keep `EnterBuilding()` defensive:
  - Resolve the building's interior id.
  - Check that the interior load state is `Loaded`.
  - If not loaded, show the loading message and refuse entry.
  - Recheck access permission before jumping.

## Failure Handling Checklist

- Wrap each load job with error capture that records:
  - property id
  - property type
  - layout
  - current phase
  - child structure id, if applicable
  - exception text
- Mark the property `Failed` in memory when a load job fails.
- Notify waiting players with the failed message.
- Refuse entry for failed properties.
- Do not retry automatically in a tight loop.
- Do not mark partial areas as loaded.
- Avoid duplicate area/placeable creation when retrying after partial failure.

## Admin Diagnostic NUI Checklist

Create an admin-only diagnostic NUI, not chat commands.

Minimum views:

- Current queued jobs.
- Current loading jobs.
- Failed jobs.
- Loaded on-demand properties.

Minimum fields:

- property id
- custom name
- property type
- owner player id
- load state
- queue priority
- spawned child count
- expected child count
- valid loaded area object
- waiter count
- last phase
- last failure summary

Allowed actions:

- Retry load.
- Abort loading.
- Notify waiters.
- Refresh list.

Disallowed in first pass:

- Force loaded.
- Delete/quarantine child structure.
- Edit ownership or permissions.
- Raw arbitrary property mutation.
- Chat-command repair verbs.

## Caller Audit Checklist

Every direct use of registered property instances must be reviewed and classified.

Known sites to audit:

- `Property.GetRegisteredInstance`
- `Property.EnterProperty`
- `Property.EnterBuilding`
- `PropertyExitDialog.ReturnToLastDockedPosition`
- `StarportDockDialog` player dock display names
- `ShipManagementViewModel` specific-property initialization
- `ShipManagementViewModel.GetShipLocation`
- `ShipManagementViewModel.OnClickBoardShip`
- ship rename logic
- apartment rename logic
- `Space` combat/effects that reference active player ship instances
- `StructureChangedAction.AssignExitLocationToInstance`
- `StructureChangedAction.AdjustBuildingName`
- `StructureChangedAction.RetrieveStarport`
- `ManageCityViewModel` city-upgrade hooks
- `ManageStructuresViewModel` structure lookup and placeable assumptions

Classification rules:

- Entry/jump paths may request a load, then must refuse entry until loaded.
- Display paths should use DB data and must not request a load.
- Already-inside-property paths can assume loaded only after validating the current area has a property id and loaded state.
- Startup/public hooks should run only after the relevant property is loaded.

## Test Plan Checklist

Unit or feature tests should cover:

- Apartments are skipped at boot.
- Starships are skipped at boot.
- Private adjustable building interiors are skipped at boot.
- Public startup properties are queued and loaded.
- Child structures under unloaded apartments/starships are not spawned at boot.
- On-demand entry request queues a load and refuses immediate entry.
- Second entry attempt during loading refuses and does not duplicate the job.
- Load completion notifies waiters but does not auto-enter.
- Loaded retry rechecks permission and enters only if allowed.
- Failed load refuses entry.
- Rename updates DB when unloaded and area name when loaded.
- Player dock display uses DB property name without forcing instance load.
- Public building door is not created before interior load completion.
- `EnterBuilding()` refuses entry when interior is not loaded.
- Admin diagnostic retry requeues failed/stuck jobs without destructive mutation.

Manual verification should cover:

- Boot log shows private properties skipped and public load batches progressing.
- A player entering an apartment immediately after boot receives the loading message, then ready notification.
- A player entering a public city building before its interior finishes loading is refused or cannot access the door.
- Starship boarding works after on-demand load completes.
- Docking to NPC and player starports still works.
- Emergency exit still returns to a valid dock.
- Existing property storage and structure management works after load completion.

## Implementation Order

1. Add load policy/state models and internal load job data structures.
2. Add safe instance lookup APIs.
3. Refactor property spawning into area creation, child structure spawning, and completion phases.
4. Add queue scheduler and batching.
5. Convert startup loading to use the queue.
6. Add on-demand request handling for apartment/starship entry.
7. Gate all property entry paths.
8. Update display/rename callers to avoid forced loads.
9. Add public building door deferral.
10. Add admin diagnostic NUI with non-destructive actions.
11. Add tests.
12. Run targeted tests, then broader server tests.

## Out Of Scope For First Pass

- Idle unloading of loaded properties.
- Destructive in-game property repair.
- Chat command recovery tools.
- Changing public property types to on-demand.
- Changing property ownership, permission, or lease rules.
- Refactoring unrelated property management UI.
