using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Species;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>
    /// The Settings tab and the window's saved state: helmet and cloak display, the saved height and head size with their preview
    /// buttons, and the editor closing for a creature it cannot customize.
    /// </summary>
    public class AppearanceSettingsService
    {
        private IDBService DB { get; }
        private IDMPresenceService DMPresence { get; }
        private IGuiService Gui { get; }

        public AppearanceSettingsService(IDBService dB, IDMPresenceService dMPresence, IGuiService gui)
        {
            DB = dB;
            DMPresence = dMPresence;
            Gui = gui;
        }

        public void CloseForUnsupportedAppearance(IAppearanceEditorHost host)
        {
            // Bind uses the possessed creature as host.Player, but the DM owns the window cache.
            var owner = GetIsDMPossessed(host.Player) ? GetMaster(host.Player) : host.Player;
            SendMessageToPC(owner, "This creature's appearance cannot be customized.");
            Gui.ClosePlayerWindow(owner, GuiWindowType.AppearanceEditor, host.Player);
        }

        public void LoadSettings(IAppearanceEditorHost host)
        {
            if (host.Session.IsDMArmorDisplay)
            {
                LoadDMArmorDisplay(host);
                return;
            }

            if (!GetIsPC(host.Session.Target) || GetIsDM(host.Session.Target) || GetIsDMPossessed(host.Session.Target))
                return;

            var playerId = GetObjectUUID(host.Session.Target);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            host.ShowHelmet = dbPlayer.Settings.ShowHelmet;
            host.ShowCloak = dbPlayer.Settings.ShowCloak;
        }

        private void LoadDMArmorDisplay(IAppearanceEditorHost host)
        {
            var (showHelmet, showCloak) = ArmorDisplay.GetDMArmorDisplay(host.Session.Target);
            host.ShowHelmet = showHelmet;
            host.ShowCloak = showCloak;
        }

        public void ApplyDMArmorDisplay(IAppearanceEditorHost host)
        {
            if (!ArmorDisplay.SetDMArmorDisplay(host.Session.Target, host.ShowHelmet, host.ShowCloak))
                DMPresence.SendMessage(host.Player, ColorToken.Red("Your helmet and cloak setting could not be saved."));
        }

        public void UpdateArmorDisplay(IAppearanceEditorHost host)
        {
            ArmorDisplay.Apply(host.Session.Target, host.ShowHelmet, host.ShowCloak);
        }

        private void SendScaleMessage(IAppearanceEditorHost host, string message)
        {
            // Messages to a possessing DM's avatar are dropped, so they go to the creature the DM controls.
            DMPresence.SendMessage(host.Player, message);
        }

        public void DecreaseAppearanceScale(IAppearanceEditorHost host)
        {
            var appearanceType = GetAppearanceType(host.Session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance(host);
                return;
            }

            var scale = GetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;

            if (scale - Increment < appearance.MinimumScale)
            {
                SendScaleMessage(host, "You cannot decrease your height any further.");
            }
            else
            {
                SetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale, scale - Increment);
                SendScaleMessage(host, $"Height: {GetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale)}");
            }
        }

        public void IncreaseAppearanceScale(IAppearanceEditorHost host)
        {
            var appearanceType = GetAppearanceType(host.Session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance(host);
                return;
            }

            var scale = GetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;

            if (scale + Increment > appearance.MaximumScale)
            {
                SendScaleMessage(host, "You cannot increase your height any further.");
            }
            else
            {
                SetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale, scale + Increment);
                SendScaleMessage(host, $"Height: {GetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale)}");
            }
        }

        public void DecreaseHeadScale(IAppearanceEditorHost host)
        {
            var appearanceType = GetAppearanceType(host.Session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance(host);
                return;
            }

            var scale = HelmetModelRenderer.GetHeadScale(host.Session.Target);
            if (scale <= 0f)
                scale = 1.0f;

            const float Increment = 0.01f;

            if (scale - Increment < appearance.MinimumHeadScale)
            {
                SendScaleMessage(host, "You cannot decrease your head size any further.");
            }
            else
            {
                HelmetModelRenderer.SetHeadScale(host.Session.Target, scale - Increment);
                SendScaleMessage(host, $"Head Size: {HelmetModelRenderer.GetHeadScale(host.Session.Target)}");
            }
        }

        public void IncreaseHeadScale(IAppearanceEditorHost host)
        {
            var appearanceType = GetAppearanceType(host.Session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance(host);
                return;
            }

            var scale = HelmetModelRenderer.GetHeadScale(host.Session.Target);
            if (scale <= 0f)
                scale = 1.0f;

            const float Increment = 0.01f;

            if (scale + Increment > appearance.MaximumHeadScale)
            {
                SendScaleMessage(host, "You cannot increase your head size any further.");
            }
            else
            {
                HelmetModelRenderer.SetHeadScale(host.Session.Target, scale + Increment);
                SendScaleMessage(host, $"Head Size: {HelmetModelRenderer.GetHeadScale(host.Session.Target)}");
            }
        }

        public void SaveSettings(IAppearanceEditorHost host)
        {
            if (!GetIsPC(host.Session.Target) || GetIsDM(host.Session.Target) || GetIsDMPossessed(host.Session.Target))
                return;

            var playerId = GetObjectUUID(host.Session.Target);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.Settings.ShowCloak = host.ShowCloak;
            dbPlayer.Settings.ShowHelmet = host.ShowHelmet;

            var newHeight = GetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale);
            dbPlayer.AppearanceScale = newHeight;

            var newHeadScale = HelmetModelRenderer.GetHeadScale(host.Session.Target);
            if (newHeadScale <= 0f)
                newHeadScale = 1.0f;
            dbPlayer.HeadAppearanceScale = newHeadScale;

            DB.Set(dbPlayer);
            SendMessageToPC(host.Session.Target, ColorToken.Green("Appearance settings saved successfully."));

            UpdateArmorDisplay(host);
        }

        public void RestoreSavedScales(IAppearanceEditorHost host)
        {
            if (GetIsDM(host.Session.Target) || GetIsDMPossessed(host.Session.Target) || !GetIsPC(host.Session.Target))
                return;

            var playerId = GetObjectUUID(host.Session.Target);
            var dbPlayer = DB.Get<Player>(playerId);
            var headScale = dbPlayer.HeadAppearanceScale <= 0f ? 1.0f : dbPlayer.HeadAppearanceScale;

            SetObjectVisualTransform(host.Session.Target, ObjectVisualTransform.Scale, dbPlayer.AppearanceScale);
            HelmetModelRenderer.SetHeadScale(host.Session.Target, headScale);
        }

        public void InitializeSettings(IAppearanceEditorHost host)
        {
            host.IsPlayerSettingsVisible = GetIsPC(host.Session.Target) && !GetIsDM(host.Session.Target) && !GetIsDMPossessed(host.Session.Target);
            host.IsSettingsVisible = host.IsPlayerSettingsVisible || GetIsDM(host.Player) || GetIsDMPossessed(host.Player);
            // A DM shows or hides their avatar's or possessed creature's helmet and cloak at once, with no Save.
            host.Session.IsDMArmorDisplay = GetIsDM(host.Session.Target) || GetIsDMPossessed(host.Session.Target);
            host.IsArmorDisplayVisible = host.IsPlayerSettingsVisible || host.Session.IsDMArmorDisplay;
            host.ShowHelmet = true;
            host.ShowCloak = true;
            LoadSettings(host);
        }
    }
}
