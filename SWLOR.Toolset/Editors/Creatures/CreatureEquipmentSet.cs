using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Loads and creates the item blueprints used by creature Stats and equipment.</summary>
    public sealed class CreatureEquipmentSet : IDisposable
    {
        private readonly CreatureValueStore _creature;
        private readonly string _itemDirectory;
        private readonly Dictionary<string, CreatureEquipmentDocument> _documents =
            new(StringComparer.OrdinalIgnoreCase);

        public CreatureEquipmentSet(CreatureValueStore creature, string creatureFilePath)
        {
            _creature = creature;
            var moduleRoot = Directory.GetParent(Path.GetDirectoryName(creatureFilePath)!)?.FullName
                             ?? throw new InvalidOperationException("The creature is not inside a module folder.");
            _itemDirectory = Path.Combine(moduleRoot, "uti");
        }

        public IReadOnlyCollection<CreatureEquipmentDocument> Documents => _documents.Values;

        public string? EquippedResRef(int slotId) => _creature.EquippedResRef(slotId);

        public void SetEquippedResRef(int slotId, string? resRef) =>
            _creature.SetEquippedResRef(slotId, resRef);

        public CreatureEquipmentDocument? ForSlot(int slotId)
        {
            var resRef = _creature.EquippedResRef(slotId);
            return string.IsNullOrWhiteSpace(resRef) ? null : Open(resRef);
        }

        public CreatureEquipmentDocument? Open(string resRef)
        {
            if (_documents.TryGetValue(resRef, out var known))
                return known;

            var path = Path.Combine(_itemDirectory, resRef + ".uti.json");
            if (!File.Exists(path))
                return null;

            var opened = new CreatureEquipmentDocument(resRef, false, DocumentSession.Open(path));
            _documents.Add(resRef, opened);
            return opened;
        }

        /// <summary>Creates and equips an economy-restricted stat item inside the caller's transaction.</summary>
        public CreatureEquipmentDocument Ensure(int slotId, int baseItem, string suffix, string displayName)
        {
            var existing = ForSlot(slotId);
            if (existing != null)
                return existing;

            var creatureResRef = _creature.GetString(
                Domain.Editors.Behaviors.BehaviorFieldStorage.Field,
                "TemplateResRef");
            var resRef = UniqueResRef(creatureResRef, suffix);
            var path = Path.Combine(_itemDirectory, resRef + ".uti.json");
            var document = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(ResourceType.Uti, resRef, displayName));
            var created = new CreatureEquipmentDocument(
                resRef,
                true,
                new DocumentSession(path, document));
            _documents.Add(resRef, created);

            var root = created.Session.Document.Root;
            root.SetInt("BaseItem", GffFieldType.Int, baseItem);
            root.SetString("Tag", GffFieldType.CExoString, resRef);
            root.GetOrAddLocString("LocalizedName").Text = displayName;
            _creature.SetEquippedResRef(slotId, resRef);
            return created;
        }

        public IReadOnlyList<CreatureEquipmentDocument> CurrentlyReferenced()
        {
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var slot in new[]
                     {
                         CreaturePropertyCatalog.MainWeaponSlot,
                         CreaturePropertyCatalog.OffWeaponSlot,
                         CreaturePropertyCatalog.CreatureWeaponSlot,
                         CreaturePropertyCatalog.StatSkinSlot
                     })
            {
                var resRef = _creature.EquippedResRef(slot);
                if (!string.IsNullOrWhiteSpace(resRef))
                    referenced.Add(resRef);
            }

            return referenced.Select(Open).Where(document => document != null).Cast<CreatureEquipmentDocument>().ToList();
        }

        /// <summary>
        /// Restores every opened linked item to its on-disk generation. Equipment mutations are
        /// captured by the creature's shared undo transaction, but when that undo history branches
        /// past its saved marker the creature session must reload from disk. The linked sessions
        /// need the same treatment or their live GFF graphs retain the abandoned branch.
        /// </summary>
        public void ReloadSavedDocuments()
        {
            foreach (var (resRef, document) in _documents.ToList())
            {
                if (document.IsNew)
                {
                    document.Dispose();
                    _documents.Remove(resRef);
                    continue;
                }

                document.Session.ReloadFromDisk();
            }
        }

        /// <summary>
        /// Linked documents that participate in the next creature save. Referenced items are always
        /// included, and an item edited before it was unlinked remains included so saving the creature
        /// cannot mark that entered data clean without putting it on disk.
        /// </summary>
        public IReadOnlyList<CreatureEquipmentDocument> SaveParticipants()
        {
            var participants = CurrentlyReferenced().ToList();
            var included = participants
                .Select(document => document.ResRef)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var document in _documents.Values)
            {
                if (document.HasUnsavedChanges && included.Add(document.ResRef))
                    participants.Add(document);
            }

            return participants;
        }

        private string UniqueResRef(string creatureResRef, string suffix)
        {
            var prefixLength = Math.Max(1, NwnResRef.MaxLength - suffix.Length);
            var stem = new string(creatureResRef
                    .ToLowerInvariant()
                    .Where(character => char.IsAsciiLetterOrDigit(character) || character == '_')
                    .Take(prefixLength)
                    .ToArray())
                .TrimEnd('_');
            if (stem.Length == 0)
                stem = "creature"[..Math.Min(8, prefixLength)];

            var candidate = (stem + suffix)[..Math.Min(NwnResRef.MaxLength, stem.Length + suffix.Length)];
            for (var counter = 2;
                 File.Exists(Path.Combine(_itemDirectory, candidate + ".uti.json")) || _documents.ContainsKey(candidate);
                 counter++)
            {
                var number = counter.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var allowedStem = Math.Max(1, NwnResRef.MaxLength - suffix.Length - number.Length);
                candidate = stem[..Math.Min(stem.Length, allowedStem)] + suffix + number;
            }

            return candidate;
        }

        public void Dispose()
        {
            foreach (var document in _documents.Values)
                document.Dispose();
            _documents.Clear();
        }
    }
}
