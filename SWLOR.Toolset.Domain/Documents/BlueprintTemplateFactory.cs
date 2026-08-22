using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Builds the file content of a brand-new blueprint of a given type, for the palette's
    /// "New Creature…/New Placeable…/…" actions. The app layer writes the returned bytes to
    /// Module\&lt;ext&gt;\&lt;resref&gt;.&lt;ext&gt;.json and opens the result in the blueprint editor;
    /// picking the resref and validating it are the caller's job.
    /// </summary>
    /// <remarks>
    /// The field list per type is the module corpus's de-facto required set: every field that every
    /// real blueprint of that type carries (a superset of what the editor schemas declare - the
    /// schemas deliberately omit fields no one edits by hand, but a file missing them is not a
    /// blueprint the game or the editors can be trusted with). Values default to 0 / empty except
    /// where the corpus is overwhelmingly consistent about something else (placeable HP 10,
    /// creature DecayTime 5000, store MarkUp 100, …) or where 0 would be structurally wrong
    /// (a 0-HP creature is dead on spawn, a 0-size stack cannot exist), which is why the numbers
    /// below are annotated rather than bare.
    /// </remarks>
    public static class BlueprintTemplateFactory
    {
        /// <summary>The corpus "__struct_id" for a creature ClassList entry.</summary>
        private const uint ClassStructId = 2;

        /// <summary>How many entries a creature's SkillList always has (one per skills.2da row).</summary>
        private const int CreatureSkillCount = 28;

        /// <summary>How many panels a store's StoreList always has; the panel is its __struct_id.</summary>
        private const int StorePanelCount = 5;

        public static bool Supports(ResourceType type)
        {
            return type is ResourceType.Utc or ResourceType.Uti or ResourceType.Utp or ResourceType.Utd
                or ResourceType.Utm or ResourceType.Utt or ResourceType.Uts or ResourceType.Utw;
        }

        public static byte[] CreateFileContent(ResourceType type, string resRef, string displayName)
        {
            if (!Supports(type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type), type, "There is no blueprint template for this resource type.");
            }

            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            // A brand-new blueprint is on nobody's undo stack, but the guard is ambient per call
            // context, so with an editor open every field added below would otherwise throw.
            using var construction = Editing.EditScope.EnterConstruction();

            var document = CreateDocument(type);
            var root = document.Root;
            var name = string.IsNullOrWhiteSpace(displayName) ? resRef : displayName;

            switch (type)
            {
                case ResourceType.Utc:
                    PopulateCreature(root, resRef, name);
                    break;
                case ResourceType.Uti:
                    PopulateItem(root, resRef, name);
                    break;
                case ResourceType.Utp:
                    PopulatePlaceable(root, resRef, name);
                    break;
                case ResourceType.Utd:
                    PopulateDoor(root, resRef, name);
                    break;
                case ResourceType.Utm:
                    PopulateStore(root, resRef, name);
                    break;
                case ResourceType.Utt:
                    PopulateTrigger(root, resRef, name);
                    break;
                case ResourceType.Uts:
                    PopulateSound(root, resRef, name);
                    break;
                case ResourceType.Utw:
                    PopulateWaypoint(root, resRef, name);
                    break;
                default:
                    // Reachable only if a type is added to Supports without a template below.
                    throw new InvalidOperationException($"No blueprint template is implemented for {type}.");
            }

            return document.ToBytes();
        }

        private static JsonGffDocument CreateDocument(ResourceType type)
        {
            // nwn_gff data types are four characters, so the three-letter extensions are padded.
            // The unpack pipeline writes an LF body terminated by a single CRLF; matching that keeps
            // a new blueprint byte-shaped like every other file in Module\.
            return new JsonGffDocument($"{type.Extension().ToUpperInvariant()} ", new JsonGffStruct())
            {
                UsesCrLf = false,
                HasTrailingNewline = true,
                TrailingNewlineUsesCrLf = true
            };
        }

        private static void PopulateCreature(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "TemplateResRef", resRef);
            root.GetOrAddLocString("FirstName").Text = name;
            AddEmptyLocString(root, "LastName");
            AddEmptyLocString(root, "Description");
            SetEmptyResRefs(root, "Conversation");

            // A blank creature is a level 1 human fighter: class, starting package, race and
            // appearance have to agree with each other, and 0 is a valid row in none of them
            // except race/appearance by coincidence.
            var classList = root.GetOrAddList("ClassList");
            var classEntry = JsonGffField.CreateStruct(ClassStructId).Struct!;
            classEntry.SetInt("Class", GffFieldType.Int, 4);
            classEntry.SetInt("ClassLevel", GffFieldType.Short, 1);
            classList.Add(classEntry);
            root.SetInt("StartingPackage", GffFieldType.Byte, 4);
            root.SetInt("Race", GffFieldType.Byte, 6);
            root.SetInt("Appearance_Type", GffFieldType.Word, 6);
            CreatureAppearanceDefaults.ApplyGenericSegmentedBody(root);

            // The engine expects one rank entry per skills.2da row, always present.
            var skillList = root.GetOrAddList("SkillList");
            for (var i = 0; i < CreatureSkillCount; i++)
            {
                var skill = JsonGffField.CreateStruct(0).Struct!;
                skill.SetInt("Rank", GffFieldType.Byte, 0);
                skillList.Add(skill);
            }

            AddEmptyLists(root, "FeatList", "Equip_ItemList", "SpecAbilityList", "TemplateList");

            // Ability scores of 0 would give -5 to everything; 10 is the neutral baseline.
            foreach (var ability in new[] { "Str", "Dex", "Con", "Int", "Wis", "Cha" })
                root.SetInt(ability, GffFieldType.Byte, 10);

            // 0 HP is a corpse, so a new creature starts on 1 across all three HP fields.
            root.SetInt("HitPoints", GffFieldType.Short, 1);
            root.SetInt("CurrentHitPoints", GffFieldType.Short, 1);
            root.SetInt("MaxHitPoints", GffFieldType.Short, 1);
            root.SetInt("NaturalAC", GffFieldType.Byte, 0);
            root.SetSingle("ChallengeRating", 1f);
            root.SetInt("CRAdjust", GffFieldType.Int, 0);
            root.SetInt("fortbonus", GffFieldType.Short, 0);
            root.SetInt("refbonus", GffFieldType.Short, 0);
            root.SetInt("willbonus", GffFieldType.Short, 0);

            root.SetInt("FactionID", GffFieldType.Word, 1);
            root.SetInt("Gender", GffFieldType.Byte, 0);
            root.SetInt("Phenotype", GffFieldType.Int, 0);
            root.SetInt("PortraitId", GffFieldType.Word, 0);
            root.SetInt("SoundSetFile", GffFieldType.Word, 0);
            root.SetString("Deity", GffFieldType.CExoString, string.Empty);
            root.SetString("Subrace", GffFieldType.CExoString, string.Empty);
            root.SetUInt("Tail_New", GffFieldType.Dword, 0);
            root.SetUInt("Wings_New", GffFieldType.Dword, 0);

            root.SetInt("GoodEvil", GffFieldType.Byte, 50); // true neutral alignment
            root.SetInt("LawfulChaotic", GffFieldType.Byte, 50);
            root.SetUInt("DecayTime", GffFieldType.Dword, 5000); // engine default corpse decay, in ms
            root.SetInt("PerceptionRange", GffFieldType.Byte, 11); // "default" row of ranges.2da
            root.SetInt("WalkRate", GffFieldType.Int, 7); // "normal" row of creaturespeed.2da
            root.SetInt("Interruptable", GffFieldType.Byte, 1);
            root.SetInt("BodyBag", GffFieldType.Byte, 0);
            root.SetInt("IsPC", GffFieldType.Byte, 0);
            root.SetInt("Disarmable", GffFieldType.Byte, 0);
            root.SetInt("IsImmortal", GffFieldType.Byte, 0);
            root.SetInt("Lootable", GffFieldType.Byte, 0);
            root.SetInt("NoPermDeath", GffFieldType.Byte, 0);
            root.SetInt("Plot", GffFieldType.Byte, 0);
            root.SetInt("PaletteID", GffFieldType.Byte, 0);

            // SWLOR drives creature behavior from C# event handlers, so the NWScript slots stay empty.
            SetEmptyResRefs(root,
                "ScriptAttacked", "ScriptDamaged", "ScriptDeath", "ScriptDialogue", "ScriptDisturbed",
                "ScriptEndRound", "ScriptHeartbeat", "ScriptOnBlocked", "ScriptOnNotice", "ScriptRested",
                "ScriptSpawn", "ScriptSpellAt", "ScriptUserDefine");
        }

        private static void PopulateItem(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "TemplateResRef", resRef);
            root.GetOrAddLocString("LocalizedName").Text = name;
            AddEmptyLocString(root, "Description");
            AddEmptyLocString(root, "DescIdentified");
            AddEmptyLists(root, "PropertiesList");

            root.SetInt("BaseItem", GffFieldType.Int, 0);

            // Base item 0 is the short sword, whose baseitems.2da ModelType is 2 - a composite weapon
            // built from a bottom, middle and top part. Those fields were never written, so they read
            // back as part 0, which has no model at all, and UtiSchema exposes only the base-item
            // selector - so a new item had no appearance and no way to be given one. 1 is the first
            // real variant of each part, the same reasoning as the dynamic-human default above.
            root.SetInt("ModelPart1", GffFieldType.Byte, 1);
            root.SetInt("ModelPart2", GffFieldType.Byte, 1);
            root.SetInt("ModelPart3", GffFieldType.Byte, 1);

            root.SetInt("StackSize", GffFieldType.Word, 1); // a 0-size stack is not a thing
            root.SetInt("Charges", GffFieldType.Byte, 0);
            root.SetUInt("Cost", GffFieldType.Dword, 0);
            root.SetUInt("AddCost", GffFieldType.Dword, 0);
            root.SetInt("Identified", GffFieldType.Byte, 1); // unidentified hides the item's own name
            root.SetInt("Plot", GffFieldType.Byte, 0);
            root.SetInt("Stolen", GffFieldType.Byte, 0);
            root.SetInt("Cursed", GffFieldType.Byte, 0);
            root.SetInt("PaletteID", GffFieldType.Byte, 0);

            // A brand-new item has no loot table, recipe, store or any other way for a player to get it.
            // Item.IsEconomyRestricted treats an unflagged item as searchable, so without this it would
            // appear on player-facing economy surfaces as something nobody can obtain. Clear the flag
            // once a real source is wired - see the economy rules in AGENTS.md.
            new VarTable(root).SetInt(NoEconomyVariable, 1);
        }

        /// <summary>Local variable marking an item as outside the player economy.</summary>
        public const string NoEconomyVariable = "NO_ECONOMY";

        private static void PopulatePlaceable(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "TemplateResRef", resRef);
            root.GetOrAddLocString("LocName").Text = name;
            AddEmptyLocString(root, "Description");

            root.SetUInt("Appearance", GffFieldType.Dword, 0);
            root.SetUInt("Faction", GffFieldType.Dword, 3); // the corpus placeable faction
            root.SetInt("AnimationState", GffFieldType.Byte, 0);
            root.SetInt("Type", GffFieldType.Byte, 0);
            root.SetInt("Useable", GffFieldType.Byte, 0);
            root.SetInt("Static", GffFieldType.Byte, 0);
            root.SetInt("Plot", GffFieldType.Byte, 0);
            root.SetInt("HasInventory", GffFieldType.Byte, 0);
            root.SetInt("Interruptable", GffFieldType.Byte, 1);
            root.SetInt("BodyBag", GffFieldType.Byte, 0);
            root.SetInt("PortraitId", GffFieldType.Word, 0);
            root.SetInt("PaletteID", GffFieldType.Byte, 0);

            SetLock(root, lockable: 1);
            SetSavesAndDurability(root, hitPoints: 10, hardness: 5, fortitude: 5);
            SetTrap(root);
            SetEmptyResRefs(root,
                "Conversation", "OnUsed", "OnClick", "OnClosed", "OnDamaged", "OnDeath", "OnDisarm",
                "OnHeartbeat", "OnInvDisturbed", "OnLock", "OnMeleeAttacked", "OnOpen", "OnSpellCastAt",
                "OnTrapTriggered", "OnUnlock", "OnUserDefined");
        }

        private static void PopulateDoor(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "TemplateResRef", resRef);
            root.GetOrAddLocString("LocName").Text = name;
            AddEmptyLocString(root, "Description");

            // Doors carry both fields: Appearance is unused (0 everywhere in the corpus) and
            // GenericType_New is the genericdoors.2da row; Appearance selects doortypes.2da.
            root.SetUInt("Appearance", GffFieldType.Dword, 0);
            root.SetUInt("GenericType_New", GffFieldType.Dword, 0);
            root.SetUInt("Faction", GffFieldType.Dword, 1);
            root.SetInt("AnimationState", GffFieldType.Byte, 0);
            root.SetInt("Plot", GffFieldType.Byte, 0);
            root.SetInt("Interruptable", GffFieldType.Byte, 1);
            root.SetInt("PortraitId", GffFieldType.Word, 558); // the generic door portrait
            root.SetInt("PaletteID", GffFieldType.Byte, 0);
            root.SetString("LinkedTo", GffFieldType.CExoString, string.Empty);
            root.SetInt("LinkedToFlags", GffFieldType.Byte, 0);
            root.SetInt("LoadScreenID", GffFieldType.Word, 0);

            SetLock(root, lockable: 1);
            SetSavesAndDurability(root, hitPoints: 10, hardness: 5, fortitude: 5);
            SetTrap(root);
            SetEmptyResRefs(root,
                "Conversation", "OnClick", "OnClosed", "OnDamaged", "OnDeath", "OnDisarm", "OnFailToOpen",
                "OnHeartbeat", "OnLock", "OnMeleeAttacked", "OnOpen", "OnSpellCastAt", "OnTrapTriggered",
                "OnUnlock", "OnUserDefined");
        }

        private static void PopulateStore(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "ResRef", resRef); // stores are the one type that uses "ResRef"
            root.GetOrAddLocString("LocName").Text = name;

            // Every corpus store carries all five panels; a panel with nothing for sale is an
            // empty struct whose __struct_id is the panel it fills.
            var storeList = root.GetOrAddList("StoreList");
            for (var panel = 0u; panel < StorePanelCount; panel++)
                storeList.Add(JsonGffField.CreateStruct(panel).Struct!);

            AddEmptyLists(root, "WillNotBuy", "WillOnlyBuy");

            // The engine's blank-store pricing, which the whole corpus agrees on.
            root.SetInt("MarkUp", GffFieldType.Int, 100);
            root.SetInt("MarkDown", GffFieldType.Int, 40);
            root.SetInt("BM_MarkDown", GffFieldType.Int, 25);
            // SWLOR always identifies merchandise for free, accepts the engine's stolen flag, and
            // has no per-store spending limit. The dedicated editor keeps these policy fields out
            // of the form and reasserts them on every save.
            root.SetInt("IdentifyPrice", GffFieldType.Int, 0);
            root.SetInt("MaxBuyPrice", GffFieldType.Int, -1); // -1 = no cap
            root.SetInt("StoreGold", GffFieldType.Int, -1); // -1 = unlimited gold
            root.SetInt("BlackMarket", GffFieldType.Byte, 1);
            root.SetInt("ID", GffFieldType.Byte, 5); // Merchants in storepalcus.itp

            root.SetString("OnOpenStore", GffFieldType.ResRef, "on_open_store");
            root.SetString("OnStoreClosed", GffFieldType.ResRef, "on_close_store");
        }

        private static void PopulateTrigger(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "TemplateResRef", resRef);
            root.GetOrAddLocString("LocalizedName").Text = name;

            // Trigger geometry lives on the area instance, not on the blueprint.
            root.SetInt("Type", GffFieldType.Int, 0); // generic trigger
            root.SetUInt("Faction", GffFieldType.Dword, 1);
            root.SetInt("Cursor", GffFieldType.Byte, 0);
            root.SetSingle("HighlightHeight", 0f);
            root.SetString("LinkedTo", GffFieldType.CExoString, string.Empty);
            root.SetInt("LinkedToFlags", GffFieldType.Byte, 0);
            root.SetInt("LoadScreenID", GffFieldType.Word, 0);
            root.SetInt("PortraitId", GffFieldType.Word, 0);
            root.SetInt("PaletteID", GffFieldType.Byte, 0);
            root.SetString("KeyName", GffFieldType.CExoString, string.Empty);
            root.SetInt("AutoRemoveKey", GffFieldType.Byte, 0);

            SetTrap(root);
            SetEmptyResRefs(root,
                "ScriptOnEnter", "ScriptOnExit", "ScriptHeartbeat", "ScriptUserDefine",
                "OnClick", "OnDisarm", "OnTrapTriggered");
        }

        private static void PopulateSound(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "TemplateResRef", resRef);
            root.GetOrAddLocString("LocName").Text = name;
            AddEmptyLists(root, "Sounds");

            // A continuous, looping, non-positional ambience: the shape a new sound object is
            // almost always given, and the one that needs no interval or position tuning.
            root.SetInt("Active", GffFieldType.Byte, 1);
            root.SetInt("Continuous", GffFieldType.Byte, 1);
            root.SetInt("Looping", GffFieldType.Byte, 1);
            root.SetInt("Positional", GffFieldType.Byte, 0);
            root.SetInt("Random", GffFieldType.Byte, 0);
            root.SetInt("RandomPosition", GffFieldType.Byte, 0);
            root.SetSingle("RandomRangeX", 0f);
            root.SetSingle("RandomRangeY", 0f);
            root.SetUInt("Interval", GffFieldType.Dword, 0);
            root.SetUInt("IntervalVrtn", GffFieldType.Dword, 0);
            root.SetUInt("Hours", GffFieldType.Dword, 0);
            root.SetInt("Times", GffFieldType.Byte, 3); // audible both day and night
            root.SetInt("Volume", GffFieldType.Byte, 127); // full volume
            root.SetInt("VolumeVrtn", GffFieldType.Byte, 0);
            root.SetInt("Priority", GffFieldType.Byte, 0);
            root.SetSingle("PitchVariation", 0f);
            root.SetSingle("Elevation", 1f);
            root.SetSingle("MinDistance", 1f);
            root.SetSingle("MaxDistance", 10f);
            root.SetInt("PaletteID", GffFieldType.Byte, 0);
        }

        private static void PopulateWaypoint(JsonGffStruct root, string resRef, string name)
        {
            SetIdentity(root, "TemplateResRef", resRef);
            root.GetOrAddLocString("LocalizedName").Text = name;
            AddEmptyLocString(root, "Description");
            AddEmptyLocString(root, "MapNote");

            root.SetInt("Appearance", GffFieldType.Byte, 2); // the plain waypoint marker
            root.SetInt("HasMapNote", GffFieldType.Byte, 0);
            root.SetInt("MapNoteEnabled", GffFieldType.Byte, 0);
            root.SetString("LinkedTo", GffFieldType.CExoString, string.Empty);
            root.SetInt("PaletteID", GffFieldType.Byte, 0);
        }

        /// <summary>
        /// The identity fields every blueprint type shares. Tag defaults to the resref because that
        /// is how the corpus blueprints and the game's tag lookups are wired.
        /// </summary>
        private static void SetIdentity(JsonGffStruct root, string resRefFieldName, string resRef)
        {
            root.SetString(resRefFieldName, GffFieldType.ResRef, resRef);
            root.SetString("Tag", GffFieldType.CExoString, resRef);
            root.SetString("Comment", GffFieldType.CExoString, string.Empty);
        }

        /// <summary>Lock state shared by placeables and doors: unlocked, with no key requirement.</summary>
        private static void SetLock(JsonGffStruct root, int lockable)
        {
            root.SetInt("Locked", GffFieldType.Byte, 0);
            root.SetInt("Lockable", GffFieldType.Byte, lockable);
            root.SetInt("OpenLockDC", GffFieldType.Byte, 0);
            root.SetInt("CloseLockDC", GffFieldType.Byte, 0);
            root.SetInt("KeyRequired", GffFieldType.Byte, 0);
            root.SetString("KeyName", GffFieldType.CExoString, string.Empty);
            root.SetInt("AutoRemoveKey", GffFieldType.Byte, 0);
        }

        /// <summary>Durability and saves shared by placeables and doors.</summary>
        private static void SetSavesAndDurability(JsonGffStruct root, int hitPoints, int hardness, int fortitude)
        {
            root.SetInt("HP", GffFieldType.Short, hitPoints);
            root.SetInt("CurrentHP", GffFieldType.Short, hitPoints);
            root.SetInt("Hardness", GffFieldType.Byte, hardness);
            root.SetInt("Fort", GffFieldType.Byte, fortitude);
            root.SetInt("Ref", GffFieldType.Byte, 0);
            root.SetInt("Will", GffFieldType.Byte, 0);
        }

        /// <summary>
        /// The trap block placeables, doors and triggers all carry. Untrapped, but detectable and
        /// disarmable once a trap type is set - the corpus is unanimous on those three flags.
        /// </summary>
        private static void SetTrap(JsonGffStruct root)
        {
            root.SetInt("TrapFlag", GffFieldType.Byte, 0);
            root.SetInt("TrapType", GffFieldType.Byte, 0);
            root.SetInt("TrapDetectable", GffFieldType.Byte, 1);
            root.SetInt("TrapDetectDC", GffFieldType.Byte, 0);
            root.SetInt("TrapDisarmable", GffFieldType.Byte, 1);
            root.SetInt("DisarmDC", GffFieldType.Byte, 0);
            root.SetInt("TrapOneShot", GffFieldType.Byte, 1);
        }

        private static void SetEmptyResRefs(JsonGffStruct root, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
                root.SetString(fieldName, GffFieldType.ResRef, string.Empty);
        }

        private static void AddEmptyLists(JsonGffStruct root, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
                root.GetOrAddList(fieldName);
        }

        /// <summary>
        /// Adds a cexolocstring with no language entries, which is how the corpus stores a
        /// blueprint's unset localized text.
        /// </summary>
        private static void AddEmptyLocString(JsonGffStruct root, string fieldName)
        {
            root.GetOrAddLocString(fieldName);
        }
    }
}
