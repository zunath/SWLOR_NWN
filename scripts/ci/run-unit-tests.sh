#!/usr/bin/env bash
#
# Runs the unit test projects the maintainability CI (.github/workflows/maintainability-ci.yml) runs, with the
# same test exclusions and, unless --no-coverage is given, the same coverage collection (coverlet.runsettings).
# Run it locally to reproduce a CI result or to measure coverage:
#
#   dotnet build SWLOR.Game.Server.sln -c Release -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly
#   scripts/ci/run-unit-tests.sh --out <directory outside the repository> [--no-coverage] [--configuration Release]
#                                [project name ...]
#
# It does not build (build the solution once first). It needs SWLOR_Haks checked out with at least sw_2da, sw_tlk
# and sw_ability (Readmes/AgentEnvironment.md). Every project runs even when an earlier one fails; the exit code is
# 0 only when all of them pass. Results go to <out>/<project>/ (TRX), <out>/<project>.log and, with coverage,
# <out>/coverage/<project>.cobertura.xml.
#
# Exclusions are VSTest filter terms, listed per project below as "Class" (the whole class) or "Class.Method"
# (every case of the method). Add a term only with the reason next to it, and remove it when the cause is fixed.

set -uo pipefail

OUT=""
CONFIGURATION="Release"
COVERAGE=1
PROJECTS=()

while [ $# -gt 0 ]; do
    case "$1" in
        --out) OUT="$2"; shift 2 ;;
        --configuration) CONFIGURATION="$2"; shift 2 ;;
        --no-coverage) COVERAGE=0; shift ;;
        -h|--help) sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        -*) echo "Unknown option: $1" >&2; exit 2 ;;
        *) PROJECTS+=("$1"); shift ;;
    esac
done

if [ -z "$OUT" ]; then
    echo "--out <directory> is required (keep it outside the repository)." >&2
    exit 2
fi
if [ ${#PROJECTS[@]} -eq 0 ]; then
    PROJECTS=(SWLOR.Game.Server.Tests SWLOR.Game.Server.CharacterizationTests SWLOR.CombatSim.Tests SWLOR.CLI.Tests
              SWLOR.NWN.Formats.Tests)
fi

cd "$(dirname "${BASH_SOURCE[0]}")/../.."
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"

# Each project is its own dotnet test process, one after the other. The characterization tests seed the global
# game Random, so they must never share a process (or run in parallel) with other projects that use it.
#
# Projects deliberately not run:
#   SWLOR.Game.Server.EngineTests   runs inside a real nwserver in Docker (Readmes/EngineTesting.md).
#   SWLOR.NWN.Formats.Corpus.Tests  reads the licensed NWN base-game corpus (NWN_INSTALL_ROOT), which hosted runners
#                                   do not have.
#   SWLOR.Toolset.Tests             drives the Avalonia GUI and Wine-only tools; release-toolset.yml covers the parts
#                                   that run headless.

# Prints the exclusion terms of one project, one per line.
exclusions_for() {
    case "$1" in
    SWLOR.Game.Server.Tests)
        # ---- Failing at the base commit (468f6d706) on a full Windows checkout too, so not a runner problem. The
        # ---- tests and the code drifted apart; fix them separately and delete the lines.
        # Roslyn harness compiles Stat.RestoreStamina out of Stat.cs; the method now calls
        # IsStaminaRestorationBlocked and GetActionMode, which the harness does not define (CS0103).
        echo "ResourceRestorationTests"
        # Same harness style; it does not define DeleteLocalInt (CS0103).
        echo "StealthRestTests"
        # Asserts the text of AbilityRequirementItem.cs, which no longer matches.
        echo "FirstAidCombatUpgradeTests.FirstAidSupplyRequirements_MatchMerchantStackTemplates"
        # PlayerMessageAudit.json is stale (refresh it with tools/ExportPlayerMessageAudit.ps1).
        echo "PlayerMessageAuditTests.MessageInventory_CoversEveryRuntimeCallSite"
        # The EspionageWorkbench recipe is not in the repository's Design Bible workbook (the owner uploads Bible
        # changes).
        echo "CombatUpgradeBibleRecipeParityTests.LiveRecipes_AreDocumentedInBible"

        # ---- Hak content tests that read SWLOR_Haks folders other than sw_2da, sw_tlk and sw_ability. The job
        # ---- fetches only those three (a full checkout is 11 GB and 160,000 files). These check models, textures,
        # ---- shaders, tilesets, walkmeshes, icons and sounds, which a code refactor cannot change. Folders they
        # ---- need: sw_shader, sw_ui, sw_item, sw_item_source, sw_sound, sw_vfx, sw_tint*, sw_weapon,
        # ---- sw_cr_creature, sw_plc*, sw_pt_*, sw_t_*. Add folders to HAKS_FOLDERS in the workflow and drop the
        # ---- matching lines to run them.
        echo "AbilityImpactVisualEffectTests.EveryActiveAbilityRank_BindsItsDeclaredImpactEffect"
        echo "ArmorPartCoverageTests.EveryCatalogArmorPartHasAModelOnEveryPlayableBody"
        echo "CustomShaderMobileCompatibilityTests.CustomShaders_KeepBitwiseOperatorsOutOfMobileCode"
        echo "CustomShaderMobileCompatibilityTests.PostProcessShader_RunsTheStockPassOnMobile"
        echo "DevicesFieldEngineerTests.BeaconShotSounds_ExistInDeployedSoundResources"
        echo "DungeonEncounterWalkmeshTests"
        echo "HelmetTintTests.EveryTintableHelmetShipsRenderHeadsForEveryPlayableSpecies"
        echo "HutlarDroidFoundryAnimationTests"
        echo "ItemIconTests.EveryEssenceBlueprint_UsesReviewedOriginalArtwork"
        echo "ItemIconTests.OriginalItemArtwork_IsCompleteUniqueAndUsesGameTgaLayout"
        echo "KeyItemIconTests.ActiveKeyItemIcons_ExistAndHaveExpectedTgaFormat"
        echo "KeyItemIconTests.DefaultIcon_HasValidResrefAndExpectedTgaFormat"
        echo "LightsaberWorkbenchTests.PartCatalog_PreviewTexturesExistAndTopValuesMatchHakModels"
        echo "PartSkinAppearanceTests.ShippedTablesCoverTheReportedPartsOnEveryRobeFamilyBody"
        echo "PlaceableWalkmeshUseNodeTests"
        echo "PlayerListRulesTests.Flags_ShipTheirIconsInTheUIHak"
        echo "PowerGloveContentTests.EveryAppearanceHasACompiledModelAndNativeCompositeInventoryIcon"
        echo "PowerGloveContentTests.LeftHandModelsMirrorTheRightHandModelsAcrossTheHookXAxis"
        echo "PowerGloveContentTests.ReportedDesignsAreSeatedInTheFistAndTheRestKeepTheirAuthoredPlacement"
        echo "ShipItemIconTests.ActionArtwork_CoversAllModuleDefinitionsAndAgreesWithInventoryRoles"
        echo "ShipItemIconTests.NativeAppearances_MatchBlueprintsAndOnlyNormalizeOriginalModels"
        echo "ShoulderAppearanceTests.ShippedTableCoversAllExternalStylesOnBothSidesForEveryMaleHumanRobe"
        echo "SittablePlaceableWalkmeshTests.Bench_IsApproachedFromItsFront"
        echo "SittablePlaceableWalkmeshTests.ChancellorChair_SeatsSittersFacingTheSeatFront"
        echo "SittablePlaceableWalkmeshTests.CurvedWardrobe_BlocksWalkingThroughIt"
        echo "SittablePlaceableWalkmeshTests.DiningChair_KeepsItsUsePointInsideTheSeat"
        echo "SittablePlaceableWalkmeshTests.PlacedSeats_SeatSittersFacingTheSeatFront"
        echo "SittablePlaceableWalkmeshTests.SeatsOpeningTowardModelPlusY_SeatSittersFacingTheSeatFront"
        echo "SittablePlaceableWalkmeshTests.SmallWoodBench2_SeatsSittersAcrossTheBench"
        echo "SlicingContentTests.Lockboxes_AreUsableMiscellaneousItemsRatherThanContainers"
        echo "SlicingContentTests.SlicingNuiArt_HasEveryThemeTypeOrientationAndState"
        echo "SpeederTailRemovalTests.LargeBodyRobeModelsRemainAvailable"
        echo "SpeederTailRemovalTests.PlaceableSpeedersKeepTheirAppearanceModelsAndSharedTextures"
        echo "TelegraphTests.ShaderPalette_MatchesTheServerColorProtocol"
        echo "TintMapReviewTests.ApplyingAColorUsesOneAtomicPaletteOrRgbScalar"
        echo "TintMapReviewTests.DefaultMaleHumanHandsRetainDetailedGeometryAndTintBindings"
        echo "TintMapReviewTests.EveryGeneratedTintMaterialUsesAShaderWithPaletteDerivedSpecularity"
        echo "TintMapReviewTests.EveryPackedTintMaskDeclaresItsSingleAti2MipLevel"
        echo "TintMapReviewTests.EveryTintMaterialDeclaresScalarRowsMatchingItsShader"
        echo "TintMapReviewTests.FemaleThighSixteenVariantsExposeTheirOwnMetalAndLeatherChannels"
        echo "TintMapReviewTests.HumanHand246ModelsUseTheirAuthoredSkinTintMasks"
        echo "TintMapReviewTests.MaleMusicianOutfitKeepsItsAuthoredDyesAndConvertedThighMaterials"
        echo "TintMapReviewTests.PlainPltBodyPartsRetainLegacyTangentsWhileMappedMaterialsRetainTheirMaps"
        echo "TintMapReviewTests.PresetPickerColorsMatchThePaletteAtlasMiddletone"
        echo "TintMapReviewTests.ReportedNpcSkinHairHatDressAndPantsUseTheRepairedTintShader"
        echo "TintMapReviewTests.RodianBountyHunterPartsBindTheConvertedHumanFallbackMaterials"
        echo "TintMapReviewTests.RodianMusicianPartsBindTheConvertedHumanFallbackMaterials"
        echo "TintMapReviewTests.TintAlphaSamplerIsDeclaredEvenWithoutNormalMapping"
        echo "TintMapReviewTests.TintShadersRebuildSpecularityFromFinalPaletteInputs"
        ;;
    esac
}

# "Class" -> FullyQualifiedName!~.Class.   "Class.Method" -> FullyQualifiedName!~.Class.Method
build_filter() {
    local filter="" term
    while IFS= read -r term; do
        [ -z "$term" ] && continue
        if [[ "$term" == *.* ]]; then term=".$term"; else term=".$term."; fi
        filter+="${filter:+&}FullyQualifiedName!~$term"
    done
    printf '%s' "$filter"
}

failed=()
summary_rows=()
for project in "${PROJECTS[@]}"; do
    filter="$(exclusions_for "$project" | build_filter)"
    excluded_terms="$(exclusions_for "$project" | grep -c .)"
    args=(test "$project" -c "$CONFIGURATION" --no-build
          --logger "trx;LogFileName=$project.trx" --results-directory "$OUT/$project")
    [ -n "$filter" ] && args+=(--filter "$filter")
    [ "$COVERAGE" = 1 ] && args+=(--collect "XPlat Code Coverage" --settings coverlet.runsettings)

    echo "::group::dotnet test $project ($excluded_terms exclusion terms)"
    start=$SECONDS
    dotnet "${args[@]}" 2>&1 | tee "$OUT/$project.log"
    code=${PIPESTATUS[0]}
    echo "::endgroup::"
    # The collector writes <out>/<project>/<run guid>/coverage.cobertura.xml (and a second copy deeper down). Keep one
    # copy per project where the report step finds it.
    if [ "$COVERAGE" = 1 ]; then
        mkdir -p "$OUT/coverage"
        find "$OUT/$project" -maxdepth 2 -name coverage.cobertura.xml -exec cp {} "$OUT/coverage/$project.cobertura.xml" \;
    fi
    result="$(grep -E '^(Passed|Failed)!' "$OUT/$project.log" | tail -n 1)"
    [ -z "$result" ] && result="no test result line (exit code $code)"
    elapsed=$((SECONDS - start))
    echo "$project: $result [exit $code, ${elapsed}s]"
    summary_rows+=("| $project | $excluded_terms | $result | ${elapsed} s |")
    [ "$code" -ne 0 ] && failed+=("$project")
done

if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
    {
        echo "### Unit tests"
        echo
        echo "| Project | Exclusion terms | Result | Time |"
        echo "|---|---|---|---|"
        printf '%s\n' "${summary_rows[@]}"
        echo
    } >> "$GITHUB_STEP_SUMMARY"
fi

if [ ${#failed[@]} -gt 0 ]; then
    echo "Failed projects: ${failed[*]}" >&2
    exit 1
fi
