# Recipe minigame parameters

Every crafting minigame gives each recipe its own parameters (heat range, overload line, flavor target, and so on). The values are generated once by a deterministic profile, written explicitly into the recipe definitions so they can be reviewed, mirrored into the Bible recipe tabs, and checked by a coverage test. `RecipeDetail.MinigameParameters` holds them at runtime; recipes without parameters work as before.

A minigame workstream adds these pieces, in this order.

1. **Parameter type** in `SWLOR.Game.Server/Service/CraftService/` (namespace `SWLOR.Game.Server.Service.CraftService`, which the recipe files already import). Derive from `RecipeMinigameParameters`, keep it immutable, and name it for the builder method (`ForgeParameters` is set by `.ForgeParameters(...)`). Its constructor arguments are named like the profile's definitions: `int` for integers, `double` for decimals, the enum itself for enumerations. `Values` lists them in definition order. Any enum it uses lives in the same namespace. Values are numbers and enum members only; string literals in recipe files are scanned for item resrefs.
2. **Builder method** on `RecipeBuilder`: `public RecipeBuilder ForgeParameters(int heatCenter, ...) => MinigameParameters(new ForgeParameters(heatCenter, ...));`
3. **Profile** next to the type: derive from `RecipeParameterProfile<ForgeParameters>` with a public parameterless constructor.
   - `Key` (`"Forge"`) and `Skill`. Bible headers must start with `Key + " "`, and `"<Key> Override"` is reserved.
   - `Definitions`: `RecipeParameterDefinition.Integer`, `.Decimal` or `.Enumeration<T>` with bounds, and how far a recipe may move from its base value.
   - `BaseValues`: the value of every definition from the recipe's category, level and components.
   - `Create`, `MeasureDifficulty` and `BudgetFor(level)`: the difficulty the parameters add up to, and the range allowed at that recipe level.
   - Optional: `Overrides` (`ForCategory` / `ForRecipe`, each with a reason that goes to the Bible), `AppliesTo` (default: active recipes of the skill), `DerivedRecipes`.
4. **Generate the values.** The seed of each value is a stable hash of the profile key, the `RecipeType` name and the parameter name (never `string.GetHashCode`), so reruns give the same values, and the first variation draw that fits the level's budget wins. Renaming a `RecipeType` member changes its draw, so regenerate and review the diff, or record an override to keep the old value.

   ```powershell
   dotnet run --project tools/SWLOR.RecipeParameters -p:RunPostBuildEvent=Never -- write Forge
   ```

   It edits `SWLOR.Game.Server/Feature/RecipeDefinition` in place and prints what it changed. Review the diff, then build. `--check` changes nothing and exits 1 when a file would change. `list` shows the registered profiles.
   - A `_builder.Create(RecipeType.X, ...)` chain gets `.ForgeParameters(...)` at its end, or has its call replaced.
   - A helper that calls `_builder.Create(parameter, ...)` gains an optional `RecipeMinigameParameters parameters = null` argument that it passes to `.MinigameParameters(parameters)`, and every call gets `parameters: new ForgeParameters(...)`. A `foreach` over a record array does the same through an optional `Parameters` member. This covers the droid, trace fuse, slicing cache, venom and cooking loops.
   - A recipe that is built by copying another recipe's `RecipeDetail` has no chain to edit. `TwinPistolRecipes` does this for 11 recipes. List each one in the profile's `DerivedRecipes` (derived recipe to source recipe), and copy the source's `MinigameParameters` where the detail is built. The coverage test then requires the copy to equal its source.
   - A loop over a catalog that another tool generates (the space revamp's module, hull, component and ammo catalogs, written by `tools/SpaceRevampBalance`) cannot carry the values on its rows, because the next regeneration would drop them. Such a chain ends with `.MinigameParameters(RecipeParameterTable.For(row.RecipeType))`, and the tool writes one explicit entry per covered recipe into `Feature/RecipeDefinition/RecipeParameterTable.cs`, keeping the other profiles' entries. A covered recipe with no chain of its own goes into the table; the coverage test still fails if its loop does not read the table.
   - Any other shape stops the tool with the file and line. The tool rewrites every file in memory first and writes nothing when a shape is refused or a problem is reported (a covered recipe with no `Create` call, or a recipe the build does not know), so a failed run never leaves the folder half regenerated.
5. **Coverage test**: one class per minigame.

   ```csharp
   public class ForgeParameterCoverageTests : RecipeParameterCoverageTests
   {
       protected override IRecipeParameterProfile Profile => new ForgeParameterProfile();
   }
   ```

   It fails on a recipe without parameters, a value outside the profile's bounds, a value that differs from the generator's output (overrides are part of that output), a difficulty outside the level's budget, and a stale override.
6. **Bible columns.** The export reads the values compiled into the recipe definitions, so build after step 4.

   ```powershell
   dotnet run --project tools/SWLOR.RecipeParameters -p:RunPostBuildEvent=Never -- export "$env:TEMP\recipe-parameters.json"
   python tools/UpdateRecipeParameterBible.py --data "$env:TEMP\recipe-parameters.json"
   powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible
   ```

   The script appends one column per header after each recipe tab's last populated header (a recipe goes on the tab that lists it, so Espionage recipes land on the Engineering and Cooking tabs), and adds the widths to `tools/CombatUpgradeBibleWorkbookLayout.json` because the formatter rebuilds every tab's widths from it. It never renames a header, copies every other part of the workbook unchanged, and a second run changes nothing. Only one Bible-editing pull request merges at a time; after a conflict take the base workbook and rerun the script. `CombatUpgradeBibleRecipeParityTests` compares the columns to the parameters in code, and requires them on every tab that documents a recipe carrying parameters.

Focused tests: `dotnet test SWLOR.Game.Server.Tests --no-build --filter "FullyQualifiedName~Service.RecipeParameters|FullyQualifiedName~CombatUpgradeBibleRecipeParityTests"` and `python tools/tests/test_update_recipe_parameter_bible.py`.
