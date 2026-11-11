using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Incubation field notes are key items that document how to produce a mutated beast.
    /// There is one note per mutation target beast; the note aggregates every incubation
    /// method that yields that target. Notes are always discoverable by performing the
    /// mutation, and may additionally be sold or dropped depending on their acquisition type.
    /// The logic and state that used to be static in <c>Service.IncubationFieldNote</c>, moved here unchanged. The
    /// event handler runs on the container's instance; static callers reach it through <c>GameServices.IncubationFieldNote</c>.
    /// </summary>
    public class IncubationFieldNoteService : IIncubationFieldNoteService
    {
        private readonly record struct ProductionMethod(BeastType Source, List<IMutationRequirement> Requirements);

        private readonly Dictionary<BeastType, FieldNoteDetail> _notesByTarget = new();
        private readonly Dictionary<KeyItemType, FieldNoteDetail> _notesByKeyItem = new();
        private bool _registered;

        private readonly IKeyItemService _keyItem;
        private readonly Lazy<IBeastMasteryService> _beastMastery;

        public IncubationFieldNoteService(IKeyItemService keyItem, Lazy<IBeastMasteryService> beastMastery)
        {
            _keyItem = keyItem;
            _beastMastery = beastMastery;
        }

        private void Register(BeastType target, KeyItemType note, FieldNoteAcquisitionType acquisition)
        {
            var detail = new FieldNoteDetail(target, note, acquisition);
            _notesByTarget[target] = detail;
            _notesByKeyItem[note] = detail;
        }

        public void EnsureRegistered()
        {
            if (_registered)
                return;

            RegisterAll();
            _registered = true;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void LoadDisplayText()
        {
            EnsureRegistered();

            var methodsByTarget = BuildProductionIndex();
            var populated = 0;

            foreach (var (target, detail) in _notesByTarget)
            {
                try
                {
                    var attribute = _keyItem.GetKeyItem(detail.Note);
                    var targetName = GetBeastDetail(target).Name;

                    methodsByTarget.TryGetValue(target, out var methods);

                    attribute.Name = targetName;
                    attribute.Description = BuildDescription(targetName, methods ?? new List<ProductionMethod>());
                    populated++;
                }
                catch (Exception ex)
                {
                    WriteLog(LogGroup.Incubation,
                        $"Failed to populate incubation field note for beast '{target}' (key item '{detail.Note}'): {ex.Message}",
                        true);
                }
            }

            WriteLog(LogGroup.Incubation, $"Populated {populated}/{_notesByTarget.Count} incubation field notes.");
        }

        public void GrantDiscoveredNote(uint player, BeastType producedBeast)
        {
            EnsureRegistered();

            if (_notesByTarget.TryGetValue(producedBeast, out var detail))
            {
                _keyItem.GiveKeyItem(player, detail.Note);
            }
        }

        public IReadOnlyCollection<FieldNoteDetail> GetAllNotes()
        {
            EnsureRegistered();
            return _notesByTarget.Values.ToList();
        }

        public bool TryGetNoteForTarget(BeastType target, out FieldNoteDetail detail)
        {
            EnsureRegistered();
            return _notesByTarget.TryGetValue(target, out detail);
        }

        public bool TryGetNoteForKeyItem(KeyItemType keyItem, out FieldNoteDetail detail)
        {
            EnsureRegistered();
            return _notesByKeyItem.TryGetValue(keyItem, out detail);
        }

        private Dictionary<BeastType, List<ProductionMethod>> BuildProductionIndex()
        {
            var index = new Dictionary<BeastType, List<ProductionMethod>>();

            foreach (var beastType in GetAllBeastTypes())
            {
                var beast = GetBeastDetail(beastType);
                foreach (var mutation in beast.PossibleMutations)
                {
                    if (!index.TryGetValue(mutation.Type, out var methods))
                    {
                        methods = new List<ProductionMethod>();
                        index[mutation.Type] = methods;
                    }

                    methods.Add(new ProductionMethod(beastType, mutation.Requirements));
                }
            }

            return index;
        }

        private string BuildDescription(string targetName, List<ProductionMethod> methods)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Field research on how to incubate a {targetName}.");
            sb.AppendLine();

            if (methods.Count == 0)
            {
                sb.Append("No known incubation method.");
                return sb.ToString();
            }

            sb.AppendLine(methods.Count == 1
                ? "Known incubation method:"
                : $"Known incubation methods ({methods.Count}):");

            foreach (var method in methods.OrderBy(m => GetBeastDetail(m.Source).Name))
            {
                var sourceName = GetBeastDetail(method.Source).Name;
                sb.AppendLine($"From {sourceName}:");
                foreach (var requirement in DescribeRequirements(method.Requirements))
                {
                    sb.AppendLine($"- {requirement}");
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private List<string> DescribeRequirements(List<IMutationRequirement> requirements)
        {
            if (requirements == null || requirements.Count == 0)
                return new List<string> { "No special requirements" };

            var parts = new List<string>();
            foreach (var requirement in requirements)
            {
                if (requirement is MutationRequirementEnzyme enzymeRequirement)
                {
                    parts.AddRange(enzymeRequirement.GetEnzymeDescriptions());
                    continue;
                }

                var description = requirement.GetRequirementDescription();
                if (!string.IsNullOrWhiteSpace(description))
                    parts.Add(description);
            }

            return parts.Count == 0
                ? new List<string> { "No special requirements" }
                : parts;
        }

        /// <summary>
        /// Every beast type the beast catalog has loaded. The original call into <c>BeastMastery</c>, on its own so a
        /// unit test can answer it. It goes through the static facade, not an injected <c>IBeastMasteryService</c>:
        /// the beast service takes this one (it grants the field note of a mutation), so injecting it both ways would
        /// be a construction cycle.
        /// </summary>
        protected virtual IEnumerable<BeastType> GetAllBeastTypes()
        {
            return _beastMastery.Value.GetAllBeastTypes();
        }

        /// <summary>
        /// The catalog entry of one beast. The original call into <c>BeastMastery</c>, on its own for the same reason as
        /// <see cref="GetAllBeastTypes"/>.
        /// </summary>
        protected virtual BeastDetail GetBeastDetail(BeastType type)
        {
            return _beastMastery.Value.GetBeastDetail(type);
        }

        /// <summary>
        /// Writes one line to the log. The original call into <c>Log</c> (which is not converted yet and writes to the
        /// configured log files), on its own so a unit test can read what was logged.
        /// </summary>
        protected virtual void WriteLog(LogGroup group, string details, bool printToConsole = false)
        {
            Log.Write(group, details, printToConsole);
        }

        // Hand-declared registry: one note per mutation target beast. Store notes are sold by the field note
        // vendor, BossDrop notes drop from bosses, RareSpawnDrop notes drop from rare spawn elites. Guarded by
        // IncubationFieldNoteTests.
        private void RegisterAll()
        {
            Register(BeastType.Aardvark, KeyItemType.IncubationFieldNoteAardvark, FieldNoteAcquisitionType.Store);
            Register(BeastType.AbysswebRavager, KeyItemType.IncubationFieldNoteAbysswebRavager, FieldNoteAcquisitionType.BossDrop);
            Register(BeastType.Allosaurus, KeyItemType.IncubationFieldNoteAllosaurus, FieldNoteAcquisitionType.Store);
            Register(BeastType.AmberhideNimbrel, KeyItemType.IncubationFieldNoteAmberhideNimbrel, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.AmethystSelori, KeyItemType.IncubationFieldNoteAmethystSelori, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.ArmourbackSpineguard, KeyItemType.IncubationFieldNoteArmourbackSpineguard, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.AshenMoonprowler, KeyItemType.IncubationFieldNoteAshenMoonprowler, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.AzurehornKargath, KeyItemType.IncubationFieldNoteAzurehornKargath, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.BalanoroForceMite, KeyItemType.IncubationFieldNoteBalanoroForceMite, FieldNoteAcquisitionType.Store);
            Register(BeastType.BasaltGorgath, KeyItemType.IncubationFieldNoteBasaltGorgath, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Bearbug, KeyItemType.IncubationFieldNoteBearbug, FieldNoteAcquisitionType.Store);
            Register(BeastType.Bhalir, KeyItemType.IncubationFieldNoteBhalir, FieldNoteAcquisitionType.Store);
            Register(BeastType.BinarianSabercat, KeyItemType.IncubationFieldNoteBinarianSabercat, FieldNoteAcquisitionType.Store);
            Register(BeastType.Blastail, KeyItemType.IncubationFieldNoteBlastail, FieldNoteAcquisitionType.Store);
            Register(BeastType.BlinkstepVekara, KeyItemType.IncubationFieldNoteBlinkstepVekara, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.BlisteringBeetle, KeyItemType.IncubationFieldNoteBlisteringBeetle, FieldNoteAcquisitionType.Store);
            Register(BeastType.BloodtuskRavor, KeyItemType.IncubationFieldNoteBloodtuskRavor, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.BomaBeast, KeyItemType.IncubationFieldNoteBomaBeast, FieldNoteAcquisitionType.Store);
            Register(BeastType.BomaBeastBaby, KeyItemType.IncubationFieldNoteBomaBeastBaby, FieldNoteAcquisitionType.Store);
            Register(BeastType.BrambleLynx, KeyItemType.IncubationFieldNoteBrambleLynx, FieldNoteAcquisitionType.Store);
            Register(BeastType.BrassjawPyralisk, KeyItemType.IncubationFieldNoteBrassjawPyralisk, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.BronzecrestThundros, KeyItemType.IncubationFieldNoteBronzecrestThundros, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.BurrowberryBird, KeyItemType.IncubationFieldNoteBurrowberryBird, FieldNoteAcquisitionType.Store);
            Register(BeastType.BurrowberryPack, KeyItemType.IncubationFieldNoteBurrowberryPack, FieldNoteAcquisitionType.Store);
            Register(BeastType.Cannok, KeyItemType.IncubationFieldNoteCannok, FieldNoteAcquisitionType.Store);
            Register(BeastType.CharHound, KeyItemType.IncubationFieldNoteCharHound, FieldNoteAcquisitionType.Store);
            Register(BeastType.CloudcallAurelith, KeyItemType.IncubationFieldNoteCloudcallAurelith, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.CobaltHornwyrm, KeyItemType.IncubationFieldNoteCobaltHornwyrm, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.CoppercoilMirelisk, KeyItemType.IncubationFieldNoteCoppercoilMirelisk, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.CragmaneValshar, KeyItemType.IncubationFieldNoteCragmaneValshar, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Cragscale, KeyItemType.IncubationFieldNoteCragscale, FieldNoteAcquisitionType.Store);
            Register(BeastType.CrimsonSkyrender, KeyItemType.IncubationFieldNoteCrimsonSkyrender, FieldNoteAcquisitionType.BossDrop);
            Register(BeastType.Crocodile, KeyItemType.IncubationFieldNoteCrocodile, FieldNoteAcquisitionType.Store);
            Register(BeastType.CrystalflowSkimmer, KeyItemType.IncubationFieldNoteCrystalflowSkimmer, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.DathomirWyrmling, KeyItemType.IncubationFieldNoteDathomirWyrmling, FieldNoteAcquisitionType.Store);
            Register(BeastType.DawnfangHound, KeyItemType.IncubationFieldNoteDawnfangHound, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.DeeprockMauler, KeyItemType.IncubationFieldNoteDeeprockMauler, FieldNoteAcquisitionType.Store);
            Register(BeastType.DeepstoneGraxal, KeyItemType.IncubationFieldNoteDeepstoneGraxal, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.DeepwoodsRager, KeyItemType.IncubationFieldNoteDeepwoodsRager, FieldNoteAcquisitionType.Store);
            Register(BeastType.Dewback, KeyItemType.IncubationFieldNoteDewback, FieldNoteAcquisitionType.Store);
            Register(BeastType.DirefangLupikar, KeyItemType.IncubationFieldNoteDirefangLupikar, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.DreadmawBarghest, KeyItemType.IncubationFieldNoteDreadmawBarghest, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.DreamcapMyconite, KeyItemType.IncubationFieldNoteDreamcapMyconite, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Dreamwalker, KeyItemType.IncubationFieldNoteDreamwalker, FieldNoteAcquisitionType.Store);
            Register(BeastType.DrexclawMarauder, KeyItemType.IncubationFieldNoteDrexclawMarauder, FieldNoteAcquisitionType.BossDrop);
            Register(BeastType.DuneshagBantha, KeyItemType.IncubationFieldNoteDuneshagBantha, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.DuskfangHound, KeyItemType.IncubationFieldNoteDuskfangHound, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.DuskmaneUrsadon, KeyItemType.IncubationFieldNoteDuskmaneUrsadon, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.EldersporeOraculum, KeyItemType.IncubationFieldNoteEldersporeOraculum, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.EmberbackBristal, KeyItemType.IncubationFieldNoteEmberbackBristal, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.EmeraldcrestKalyth, KeyItemType.IncubationFieldNoteEmeraldcrestKalyth, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Frog, KeyItemType.IncubationFieldNoteFrog, FieldNoteAcquisitionType.Store);
            Register(BeastType.FrostbackSpineguard, KeyItemType.IncubationFieldNoteFrostbackSpineguard, FieldNoteAcquisitionType.BossDrop);
            Register(BeastType.FrostmawGlacieron, KeyItemType.IncubationFieldNoteFrostmawGlacieron, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.FungalShambler, KeyItemType.IncubationFieldNoteFungalShambler, FieldNoteAcquisitionType.Store);
            Register(BeastType.Garral, KeyItemType.IncubationFieldNoteGarral, FieldNoteAcquisitionType.Store);
            Register(BeastType.GaruBearRipper, KeyItemType.IncubationFieldNoteGaruBearRipper, FieldNoteAcquisitionType.Store);
            Register(BeastType.GiantGaruBear, KeyItemType.IncubationFieldNoteGiantGaruBear, FieldNoteAcquisitionType.Store);
            Register(BeastType.GildedMirewyrm, KeyItemType.IncubationFieldNoteGildedMirewyrm, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.GlimmerwingMykal, KeyItemType.IncubationFieldNoteGlimmerwingMykal, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.GloomthreadSkiver, KeyItemType.IncubationFieldNoteGloomthreadSkiver, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.GoldpeltSahrak, KeyItemType.IncubationFieldNoteGoldpeltSahrak, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.GranitebackUrsavar, KeyItemType.IncubationFieldNoteGranitebackUrsavar, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.GraymireAmalgam, KeyItemType.IncubationFieldNoteGraymireAmalgam, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.GreenbulkWallow, KeyItemType.IncubationFieldNoteGreenbulkWallow, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Grutchin, KeyItemType.IncubationFieldNoteGrutchin, FieldNoteAcquisitionType.Store);
            Register(BeastType.Hanadak, KeyItemType.IncubationFieldNoteHanadak, FieldNoteAcquisitionType.Store);
            Register(BeastType.HornedKathHound, KeyItemType.IncubationFieldNoteHornedKathHound, FieldNoteAcquisitionType.Store);
            Register(BeastType.HouseCat, KeyItemType.IncubationFieldNoteHouseCat, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Hssiss, KeyItemType.IncubationFieldNoteHssiss, FieldNoteAcquisitionType.Store);
            Register(BeastType.HutlarPenguin, KeyItemType.IncubationFieldNoteHutlarPenguin, FieldNoteAcquisitionType.Store);
            Register(BeastType.IcewingKestrelith, KeyItemType.IncubationFieldNoteIcewingKestrelith, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.IronmawBastionback, KeyItemType.IncubationFieldNoteIronmawBastionback, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.JadeclawVyrkol, KeyItemType.IncubationFieldNoteJadeclawVyrkol, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.JuvenileChirodactyl, KeyItemType.IncubationFieldNoteJuvenileChirodactyl, FieldNoteAcquisitionType.Store);
            Register(BeastType.JuvenileRancor, KeyItemType.IncubationFieldNoteJuvenileRancor, FieldNoteAcquisitionType.Store);
            Register(BeastType.Katarn, KeyItemType.IncubationFieldNoteKatarn, FieldNoteAcquisitionType.Store);
            Register(BeastType.MoonthornVeloria, KeyItemType.IncubationFieldNoteMoonthornVeloria, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.MushWarrior, KeyItemType.IncubationFieldNoteMushWarrior, FieldNoteAcquisitionType.Store);
            Register(BeastType.MustardlashSlime, KeyItemType.IncubationFieldNoteMustardlashSlime, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.MutatedBoar, KeyItemType.IncubationFieldNoteMutatedBoar, FieldNoteAcquisitionType.Store);
            Register(BeastType.MutatedFrog, KeyItemType.IncubationFieldNoteMutatedFrog, FieldNoteAcquisitionType.Store);
            Register(BeastType.NightspotAralynx, KeyItemType.IncubationFieldNoteNightspotAralynx, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.OchreMaw, KeyItemType.IncubationFieldNoteOchreMaw, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.OrbakWaterHorse, KeyItemType.IncubationFieldNoteOrbakWaterHorse, FieldNoteAcquisitionType.Store);
            Register(BeastType.Orray, KeyItemType.IncubationFieldNoteOrray, FieldNoteAcquisitionType.Store);
            Register(BeastType.PhaselegSilkstalker, KeyItemType.IncubationFieldNotePhaselegSilkstalker, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Porg, KeyItemType.IncubationFieldNotePorg, FieldNoteAcquisitionType.Store);
            Register(BeastType.PyrestemScarab, KeyItemType.IncubationFieldNotePyrestemScarab, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.RazorhideHound, KeyItemType.IncubationFieldNoteRazorhideHound, FieldNoteAcquisitionType.Store);
            Register(BeastType.RedcrestTatterquill, KeyItemType.IncubationFieldNoteRedcrestTatterquill, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Ronto, KeyItemType.IncubationFieldNoteRonto, FieldNoteAcquisitionType.Store);
            Register(BeastType.RootboundColossus, KeyItemType.IncubationFieldNoteRootboundColossus, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.RoyalPlumage, KeyItemType.IncubationFieldNoteRoyalPlumage, FieldNoteAcquisitionType.Store);
            Register(BeastType.RubybackDrakon, KeyItemType.IncubationFieldNoteRubybackDrakon, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.RuinfangMongrel, KeyItemType.IncubationFieldNoteRuinfangMongrel, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.RustwhiskerGnawfiend, KeyItemType.IncubationFieldNoteRustwhiskerGnawfiend, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SaberlegKharaxis, KeyItemType.IncubationFieldNoteSaberlegKharaxis, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SapphireVeylori, KeyItemType.IncubationFieldNoteSapphireVeylori, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SapphirebackVorex, KeyItemType.IncubationFieldNoteSapphirebackVorex, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.ScrapRat, KeyItemType.IncubationFieldNoteScrapRat, FieldNoteAcquisitionType.Store);
            Register(BeastType.SereneGrovetreader, KeyItemType.IncubationFieldNoteSereneGrovetreader, FieldNoteAcquisitionType.Store);
            Register(BeastType.ShatterpeltLurax, KeyItemType.IncubationFieldNoteShatterpeltLurax, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SilverveilAerolith, KeyItemType.IncubationFieldNoteSilverveilAerolith, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SinkCrab, KeyItemType.IncubationFieldNoteSinkCrab, FieldNoteAcquisitionType.Store);
            Register(BeastType.SootbellyMirekit, KeyItemType.IncubationFieldNoteSootbellyMirekit, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SpeckledSeer, KeyItemType.IncubationFieldNoteSpeckledSeer, FieldNoteAcquisitionType.Store);
            Register(BeastType.SpinedCrawler, KeyItemType.IncubationFieldNoteSpinedCrawler, FieldNoteAcquisitionType.Store);
            Register(BeastType.Spinosaurus, KeyItemType.IncubationFieldNoteSpinosaurus, FieldNoteAcquisitionType.Store);
            Register(BeastType.Stegosaurus, KeyItemType.IncubationFieldNoteStegosaurus, FieldNoteAcquisitionType.Store);
            Register(BeastType.StingingSwarm, KeyItemType.IncubationFieldNoteStingingSwarm, FieldNoteAcquisitionType.Store);
            Register(BeastType.StonecladBehemoth, KeyItemType.IncubationFieldNoteStonecladBehemoth, FieldNoteAcquisitionType.Store);
            Register(BeastType.StrayfangKavor, KeyItemType.IncubationFieldNoteStrayfangKavor, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SumpbackChitinmaw, KeyItemType.IncubationFieldNoteSumpbackChitinmaw, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.SwampRat, KeyItemType.IncubationFieldNoteSwampRat, FieldNoteAcquisitionType.Store);
            Register(BeastType.Tach, KeyItemType.IncubationFieldNoteTach, FieldNoteAcquisitionType.Store);
            Register(BeastType.TempestBulwark, KeyItemType.IncubationFieldNoteTempestBulwark, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Terentatek, KeyItemType.IncubationFieldNoteTerentatek, FieldNoteAcquisitionType.Store);
            Register(BeastType.TideplumeStriderel, KeyItemType.IncubationFieldNoteTideplumeStriderel, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Torosaurus, KeyItemType.IncubationFieldNoteTorosaurus, FieldNoteAcquisitionType.Store);
            Register(BeastType.Triceratops, KeyItemType.IncubationFieldNoteTriceratops, FieldNoteAcquisitionType.Store);
            Register(BeastType.Tukata, KeyItemType.IncubationFieldNoteTukata, FieldNoteAcquisitionType.Store);
            Register(BeastType.TundraPonderer, KeyItemType.IncubationFieldNoteTundraPonderer, FieldNoteAcquisitionType.Store);
            Register(BeastType.Tyrannosaurus, KeyItemType.IncubationFieldNoteTyrannosaurus, FieldNoteAcquisitionType.Store);
            Register(BeastType.UbeseThorn, KeyItemType.IncubationFieldNoteUbeseThorn, FieldNoteAcquisitionType.Store);
            Register(BeastType.UmberrootArctara, KeyItemType.IncubationFieldNoteUmberrootArctara, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.UmbralBarghest, KeyItemType.IncubationFieldNoteUmbralBarghest, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.UmbratalonCorvax, KeyItemType.IncubationFieldNoteUmbratalonCorvax, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.UnderbrushScamp, KeyItemType.IncubationFieldNoteUnderbrushScamp, FieldNoteAcquisitionType.Store);
            Register(BeastType.UnderseaCarver, KeyItemType.IncubationFieldNoteUnderseaCarver, FieldNoteAcquisitionType.Store);
            Register(BeastType.VeilphaseArachnyx, KeyItemType.IncubationFieldNoteVeilphaseArachnyx, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.VenomspikeLaigrek, KeyItemType.IncubationFieldNoteVenomspikeLaigrek, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.VerdantThornwold, KeyItemType.IncubationFieldNoteVerdantThornwold, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.VermilionRavager, KeyItemType.IncubationFieldNoteVermilionRavager, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.ViridianPlatewyrm, KeyItemType.IncubationFieldNoteViridianPlatewyrm, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.VoidmireEcho, KeyItemType.IncubationFieldNoteVoidmireEcho, FieldNoteAcquisitionType.RareSpawnDrop);
            Register(BeastType.Weasel, KeyItemType.IncubationFieldNoteWeasel, FieldNoteAcquisitionType.Store);
            Register(BeastType.Wraid, KeyItemType.IncubationFieldNoteWraid, FieldNoteAcquisitionType.Store);
            Register(BeastType.WraithwebNythrax, KeyItemType.IncubationFieldNoteWraithwebNythrax, FieldNoteAcquisitionType.RareSpawnDrop);
        }
    }
}
