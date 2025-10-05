using System.Numerics;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWNX.Model;
using SWLOR.NWN.API.NWScript.Enum;
using QuickBarSlot = SWLOR.NWN.API.NWNX.Model.QuickBarSlot;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive player interface and interaction functionality including GUI controls,
    /// timing bars, quickbar management, and advanced player-specific features. This plugin allows
    /// for detailed control over the player experience and interface customization.
    /// </summary>
    public class PlayerPluginService : IPlayerPluginService
    {
        /// <inheritdoc/>
        public void ForcePlaceableExamineWindow(uint player, uint placeable)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ForcePlaceableExamineWindow(player, placeable);
        }

        /// <inheritdoc/>
        public void ForcePlaceableInventoryWindow(uint player, uint placeable)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ForcePlaceableInventoryWindow(player, placeable);
        }

        /// <inheritdoc/>
        public void StartGuiTimingBar(uint player, float seconds, string script = "", TimingBarType type = TimingBarType.Custom)
        {
            global::NWN.Core.NWNX.PlayerPlugin.StartGuiTimingBar(player, seconds, script, (int)type);
        }

        /// <inheritdoc/>
        public void StopGuiTimingBar(uint player, string script = "")
        {
            global::NWN.Core.NWNX.PlayerPlugin.StopGuiTimingBar(player, script);
        }

        /// <inheritdoc/>
        public void SetAlwaysWalk(uint player, bool walk)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetAlwaysWalk(player, walk ? 1 : 0);
        }

        /// <inheritdoc/>
        public QuickBarSlot GetQuickBarSlot(uint player, int slot)
        {
            var coreResult = global::NWN.Core.NWNX.PlayerPlugin.GetQuickBarSlot(player, slot);
            return new QuickBarSlot
            {
                Associate = coreResult.oAssociate,
                AssociateType = coreResult.nAssociateType,
                DomainLevel = coreResult.nDomainLevel,
                MetaType = coreResult.nMetaType,
                INTParam1 = coreResult.nINTParam1,
                ToolTip = coreResult.sToolTip,
                CommandLine = coreResult.sCommandLine,
                CommandLabel = coreResult.sCommandLabel,
                Resref = coreResult.sResRef,
                MultiClass = coreResult.nMultiClass,
                ObjectType = (QuickBarSlotType)coreResult.nObjectType,
                SecondaryItem = coreResult.oSecondaryItem,
                Item = coreResult.oItem
            };
        }

        /// <inheritdoc/>
        public void SetQuickBarSlot(uint player, int slot, QuickBarSlot qbs)
        {
            var coreQbs = new global::NWN.Core.NWNX.QuickBarSlot
            {
                oItem = qbs.Item ?? OBJECT_INVALID,
                oSecondaryItem = qbs.SecondaryItem ?? OBJECT_INVALID,
                nObjectType = (int)qbs.ObjectType,
                nMultiClass = qbs.MultiClass,
                sResRef = qbs.Resref ?? "",
                sCommandLabel = qbs.CommandLabel ?? "",
                sCommandLine = qbs.CommandLine ?? "",
                sToolTip = qbs.ToolTip ?? "",
                nINTParam1 = qbs.INTParam1,
                nMetaType = qbs.MetaType,
                nDomainLevel = qbs.DomainLevel,
                nAssociateType = qbs.AssociateType,
                oAssociate = qbs.Associate ?? OBJECT_INVALID
            };
            global::NWN.Core.NWNX.PlayerPlugin.SetQuickBarSlot(player, slot, coreQbs);
        }


        /// <inheritdoc/>
        public string GetBicFileName(uint player)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetBicFileName(player);
        }

        /// <inheritdoc/>
        public void ShowVisualEffect(uint player, int effectId, float scale, Vector3 position, Vector3 translate, Vector3 rotate)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ShowVisualEffect(player, effectId, position, scale, translate, rotate);
        }

        /// <inheritdoc/>
        public void MusicBackgroundChangeTimeToggle(uint player, int track, bool night)
        {
            if (night)
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundChangeNight(player, track);
            else
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundChangeDay(player, track);
        }

        /// <inheritdoc/>
        public void MusicBackgroundToggle(uint player, bool on)
        {
            if (on)
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundStart(player);
            else
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundStop(player);
        }

        /// <inheritdoc/>
        public void MusicBattleChange(uint player, int track)
        {
            global::NWN.Core.NWNX.PlayerPlugin.MusicBattleChange(player, track);
        }

        /// <inheritdoc/>
        public void MusicBattleToggle(uint player, bool on)
        {
            if (on)
                global::NWN.Core.NWNX.PlayerPlugin.MusicBattleStart(player);
            else
                global::NWN.Core.NWNX.PlayerPlugin.MusicBattleStop(player);
        }

        /// <inheritdoc/>
        public void PlaySound(uint player, string sound, uint target)
        {
            global::NWN.Core.NWNX.PlayerPlugin.PlaySound(player, sound, target);
        }

        /// <inheritdoc/>
        public void SetPlaceableUseable(uint player, uint placeable, bool usable)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetPlaceableUsable(player, placeable, usable ? 1 : 0);
        }

        /// <inheritdoc/>
        public void SetRestDuration(uint player, int duration)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetRestDuration(player, duration);
        }

        /// <inheritdoc/>
        public void ApplyInstantVisualEffectToObject(uint player, uint target, VisualEffectType visualEffect)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ApplyInstantVisualEffectToObject(player, target, (int)visualEffect);
        }

        /// <inheritdoc/>
        public void UpdateCharacterSheet(uint player)
        {
            global::NWN.Core.NWNX.PlayerPlugin.UpdateCharacterSheet(player);
        }

        /// <inheritdoc/>
        public void OpenInventory(uint player, uint target, bool open = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.OpenInventory(player, target, open ? 1 : 0);
        }

        /// <inheritdoc/>
        public string GetAreaExplorationState(uint player, uint area)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetAreaExplorationState(player, area);
        }

        /// <inheritdoc/>
        public void SetAreaExplorationState(uint player, uint area, string encodedString)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetAreaExplorationState(player, area, encodedString);
        }

        /// <inheritdoc/>
        public void SetRestAnimation(uint player, int animation)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetRestAnimation(player, animation);
        }

        /// <inheritdoc/>
        public void SetObjectVisualTransformOverride(uint player, uint target, int transform,
            float valueToApply)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetObjectVisualTransformOverride(player, target, transform, valueToApply);
        }

        /// <inheritdoc/>
        public void ApplyLoopingVisualEffectToObject(uint player, uint target, VisualEffectType visualEffect)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ApplyLoopingVisualEffectToObject(player, target, (int)visualEffect);
        }

        /// <inheritdoc/>
        public void SetPlaceableNameOverride(uint player, uint placeable, string name)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetPlaceableNameOverride(player, placeable, name);
        }

        /// <inheritdoc/>
        public int GetQuestCompleted(uint player, string questName)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetQuestCompleted(player, questName);
        }

        /// <inheritdoc/>
        public void SetPersistentLocation(string cdKeyOrCommunityName, string bicFileName, uint wayPoint,
            bool firstConnect = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetPersistentLocation(cdKeyOrCommunityName, bicFileName, wayPoint, firstConnect ? 1 : 0);
        }

        /// <inheritdoc/>
        public void UpdateItemName(uint player, uint item)
        {
            global::NWN.Core.NWNX.PlayerPlugin.UpdateItemName(player, item);
        }

        /// <inheritdoc/>
        public bool PossessCreature(uint possessor, uint possessed, bool mindImmune = true,
            bool createDefaultQB = false)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.PossessCreature(possessor, possessed, mindImmune ? 1 : 0, createDefaultQB ? 1 : 0) != 0;
        }

        /// <inheritdoc/>
        public int GetPlatformId(uint oPlayer)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetPlatformId(oPlayer);
        }

        /// <inheritdoc/>
        public int GetLanguage(uint oPlayer)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetLanguage(oPlayer);
        }

        /// <inheritdoc/>
        public void SetResManOverride(uint oPlayer, int nResType, string sOldResName, string sNewResName)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetResManOverride(oPlayer, nResType, sOldResName, sNewResName);
        }

        // @brief Toggle oPlayer's PlayerDM status.
        /// <inheritdoc/>
        public void ToggleDM(uint oPlayer, bool bIsDM)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ToggleDM(oPlayer, bIsDM ? 1 : 0);
        }


        /// <inheritdoc/>
        public void SetObjectMouseCursorOverride(uint oPlayer, uint oObject, MouseCursorType nCursor)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetObjectMouseCursorOverride(oPlayer, oObject, (int)nCursor);
        }

        /// <inheritdoc/>
        public void SetObjectHiliteColorOverride(uint oPlayer, uint oObject, int nColor)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetObjectHiliteColorOverride(oPlayer, oObject, nColor);
        }

        /// <inheritdoc/>
        public void RemoveEffectFromTURD(uint oPlayer, string sEffectTag)
        {
            global::NWN.Core.NWNX.PlayerPlugin.RemoveEffectFromTURD(oPlayer, sEffectTag);
        }

        /// <inheritdoc/>
        public void SetSpawnLocation(uint oPlayer, Location locSpawn)
        {
            var vPosition = GetPositionFromLocation(locSpawn);
            var locationPtr = Location(GetAreaFromLocation(locSpawn), vPosition, GetFacingFromLocation(locSpawn));
            global::NWN.Core.NWNX.PlayerPlugin.SetSpawnLocation(oPlayer, locationPtr);
        }

        /// <inheritdoc/>
        public void SetCustomToken(uint oPlayer, int nCustomTokenNumber, string sTokenValue)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetCustomToken(oPlayer, nCustomTokenNumber, sTokenValue);
        }

        /// <inheritdoc/>
        public void SetCreatureNameOverride(uint oPlayer, uint oCreature, string sName)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetCreatureNameOverride(oPlayer, oCreature, sName);
        }

        /// <inheritdoc/>
        public void FloatingTextStringOnCreature(uint oPlayer, uint oCreature, string sText, bool bChatWindow = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.FloatingTextStringOnCreature(oPlayer, oCreature, sText, bChatWindow ? 1 : 0);
        }
        /// <inheritdoc/>
        public int AddCustomJournalEntry(uint player, JournalEntry journalEntry, bool isSilentUpdate = false)
        {
            var coreEntry = new global::NWN.Core.NWNX.JournalEntry
            {
                sName = journalEntry.Name,
                sText = journalEntry.Text,
                sTag = journalEntry.Tag,
                nState = journalEntry.State,
                nPriority = journalEntry.Priority,
                nQuestCompleted = journalEntry.IsQuestCompleted ? 1 : 0,
                nQuestDisplayed = journalEntry.IsQuestDisplayed ? 1 : 0,
                nUpdated = journalEntry.Updated,
                nCalendarDay = journalEntry.CalendarDay,
                nTimeOfDay = journalEntry.TimeOfDay
            };
            return global::NWN.Core.NWNX.PlayerPlugin.AddCustomJournalEntry(player, coreEntry, isSilentUpdate ? 1 : 0);
        }


        /// <inheritdoc/>
        public JournalEntry GetJournalEntry(uint player, string questTag)
        {
            var coreResult = global::NWN.Core.NWNX.PlayerPlugin.GetJournalEntry(player, questTag);
            return new JournalEntry
            {
                Name = coreResult.sName,
                Text = coreResult.sText,
                Tag = coreResult.sTag,
                State = coreResult.nState,
                Priority = coreResult.nPriority,
                IsQuestCompleted = coreResult.nQuestCompleted != 0,
                IsQuestDisplayed = coreResult.nQuestDisplayed != 0,
                Updated = coreResult.nUpdated,
                CalendarDay = coreResult.nCalendarDay,
                TimeOfDay = coreResult.nTimeOfDay
            };
        }

        /// <inheritdoc/>
        public void CloseStore(uint oPlayer)
        {
            global::NWN.Core.NWNX.PlayerPlugin.CloseStore(oPlayer);
        }

        /// <inheritdoc/>
        public void SetTlkOverride(uint oPlayer, int nStrRef, string sOverride, bool bRestoreGlobal = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetTlkOverride(oPlayer, nStrRef, sOverride, bRestoreGlobal ? 1 : 0);
        }

        /// <inheritdoc/>
        public uint GetOpenStore(uint player)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetOpenStore(player);
        }
    }
}