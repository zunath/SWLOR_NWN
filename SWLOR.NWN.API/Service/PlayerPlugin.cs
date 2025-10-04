using System;
using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWNX.Model;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class PlayerPlugin
    {
        private static IPlayerPluginService _service = new PlayerPluginService();

        internal static void SetService(IPlayerPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IPlayerPluginService.ForcePlaceableExamineWindow"/>
        public static void ForcePlaceableExamineWindow(uint player, uint placeable) => _service.ForcePlaceableExamineWindow(player, placeable);

        /// <inheritdoc cref="IPlayerPluginService.ForcePlaceableInventoryWindow"/>
        public static void ForcePlaceableInventoryWindow(uint player, uint placeable) => _service.ForcePlaceableInventoryWindow(player, placeable);

        /// <inheritdoc cref="IPlayerPluginService.StartGuiTimingBar"/>
        public static void StartGuiTimingBar(uint player, float seconds, string script = "", TimingBarType type = TimingBarType.Custom) => _service.StartGuiTimingBar(player, seconds, script, type);

        /// <inheritdoc cref="IPlayerPluginService.StopGuiTimingBar"/>
        public static void StopGuiTimingBar(uint player, string script = "") => _service.StopGuiTimingBar(player, script);

        /// <inheritdoc cref="IPlayerPluginService.SetAlwaysWalk"/>
        public static void SetAlwaysWalk(uint player, bool walk) => _service.SetAlwaysWalk(player, walk);

        /// <inheritdoc cref="IPlayerPluginService.GetQuickBarSlot"/>
        public static QuickBarSlot GetQuickBarSlot(uint player, int slot) => _service.GetQuickBarSlot(player, slot);

        /// <inheritdoc cref="IPlayerPluginService.SetQuickBarSlot"/>
        public static void SetQuickBarSlot(uint player, int slot, QuickBarSlot qbs) => _service.SetQuickBarSlot(player, slot, qbs);

        /// <inheritdoc cref="IPlayerPluginService.GetBicFileName"/>
        public static string GetBicFileName(uint player) => _service.GetBicFileName(player);

        /// <inheritdoc cref="IPlayerPluginService.ShowVisualEffect"/>
        public static void ShowVisualEffect(uint player, int effectId, float scale, Vector3 position, Vector3 translate, Vector3 rotate) => _service.ShowVisualEffect(player, effectId, scale, position, translate, rotate);

        /// <inheritdoc cref="IPlayerPluginService.MusicBackgroundChangeTimeToggle"/>
        public static void MusicBackgroundChangeTimeToggle(uint player, int track, bool night) => _service.MusicBackgroundChangeTimeToggle(player, track, night);

        /// <inheritdoc cref="IPlayerPluginService.MusicBackgroundToggle"/>
        public static void MusicBackgroundToggle(uint player, bool on) => _service.MusicBackgroundToggle(player, on);

        /// <inheritdoc cref="IPlayerPluginService.MusicBattleChange"/>
        public static void MusicBattleChange(uint player, int track) => _service.MusicBattleChange(player, track);

        /// <inheritdoc cref="IPlayerPluginService.MusicBattleToggle"/>
        public static void MusicBattleToggle(uint player, bool on) => _service.MusicBattleToggle(player, on);

        /// <inheritdoc cref="IPlayerPluginService.PlaySound"/>
        public static void PlaySound(uint player, string sound, uint target) => _service.PlaySound(player, sound, target);

        /// <inheritdoc cref="IPlayerPluginService.SetPlaceableUseable"/>
        public static void SetPlaceableUseable(uint player, uint placeable, bool usable) => _service.SetPlaceableUseable(player, placeable, usable);

        /// <inheritdoc cref="IPlayerPluginService.SetRestDuration"/>
        public static void SetRestDuration(uint player, int duration) => _service.SetRestDuration(player, duration);

        /// <inheritdoc cref="IPlayerPluginService.ApplyInstantVisualEffectToObject"/>
        public static void ApplyInstantVisualEffectToObject(uint player, uint target, VisualEffectType visualEffect) => _service.ApplyInstantVisualEffectToObject(player, target, visualEffect);

        /// <inheritdoc cref="IPlayerPluginService.UpdateCharacterSheet"/>
        public static void UpdateCharacterSheet(uint player) => _service.UpdateCharacterSheet(player);

        /// <inheritdoc cref="IPlayerPluginService.OpenInventory"/>
        public static void OpenInventory(uint player, uint target, bool open = true) => _service.OpenInventory(player, target, open);

        /// <inheritdoc cref="IPlayerPluginService.GetAreaExplorationState"/>
        public static string GetAreaExplorationState(uint player, uint area) => _service.GetAreaExplorationState(player, area);

        /// <inheritdoc cref="IPlayerPluginService.SetAreaExplorationState"/>
        public static void SetAreaExplorationState(uint player, uint area, string encodedString) => _service.SetAreaExplorationState(player, area, encodedString);

        /// <inheritdoc cref="IPlayerPluginService.SetRestAnimation"/>
        public static void SetRestAnimation(uint player, int animation) => _service.SetRestAnimation(player, animation);

        /// <inheritdoc cref="IPlayerPluginService.SetObjectVisualTransformOverride"/>
        public static void SetObjectVisualTransformOverride(uint player, uint target, int transform, float valueToApply) => _service.SetObjectVisualTransformOverride(player, target, transform, valueToApply);

        /// <inheritdoc cref="IPlayerPluginService.ApplyLoopingVisualEffectToObject"/>
        public static void ApplyLoopingVisualEffectToObject(uint player, uint target, VisualEffectType visualEffect) => _service.ApplyLoopingVisualEffectToObject(player, target, visualEffect);

        /// <inheritdoc cref="IPlayerPluginService.SetPlaceableNameOverride"/>
        public static void SetPlaceableNameOverride(uint player, uint placeable, string name) => _service.SetPlaceableNameOverride(player, placeable, name);

        /// <inheritdoc cref="IPlayerPluginService.GetQuestCompleted"/>
        public static int GetQuestCompleted(uint player, string questName) => _service.GetQuestCompleted(player, questName);

        /// <inheritdoc cref="IPlayerPluginService.SetPersistentLocation"/>
        public static void SetPersistentLocation(string cdKeyOrCommunityName, string bicFileName, uint wayPoint, bool firstConnect = true) => _service.SetPersistentLocation(cdKeyOrCommunityName, bicFileName, wayPoint, firstConnect);

        /// <inheritdoc cref="IPlayerPluginService.UpdateItemName"/>
        public static void UpdateItemName(uint player, uint item) => _service.UpdateItemName(player, item);

        /// <inheritdoc cref="IPlayerPluginService.PossessCreature"/>
        public static bool PossessCreature(uint possessor, uint possessed, bool mindImmune = true, bool createDefaultQB = false) => _service.PossessCreature(possessor, possessed, mindImmune, createDefaultQB);

        /// <inheritdoc cref="IPlayerPluginService.GetPlatformId"/>
        public static int GetPlatformId(uint oPlayer) => _service.GetPlatformId(oPlayer);

        /// <inheritdoc cref="IPlayerPluginService.GetLanguage"/>
        public static int GetLanguage(uint oPlayer) => _service.GetLanguage(oPlayer);

        /// <inheritdoc cref="IPlayerPluginService.SetResManOverride"/>
        public static void SetResManOverride(uint oPlayer, int nResType, string sOldResName, string sNewResName) => _service.SetResManOverride(oPlayer, nResType, sOldResName, sNewResName);

        /// <inheritdoc cref="IPlayerPluginService.ToggleDM"/>
        public static void ToggleDM(uint oPlayer, bool bIsDM) => _service.ToggleDM(oPlayer, bIsDM);

        /// <inheritdoc cref="IPlayerPluginService.SetObjectMouseCursorOverride"/>
        public static void SetObjectMouseCursorOverride(uint oPlayer, uint oObject, MouseCursorType nCursor) => _service.SetObjectMouseCursorOverride(oPlayer, oObject, nCursor);

        /// <inheritdoc cref="IPlayerPluginService.SetObjectHiliteColorOverride"/>
        public static void SetObjectHiliteColorOverride(uint oPlayer, uint oObject, int nColor) => _service.SetObjectHiliteColorOverride(oPlayer, oObject, nColor);

        /// <inheritdoc cref="IPlayerPluginService.RemoveEffectFromTURD"/>
        public static void RemoveEffectFromTURD(uint oPlayer, string sEffectTag) => _service.RemoveEffectFromTURD(oPlayer, sEffectTag);

        /// <inheritdoc cref="IPlayerPluginService.SetSpawnLocation"/>
        public static void SetSpawnLocation(uint oPlayer, Location locSpawn) => _service.SetSpawnLocation(oPlayer, locSpawn);

        /// <inheritdoc cref="IPlayerPluginService.SetCustomToken"/>
        public static void SetCustomToken(uint oPlayer, int nCustomTokenNumber, string sTokenValue) => _service.SetCustomToken(oPlayer, nCustomTokenNumber, sTokenValue);

        /// <inheritdoc cref="IPlayerPluginService.SetCreatureNameOverride"/>
        public static void SetCreatureNameOverride(uint oPlayer, uint oCreature, string sName) => _service.SetCreatureNameOverride(oPlayer, oCreature, sName);

        /// <inheritdoc cref="IPlayerPluginService.FloatingTextStringOnCreature"/>
        public static void FloatingTextStringOnCreature(uint oPlayer, uint oCreature, string sText, bool bChatWindow = true) => _service.FloatingTextStringOnCreature(oPlayer, oCreature, sText, bChatWindow);

        /// <inheritdoc cref="IPlayerPluginService.AddCustomJournalEntry"/>
        public static int AddCustomJournalEntry(uint player, JournalEntry journalEntry, bool isSilentUpdate = false) => _service.AddCustomJournalEntry(player, journalEntry, isSilentUpdate);

        /// <inheritdoc cref="IPlayerPluginService.GetJournalEntry"/>
        public static JournalEntry GetJournalEntry(uint player, string questTag) => _service.GetJournalEntry(player, questTag);

        /// <inheritdoc cref="IPlayerPluginService.CloseStore"/>
        public static void CloseStore(uint oPlayer) => _service.CloseStore(oPlayer);

        /// <inheritdoc cref="IPlayerPluginService.SetTlkOverride"/>
        public static void SetTlkOverride(uint oPlayer, int nStrRef, string sOverride, bool bRestoreGlobal = true) => _service.SetTlkOverride(oPlayer, nStrRef, sOverride, bRestoreGlobal);

        /// <inheritdoc cref="IPlayerPluginService.GetOpenStore"/>
        public static uint GetOpenStore(uint player) => _service.GetOpenStore(player);
    }
}
