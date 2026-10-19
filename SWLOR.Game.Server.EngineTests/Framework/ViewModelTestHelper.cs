using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Opens real windows for a headless player fixture and reaches the view model members that NUI events would use.
    /// </summary>
    public static class ViewModelTestHelper
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Opens the window if it is closed and returns its view model.</summary>
        public static T OpenWindow<T>(PlayerAbilityFixture fixture, GuiWindowType type, GuiPayloadBase payload)
        {
            var windows = PlayerWindows();
            if (!windows.TryGetValue(fixture.Id, out var byType))
                windows[fixture.Id] = byType = new Dictionary<GuiWindowType, GuiPlayerWindow>();
            if (!byType.ContainsKey(type))
            {
                var template = Gui.GetWindowTemplate(type);
                var window = template.CreatePlayerWindowAction();
                window.ViewModel.Geometry = template.InitialGeometry;
                byType[type] = window;
            }

            if (!Gui.IsWindowOpen(fixture.Creature, type))
                Gui.TogglePlayerWindow(fixture.Creature, type, payload);
            return (T)Gui.GetPlayerWindow(fixture.Creature, type).ViewModel;
        }

        /// <summary>Closes the window from the server and opens it again with the same view model.</summary>
        public static T ReopenWindow<T>(PlayerAbilityFixture fixture, GuiWindowType type, GuiPayloadBase payload)
        {
            Gui.ClosePlayerWindow(fixture.Creature, type);
            return OpenWindow<T>(fixture, type, payload);
        }

        public static void CloseWindows(PlayerAbilityFixture fixture)
        {
            var windows = PlayerWindows();
            if (!windows.TryGetValue(fixture.Id, out var byType))
                return;
            foreach (var type in byType.Keys.ToList())
                Gui.ClosePlayerWindow(fixture.Creature, type);
            windows.Remove(fixture.Id);
        }

        public static object Get(object model, string name)
        {
            var type = model.GetType();
            return type.GetField(name, Members)?.GetValue(model) ?? type.GetProperty(name, Members).GetValue(model);
        }

        public static void Set(object model, string name, object value)
        {
            var type = model.GetType();
            var property = type.GetProperty(name, Members);
            if (property != null)
                property.SetValue(model, value);
            else
                type.GetField(name, Members).SetValue(model, value);
        }

        public static object Call(object model, string name, params object[] args) =>
            model.GetType().GetMethod(name, Members).Invoke(model, args);

        /// <summary>Runs the action a NUI event bound to the named method would run.</summary>
        public static void Click(object model, string name) =>
            ((Action)model.GetType().GetMethod(name, Members, Type.EmptyTypes).Invoke(model, null))();

        private static Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> PlayerWindows() =>
            (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    }
}
