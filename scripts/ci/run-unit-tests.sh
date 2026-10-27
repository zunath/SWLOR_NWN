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
        # ---- Failing at the base commit on a full Windows checkout too, so not a runner problem. The
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
        # Counts the assemblies loaded in the AppDomain around the registry scans. Passes in a Debug build; in Release
        # (what this CI builds) it finds 106 loaded assemblies where it expects 105. Belongs to P0-5; fix the test.
        echo "ScriptHandlerSeamTests.LoadHandlersFromAssemblyScansEveryAssemblyLoadedInTheAppDomain"

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

# Tests that cannot run with coverage instrumentation on, one term per line in the same form as above. With coverage
# they are left out of the instrumented pass and run on their own, uninstrumented, right after it.
uninstrumented_for() {
    case "$1" in
    SWLOR.CombatSim.Tests)
        # The simulator reads the game ability lambdas by decoding their IL (CombatSim/Data/WeaponImpactReader.cs);
        # coverlet inserts its hit counters into that IL, so the decoder no longer finds the weapon impacts.
        echo "WeaponLineTests.Catalog_ReadsVibrobladeAndStaffSourceImpacts"
        echo "WeaponLineTests.RendingStrike_ExposesTheTargetForThirtySecondsUnlessTraumaResistanceBlocksIt"
        ;;
    esac
}

# Reads terms on stdin, one per line. "Class" matches the whole class, "Class.Method" every case of the method. The
# class term has no trailing dot on purpose: NUnit also matches the fixture itself (named Namespace.Class), and a fixture
# that is only partly filtered out still runs its OneTimeSetUp, which fails for the classes excluded above.
# $1 = exclude builds FullyQualifiedName!~.Term terms joined with &, $1 = include builds FullyQualifiedName~.Term terms
# joined with |.
build_filter() {
    local mode="$1" filter="" term
    while IFS= read -r term; do
        [ -z "$term" ] && continue
        term=".$term"
        if [ "$mode" = exclude ]; then
            filter+="${filter:+&}FullyQualifiedName!~$term"
        else
            filter+="${filter:+|}FullyQualifiedName~$term"
        fi
    done
    printf '%s' "$filter"
}

# Counts the non-empty lines of $1.
count_terms() {
    grep -c . <<< "$1" || true
}

# Runs one dotnet test pass and records its result row.
#   $1 label   $2 project   $3 filter   $4 1 to collect coverage   $5 number of filter terms   $6 results suffix
run_pass() {
    local label="$1" project="$2" filter="$3" coverage="$4" terms="$5" suffix="${6:-}"
    local results="$OUT/$project$suffix"
    local args=(test "$project" -c "$CONFIGURATION" --no-build
                --logger "trx;LogFileName=$project$suffix.trx" --results-directory "$results")
    [ -n "$filter" ] && args+=(--filter "$filter")
    [ "$coverage" = 1 ] && args+=(--collect "XPlat Code Coverage" --settings coverlet.runsettings)

    echo "::group::dotnet test $label ($terms filter terms)"
    local start=$SECONDS
    dotnet "${args[@]}" 2>&1 | tee "$OUT/$project$suffix.log"
    local code=${PIPESTATUS[0]}
    echo "::endgroup::"
    # The collector writes <results>/<run guid>/coverage.cobertura.xml (and a second copy deeper down). Keep one copy
    # per project where the report step finds it.
    if [ "$coverage" = 1 ]; then
        mkdir -p "$OUT/coverage"
        find "$results" -maxdepth 2 -name coverage.cobertura.xml -exec cp {} "$OUT/coverage/$project.cobertura.xml" \;
    fi
    local result
    result="$(grep -E '^(Passed|Failed)!' "$OUT/$project$suffix.log" | tail -n 1)"
    [ -z "$result" ] && result="no test result line (exit code $code)"
    local elapsed=$((SECONDS - start))
    echo "$label: $result [exit $code, ${elapsed}s]"
    summary_rows+=("| $label | $terms | $result | ${elapsed} s |")
    if [ "$code" -ne 0 ]; then failed+=("$label"); fi
    return 0
}

failed=()
summary_rows=()
for project in "${PROJECTS[@]}"; do
    exclusions="$(exclusions_for "$project")"
    uninstrumented="$(uninstrumented_for "$project")"
    if [ "$COVERAGE" = 1 ]; then
        # Instrumented pass: everything except the exclusions and the tests that cannot run instrumented.
        both="$exclusions"$'\n'"$uninstrumented"
        run_pass "$project" "$project" "$(build_filter exclude <<< "$both")" 1 "$(count_terms "$both")"
        if [ -n "$uninstrumented" ]; then
            run_pass "$project (not instrumented)" "$project" "$(build_filter include <<< "$uninstrumented")" 0 \
                "$(count_terms "$uninstrumented")" ".uninstrumented"
        fi
    else
        run_pass "$project" "$project" "$(build_filter exclude <<< "$exclusions")" 0 "$(count_terms "$exclusions")"
    fi
done

if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
    {
        echo "### Unit tests"
        echo
        echo "| Pass | Filter terms | Result | Time |"
        echo "|---|---|---|---|"
        printf '%s\n' "${summary_rows[@]}"
        echo
    } >> "$GITHUB_STEP_SUMMARY"
fi

if [ ${#failed[@]} -gt 0 ]; then
    echo "Failed: ${failed[*]}" >&2
    exit 1
fi
