using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// The binding recorder of the view model split (P5-16h, PR 0), copied from the appearance editor's recorder (P5-16e) and
    /// widened to the whole window: it is attached to the view model BEFORE <c>Bind</c>, so the opening order is recorded, and it
    /// writes one transcript line for each of the following, in the order they happen, per step.
    /// <list type="bullet">
    /// <item>every <c>PropertyChanged</c> name with the JSON <c>GuiPropertyConverter</c> gives the cached value;</item>
    /// <item>every client publication the engine sees (<c>NuiSetBind</c>, <c>NuiSetGroupLayout</c> for a partial swap or a tab, <c>NuiSetBindWatch</c> for
    /// <c>WatchOnClient</c>), through a recording decorator of the engine seam;</item>
    /// <item>the service calls that change something, through recording decorators over the real services (<c>IGuiService.PublishRefreshEvent</c>,
    /// <c>ITargetingService.EnterTargetingMode</c>, <c>IKeyItemService</c>, <c>IIncubationFieldNoteService</c>, <c>ICreatureStatService.ApplyPlayerStat</c>);</item>
    /// <item>the engine calls that touch the world: <c>EventsPlugin.SignalEvent</c>, <c>ExportSingleCharacter</c>, sounds, quick bar slots, feats, and
    /// the items created, destroyed, serialized, deserialized or acquired.</item>
    /// </list>
    /// After every step the engine state of the player is dumped: the carried and worn items, the feats, the quick bar and what changed in the saved
    /// <see cref="Player"/> record since the step before. A long JSON value is written as its length and SHA-1 with a short preview, so a one-character
    /// change still fails the comparison. The player's id is written as <c>&lt;player&gt;</c>, and no other text is masked.
    /// </summary>
    public sealed class ViewModelTranscript : IDisposable
    {
        private const int InlineJsonLimit = 400;

        private readonly PlayerAbilityFixture _fixture;
        private readonly StringBuilder _text = new();
        private readonly List<IDisposable> _scopes = new();
        private readonly List<KeyValuePair<string, Func<object>>> _tracked = new();
        private readonly Dictionary<string, Dictionary<string, string>> _previous = new();
        private readonly GuiPropertyConverter _converter = new();
        private string _phase = "open";
        private int _sequence;
        private Action<uint> _targetingCallback;

        public ViewModelTranscript(PlayerAbilityFixture fixture)
        {
            _fixture = fixture;
        }

        public uint Player => _fixture.Creature;

        public string Text => _text.ToString();

        /// <summary>The selection action of the last targeting prompt a window opened, or null.</summary>
        public Action<uint> PendingTargeting => _targetingCallback;

        /// <summary>
        /// Puts the recording decorators in front of the engine seam, the plugins and the services for the current flow. Call it inside the
        /// creature context that runs the script, and dispose the transcript when the script ends.
        /// </summary>
        public void Install()
        {
            Baseline();
            _scopes.Add(Activate());
            _scopes.Add(CompositionRoot.InstallForTests(new SpyProvider(CompositionRoot.ServerHost.Provider, this)));
            _scopes.Add(NWScriptRuntime.Override(Spy<INWScript>(NWScriptRuntime.Current, name => NWScriptNames.Contains(name))));
            _scopes.Add(EventsPluginRuntime.Override(Spy<IEventsPlugin>(EventsPluginRuntime.Current, name => name == "SignalEvent")));
            _scopes.Add(PlayerPluginRuntime.Override(Spy<IPlayerPlugin>(PlayerPluginRuntime.Current, name => name is "SetQuickBarSlot" or "PlaySound")));
            _scopes.Add(CreaturePluginRuntime.Override(Spy<ICreaturePlugin>(CreaturePluginRuntime.Current,
                name => name is "AddFeat" or "RemoveFeat" or "AddFeatByLevel" or "SetFeatRemainingUses")));
            _scopes.Add(ObjectPluginRuntime.Override(Spy<IObjectPlugin>(ObjectPluginRuntime.Current,
                name => name is "Serialize" or "Deserialize" or "AcquireItem" or "SetMaxHitPoints")));
        }

        private bool _baselined;

        /// <summary>Takes the saved record as the baseline the first dump compares with, so what opening the window changes is written.</summary>
        private void Baseline()
        {
            if (_baselined)
                return;
            _baselined = true;
            var current = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var leaf in JObject.FromObject(DB.Get<Player>(_fixture.Id)).SelectTokens("$..*").OfType<JValue>())
                current[leaf.Path] = leaf.ToString(Formatting.None, Array.Empty<JsonConverter>());
            _previous["record"] = current;
        }

        /// <summary>
        /// Opens a window the way <see cref="ViewModelTestHelper.OpenWindow{T}"/> does, but with the recorder attached to the view model before
        /// it is bound. A window that is already open is only returned.
        /// </summary>
        public T Open<T>(GuiWindowType type, GuiPayloadBase payload)
        {
            var windows = GuiInternals.PlayerWindows();
            if (!windows.TryGetValue(_fixture.Id, out var byType))
                windows[_fixture.Id] = byType = new Dictionary<GuiWindowType, GuiPlayerWindow>();
            if (!byType.ContainsKey(type))
            {
                var template = Gui.GetWindowTemplate(type);
                var window = template.CreatePlayerWindowAction();
                window.ViewModel.Geometry = template.InitialGeometry;
                byType[type] = window;
                Attach(window.ViewModel);
            }

            if (!Gui.IsWindowOpen(_fixture.Creature, type))
                Gui.TogglePlayerWindow(_fixture.Creature, type, payload);
            return (T)Gui.GetPlayerWindow(_fixture.Creature, type).ViewModel;
        }

        /// <summary>Closes the window from the server and opens it again with the same view model, which stays attached.</summary>
        public T Reopen<T>(GuiWindowType type, GuiPayloadBase payload)
        {
            Gui.ClosePlayerWindow(_fixture.Creature, type);
            return Open<T>(type, payload);
        }

        /// <summary>Adds a labelled object to the dump after every step; only what changed since the step before is written.</summary>
        public void Track(string label, Func<object> read) => _tracked.Add(new KeyValuePair<string, Func<object>>(label, read));

        /// <summary>Runs one scripted step, then writes the engine state.</summary>
        public void Step(string name, Action action)
        {
            _phase = name;
            _sequence = 0;
            Line("step", string.Empty);
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Line("exception", exception.GetType().Name + " " + FirstLine(exception.Message));
            }

            DumpState();
        }

        /// <summary>Runs the targeting action the last prompt registered with the item, as a player's selection would.</summary>
        public void Target(uint target)
        {
            var callback = _targetingCallback;
            _targetingCallback = null;
            Line("target", Describe(target));
            callback?.Invoke(target);
        }

        /// <summary>Removes the recording decorators; the transcript keeps what it recorded and can be installed again.</summary>
        public void Uninstall()
        {
            for (var index = _scopes.Count - 1; index >= 0; index--)
                _scopes[index].Dispose();
            _scopes.Clear();
        }

        public void Dispose() => Uninstall();

        private void Attach(IGuiViewModel model)
        {
            var cache = (IDictionary)model.GetType().BaseType!
                .GetField("_propertyValues", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;
            ((INotifyPropertyChanged)model).PropertyChanged += (_, args) =>
            {
                // Only what a scripted phase causes is recorded; the sheet's live hit point loop and the engine's delayed refreshes
                // raise events between phases, at times that differ from run to run.
                if (!ReferenceEquals(_current, this))
                    return;
                var detail = cache.Contains(args.PropertyName) ? cache[args.PropertyName] : null;
                var raw = detail?.GetType().GetProperty("Value")!.GetValue(detail);
                var json = raw == null
                    ? "null"
                    : Compress(JToken.Parse(JsonDump(_converter.ToJson(raw))).ToString(Formatting.None));
                Line("property " + args.PropertyName, json);
            };
        }

        internal void Line(string kind, string value)
        {
            _text.Append(Clean(_phase)).Append(' ').Append(++_sequence).Append(' ').Append(Clean(kind));
            if (value.Length > 0)
                _text.Append(' ').Append(Clean(value));
            _text.Append('\n');
        }

        private string Clean(string text) => text.Replace(_fixture.Id, "<player>");

        private static string FirstLine(string text) => text.Split('\n')[0].TrimEnd('\r');

        // Not System.Security.Cryptography: the hosted runtime in the server container hangs creating a SHA-1 (the server's own notes name it).
        // Two 64-bit FNV-1a hashes with different bases and primes over the UTF-16 text identify a changed value as well.
        private static string Compress(string json)
        {
            if (json.Length <= InlineJsonLimit)
                return json;
            ulong first = 14695981039346656037UL;
            ulong second = 0x9E3779B97F4A7C15UL;
            foreach (var character in json)
            {
                first = (first ^ character) * 1099511628211UL;
                second = (second ^ character) * 0x100000001B3UL * 31UL + 17UL;
            }

            return "<" + json.Length + " chars hash " + first.ToString("x16") + second.ToString("x16") + "> " + json.Substring(0, 120);
        }

        internal string Describe(object argument)
        {
            switch (argument)
            {
                case null:
                    return "null";
                case string text:
                    // A serialized item or entity holds ids and stamps of its own; its length is what is recorded.
                    return text.Length > InlineJsonLimit ? "<string " + text.Length + " chars>" : text;
                case EntityBase entity:
                    return "<" + entity.GetType().Name + ">";
                case uint id:
                    return DescribeObject(id);
                case Delegate:
                    return "<action>";
                case IEnumerable sequence:
                    return "[" + string.Join(",", sequence.Cast<object>().Select(Describe)) + "]";
                case float number:
                    return number.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                case IConvertible convertible:
                    return convertible.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            // An engine JSON value is not read here: the engine owns it once a call has it, and reading it again can crash the server. The
            // values a window publishes are recorded from the view model's own cache (property lines).
            if (argument.GetType().Name == "Json")
                return "<json>";
            try
            {
                return Compress(JsonConvert.SerializeObject(argument));
            }
            catch (Exception)
            {
                return argument.GetType().Name;
            }
        }

        private string DescribeObject(uint id)
        {
            if (id == _fixture.Creature)
                return "<player>";
            if (!GetIsObjectValid(id))
                return id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "obj(" + GetResRef(id) + "|" + GetTag(id) + ")";
        }

        private void DumpState()
        {
            var creature = _fixture.Creature;
            var carried = new List<string>();
            for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
                carried.Add(GetResRef(item) + "|" + GetTag(item) + "|" + GetItemStackSize(item));
            carried.Sort(StringComparer.Ordinal);
            Line("state carried", string.Join(";", carried));

            var worn = new List<string>();
            foreach (InventorySlot slot in Enum.GetValues(typeof(InventorySlot)))
            {
                if (slot == InventorySlot.Invalid)
                    continue;
                var item = GetItemInSlot(slot, creature);
                if (GetIsObjectValid(item))
                    worn.Add(slot + "=" + GetResRef(item));
            }

            Line("state worn", string.Join(";", worn));

            var feats = new List<int>();
            for (var index = 0; index < CreaturePlugin.GetFeatCount(creature); index++)
                feats.Add((int)CreaturePlugin.GetFeatByIndex(creature, index));
            feats.Sort();
            Line("state feats", string.Join(",", feats));

            var bar = new List<string>();
            for (var slot = 0; slot < 36; slot++)
            {
                var entry = PlayerPlugin.GetQuickBarSlot(creature, slot);
                if (entry.ObjectType != QuickBarSlotType.Empty)
                    bar.Add(slot + "=" + (int)entry.ObjectType + ":" + entry.INTParam1 + ":" + entry.Resref);
            }

            Line("state quickbar", string.Join(";", bar));

            DumpDiff("record", () => JObject.FromObject(DB.Get<Player>(_fixture.Id)));
            foreach (var pair in _tracked)
                DumpDiff(pair.Key, () => pair.Value() is JToken token ? token : JToken.FromObject(pair.Value()));
        }

        private void DumpListDiff(string name, Func<IList> read)
        {
            var list = read();
            var parts = new List<string>();
            for (var index = 0; list != null && index < list.Count; index++)
            {
                parts.Add(Plain(list[index]));
            }

            var text = list == null ? "null" : Compress("[" + string.Join(",", parts) + "]");
            if (_listText.TryGetValue(name, out var before) && before == text)
                return;
            _listText[name] = text;
            Line("state list " + name, text);
        }

        /// <summary>One list element as text, from its own simple members only (no serializer, no engine-backed property).</summary>
        private static string Plain(object value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string text:
                    return JsonConvert.ToString(text);
                case bool or Enum:
                    return value.ToString();
                case IConvertible convertible:
                    return convertible.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            // Only the window component value types are read member by member; anything else (an engine structure, say) is named.
            if (value.GetType().Namespace?.StartsWith("SWLOR.Game.Server.Domain.UI", StringComparison.Ordinal) != true || value is IEnumerable)
                return "<" + value.GetType().Name + ">";
            var members = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length == 0 && ((property.PropertyType.IsPrimitive && property.PropertyType != typeof(IntPtr) && property.PropertyType != typeof(UIntPtr)) ||
                                                                               property.PropertyType.IsEnum || property.PropertyType == typeof(string)))
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => property.Name + "=" + Plain(property.GetValue(value)));
            return "{" + string.Join(",", members) + "}";
        }

        private readonly Dictionary<string, string> _listText = new();
        private readonly List<KeyValuePair<string, Func<IList>>> _watched = new();

        private void DumpDiff(string label, Func<JToken> read)
        {
            var current = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var leaf in read().SelectTokens("$..*").OfType<JValue>())
                current[leaf.Path] = leaf.ToString(Formatting.None, Array.Empty<JsonConverter>());
            if (_previous.TryGetValue(label, out var before))
            {
                foreach (var pair in current.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                    if (!before.TryGetValue(pair.Key, out var old) || old != pair.Value)
                        Line("state " + label + " " + pair.Key, Compress(pair.Value));
                foreach (var key in before.Keys.Where(key => !current.ContainsKey(key)).OrderBy(key => key, StringComparer.Ordinal))
                    Line("state " + label + " " + key, "<removed>");
            }

            _previous[label] = current;
        }

        private static readonly HashSet<string> NWScriptNames = new(StringComparer.Ordinal)
        {
            "NuiSetBind", "NuiSetGroupLayout", "NuiSetBindWatch", "NuiDestroy", "NuiCreate", "ExportSingleCharacter",
            "CreateItemOnObject", "DestroyObject", "CopyItem", "SetItemStackSize", "SendMessageToPC", "FloatingTextStringOnCreature",
            "PlaySound", "PlaySoundByStrRef", "GiveXPToCreature", "SetXP", "ActionTakeItem", "ActionGiveItem", "ApplyEffectToObject",
            "RemoveEffect", "SetLocalInt", "DeleteLocalInt", "SetLocalString", "DeleteLocalString", "SetLocalObject"
        };

        private static T Spy<T>(T target, Func<string, bool> include)
            where T : class
        {
            var proxy = DispatchProxy.Create<T, RecordingProxy>();
            ((RecordingProxy)(object)proxy).Target = target;
            ((RecordingProxy)(object)proxy).Include = include;
            return proxy;
        }

        private void Spied(string name, object[] args, object result, string owner)
        {
            var text = string.Join(", ", args.Select(Describe));
            Line(owner + " " + name, text);
        }

        /// <summary>The container the facades resolve from while the transcript runs: the server's, with the five services spied.</summary>
        private sealed class SpyProvider : IServiceProvider, ISupportRequiredService
        {
            private readonly IServiceProvider _real;
            private readonly ViewModelTranscript _owner;
            private readonly Dictionary<Type, object> _spies = new();

            public SpyProvider(IServiceProvider real, ViewModelTranscript owner)
            {
                _real = real;
                _owner = owner;
            }

            public object GetService(Type serviceType)
            {
                var real = _real.GetService(serviceType);
                if (real == null || !SpiedTypes.Contains(serviceType))
                    return real;
                if (!_spies.TryGetValue(serviceType, out var spy))
                {
                    var create = typeof(DispatchProxy).GetMethods().Single(method => method.Name == nameof(DispatchProxy.Create) && method.GetGenericArguments().Length == 2)
                        .MakeGenericMethod(serviceType, typeof(ServiceProxy));
                    var proxy = create.Invoke(null, null)!;
                    var service = (ServiceProxy)proxy;
                    service.Target = real;
                    service.Owner = _owner;
                    service.Interface = serviceType.Name;
                    _spies[serviceType] = spy = proxy;
                }

                return spy;
            }

            public object GetRequiredService(Type serviceType) =>
                GetService(serviceType) ?? throw new InvalidOperationException("No service of type " + serviceType.Name + " is registered.");

            private static readonly HashSet<Type> SpiedTypes = new()
            {
                typeof(IGuiService), typeof(ITargetingService), typeof(IKeyItemService), typeof(IIncubationFieldNoteService), typeof(ICreatureStatService)
            };
        }

        /// <summary>Records the calls of one engine interface that the transcript names.</summary>
        public class RecordingProxy : DispatchProxy
        {
            public object Target { get; set; }

            public Func<string, bool> Include { get; set; }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                // The arguments are described before the call: an engine call can consume a JSON value or destroy an object.
                var transcript = Current;
                var described = transcript != null && Include(targetMethod.Name)
                    ? string.Join(", ", (args ?? Array.Empty<object>()).Select(transcript.Describe))
                    : null;
                object result;
                try
                {
                    result = targetMethod.Invoke(Target, args);
                }
                catch (TargetInvocationException exception)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
                    throw;
                }

                if (described != null)
                    transcript.Line("engine " + targetMethod.Name, described);
                return result;
            }
        }

        /// <summary>Records the calls of one service that change something, and holds back the targeting prompt for the script to answer.</summary>
        public class ServiceProxy : DispatchProxy
        {
            public object Target { get; set; }

            public ViewModelTranscript Owner { get; set; }

            public string Interface { get; set; }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                var name = targetMethod.Name;
                args ??= Array.Empty<object>();
                if (Interface == nameof(ITargetingService) && name == nameof(ITargetingService.EnterTargetingMode))
                {
                    Owner._targetingCallback = (Action<uint>)args.First(argument => argument is Action<uint>);
                    Owner.Line("service " + Interface + "." + name, string.Join(", ", args.Where(argument => argument is not Delegate).Select(Owner.Describe)));
                    return null;
                }

                var record = !IsRead(name) && (Interface != nameof(IGuiService) || name == nameof(IGuiService.PublishRefreshEvent));
                var described = record ? string.Join(", ", args.Select(Owner.Describe)) : null;
                object result;
                try
                {
                    result = targetMethod.Invoke(Target, args);
                }
                catch (TargetInvocationException exception)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
                    throw;
                }

                if (described != null)
                    Owner.Line("service " + Interface + "." + name, described);
                return result;
            }

            private static bool IsRead(string name) =>
                name.StartsWith("Get", StringComparison.Ordinal) || name.StartsWith("Has", StringComparison.Ordinal) ||
                name.StartsWith("Is", StringComparison.Ordinal) || name.StartsWith("Can", StringComparison.Ordinal) ||
                name.StartsWith("Find", StringComparison.Ordinal) || name.StartsWith("Load", StringComparison.Ordinal) ||
                name.StartsWith("Build", StringComparison.Ordinal);
        }

        [ThreadStatic]
        private static ViewModelTranscript _current;

        private static ViewModelTranscript Current => _current;

        /// <summary>Makes this transcript the recorder of the current thread, for the decorators.</summary>
        public IDisposable Activate()
        {
            var previous = _current;
            _current = this;
            return new ActiveScope(previous);
        }

        private sealed class ActiveScope : IDisposable
        {
            private readonly ViewModelTranscript _previous;

            public ActiveScope(ViewModelTranscript previous) => _previous = previous;

            public void Dispose() => _current = _previous;
        }
    }
    /// <summary>
    /// <c>Clock.UtcNow</c> for the transcripts: fixed while a scripted phase runs and moved only by <see cref="Advance"/>, so the time the
    /// view models read is the same in every run. Outside a phase it runs on from the fixed value, because the scheduler and the engine test
    /// runner's own waits spin forever on a clock that never moves. The tick count always reads the real clock.
    /// </summary>
    public sealed class TranscriptClock : IDisposable
    {
        public static readonly DateTime Start = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        private readonly IDisposable _scope;
        private readonly System.Diagnostics.Stopwatch _running = System.Diagnostics.Stopwatch.StartNew();
        private DateTime _fixed = Start;
        private bool _frozen;

        public TranscriptClock()
        {
            _scope = SWLOR.Game.Server.Core.Clock.OverrideForTests(utcNow: () => _frozen ? _fixed : _fixed + _running.Elapsed);
        }

        /// <summary>The fixed time: what the view models read inside a phase.</summary>
        public DateTime Now => _fixed;

        public void Advance(double seconds) => _fixed = _fixed.AddSeconds(seconds);

        /// <summary>Fixes the time at <see cref="Now"/> until the returned scope is disposed.</summary>
        public IDisposable Freeze()
        {
            _frozen = true;
            return new Thaw(this);
        }

        public void Dispose() => _scope.Dispose();

        private sealed class Thaw : IDisposable
        {
            private readonly TranscriptClock _owner;

            public Thaw(TranscriptClock owner) => _owner = owner;

            public void Dispose()
            {
                _owner._running.Restart();
                _owner._frozen = false;
            }
        }
    }
}
