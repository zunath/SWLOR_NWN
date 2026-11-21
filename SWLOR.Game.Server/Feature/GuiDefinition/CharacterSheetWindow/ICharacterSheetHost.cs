using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// What the character sheet services need from the view model that hosts them: the player and the target it shows, the bindings they
    /// write by property (so each setter runs), and the callbacks the view model owns. The view model implements it explicitly by
    /// delegating to the members it already has.
    /// </summary>
    public interface ICharacterSheetHost
    {
        uint Player { get; }
        uint Target { get; }
        bool IsPlayerMode { get; }
        string AttackDescription { get; }

        string HP { get; set; }
        string FP { get; set; }
        string STM { get; set; }
        string Name { get; set; }

        int Might { get; set; }
        int Perception { get; set; }
        int Vitality { get; set; }
        int Willpower { get; set; }
        int Agility { get; set; }
        int Social { get; set; }

        bool IsMightUpgradeAvailable { get; set; }
        bool IsPerceptionUpgradeAvailable { get; set; }
        bool IsVitalityUpgradeAvailable { get; set; }
        bool IsWillpowerUpgradeAvailable { get; set; }
        bool IsAgilityUpgradeAvailable { get; set; }
        bool IsSocialUpgradeAvailable { get; set; }

        string SkillRanks { get; set; }
        string SP { get; set; }
        string APOrLevel { get; set; }
        string APOrLevelTooltip { get; set; }

        string MainHandDMG { get; set; }
        string OffHandDMG { get; set; }
        string MainHandTooltip { get; set; }
        string OffHandTooltip { get; set; }
        string AttackDelay { get; set; }
        string AttackDelayTooltip { get; set; }
        int Attack { get; set; }
        string AttackTooltip { get; set; }
        int ForceAttack { get; set; }
        int PhysicalDefense { get; set; }
        int ForceDefense { get; set; }
        int WeaponAccuracy { get; set; }
        string OffHandAccuracy { get; set; }
        int ForceAccuracy { get; set; }
        int Evasion { get; set; }

        string Control { get; set; }
        string Craftsmanship { get; set; }

        void ShowModal(string prompt, Action confirmAction);
        void LoadData();
        void RefreshHitPoints();
        void RefreshCharacterStatsList();
        void RefreshStatsTable(IEnumerable<StatEntry> rows);
        void RefreshResistancesTable(IEnumerable<ResistanceEntry> rows);
        void RefreshCraftingTable(IEnumerable<CraftEntry> rows);
    }
}
