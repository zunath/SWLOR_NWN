using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Properties;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiViewModelBase<TDerived, TPayload> : IGuiViewModel, INotifyPropertyChanged
        where TDerived : GuiViewModelBase<TDerived, TPayload>
        where TPayload : GuiPayloadBase
    {
        public uint TetherObject { get; private set; }

        private class PropertyDetail
        {
            public object Value { get; set; }
            public Type Type { get; set; }
            public bool HasEventBeenHooked { get; set; }
            public bool IsGuiList { get; set; }
            public bool SkipNotify { get; set; }
        }

        private static readonly GuiPropertyConverter _converter = new GuiPropertyConverter();

        protected uint Player { get; private set; }
        protected int WindowToken { get; private set; }

        private readonly Dictionary<string, PropertyDetail> _propertyValues = new Dictionary<string, PropertyDetail>();
        private readonly Dictionary<string, int> _partialViewGenerations = new();
        private int _bindingGeneration;

        protected abstract void Initialize(TPayload initialPayload);

        /// <summary>
        /// The window geometry. This is automatically bound for all windows.
        /// </summary>
        public GuiRectangle Geometry
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        /// <summary>
        /// Retrieves a property's value and handles notification to subscribers.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The retrieved object.</returns>
        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return default(T);

            if (_propertyValues.ContainsKey(propertyName))
                return (T)_propertyValues[propertyName].Value;

            return default(T);
        }

        /// <summary>
        /// Sets a property's value and handles notification to subscribers.
        /// </summary>
        /// <typeparam name="T">The type of data to set.</typeparam>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property.</param>
        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new Exception("Attempted to Set a value on a ViewModel with a null or empty propertyName.");
            }

            if (_propertyValues.ContainsKey(propertyName))
            {
                // No change detected, exit early.
                if (_propertyValues[propertyName] == (object)value)
                    return;
            }

            if (!_propertyValues.ContainsKey(propertyName))
                _propertyValues[propertyName] = new PropertyDetail();

            // List has already been bound, but the new value is a different object.
            // Unsubscribe the event and reset the flag.
            if (_propertyValues.ContainsKey(propertyName) &&
                _propertyValues[propertyName].IsGuiList &&
                _propertyValues[propertyName].HasEventBeenHooked &&
                !ReferenceEquals(value, _propertyValues[propertyName].Value))
            {
                var list = ((IGuiBindingList)_propertyValues[propertyName].Value);
                list.PropertyName = propertyName;
                list.ListChanged -= OnListChanged;
                _propertyValues[propertyName].HasEventBeenHooked = false;
            }

            var valueType = typeof(T);

            // The following section is explicitly for applying the workaround
            // for the Vector issue outlined here: https://github.com/Beamdog/nwn-issues/issues/427
            // If Beamdog fixes this issue, this section can be removed.
            var oldMaxSize = 0;
            var oldListItemVisibility = new GuiBindingList<bool>();

            if (_propertyValues[propertyName].Value != null)
            {
                if (
                    (valueType == typeof(GuiBindingList<string>) ||
                     valueType == typeof(GuiBindingList<int>) ||
                     valueType == typeof(GuiBindingList<bool>) ||
                     valueType == typeof(GuiBindingList<float>) ||
                     valueType == typeof(GuiBindingList<GuiRectangle>) ||
                     valueType == typeof(GuiBindingList<GuiVector2>) ||
                     valueType == typeof(GuiBindingList<GuiColor>)))
                {
                    var list = ((IGuiBindingList)_propertyValues[propertyName].Value);
                    oldMaxSize = list.MaxSize;
                    oldListItemVisibility = list.ListItemVisibility;
                }
            }

            // Update the type and value for this entry.
            _propertyValues[propertyName].Value = value;
            _propertyValues[propertyName].Type = typeof(T);

            // Binding lists - The ListChanged event must also be hooked in order to raise
            // the OnPropertyChanged event.
            if (
                (valueType == typeof(GuiBindingList<string>) ||
                 valueType == typeof(GuiBindingList<int>) ||
                 valueType == typeof(GuiBindingList<bool>) ||
                 valueType == typeof(GuiBindingList<float>) ||
                 valueType == typeof(GuiBindingList<GuiRectangle>) ||
                 valueType == typeof(GuiBindingList<GuiVector2>) ||
                 valueType == typeof(GuiBindingList<GuiColor>)))
            {
                var list = ((IGuiBindingList)_propertyValues[propertyName].Value);
                list.PropertyName = propertyName;
                list.MaxSize = oldMaxSize;
                list.ListItemVisibility = oldListItemVisibility;

                list.ListChanged += OnListChanged;

                _propertyValues[propertyName].HasEventBeenHooked = true;
                _propertyValues[propertyName].IsGuiList = true;
            }

            if (!_propertyValues[propertyName].SkipNotify)
                OnPropertyChanged(propertyName);
        }

        private void OnListChanged(object sender, ListChangedEventArgs e)
        {
            var list = ((IGuiBindingList)sender);
            OnPropertyChanged(list.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Publishes the current binding values after replacement controls have been created.
        /// Uses the cached values without invoking property setters or their editing actions.
        /// </summary>
        protected void RepublishBindings()
        {
            var propertyNames = _propertyValues
                .Where(property => property.Value.Value != null)
                .Select(property => property.Key)
                .ToArray();
            foreach (var propertyName in propertyNames)
                OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Notifies subscribers of changes.
        /// </summary>
        /// <param name="propertyName">The name of the property to notify about.</param>
        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (Player == 0 || WindowToken <= 0)
                return;

            var value = _propertyValues[propertyName].Value;
            var json = _converter.ToJson(value);

            if (_propertyValues[propertyName].IsGuiList)
            {
                var list = (IGuiBindingList)_propertyValues[propertyName].Value;

                // List visibility workaround for issue outlined here: https://github.com/Beamdog/nwn-issues/issues/427
                // This can be removed if Beamdog fixes the Vector error.
                if (list.ListItemVisibility == null)
                {
                    list.ListItemVisibility = new GuiBindingList<bool>();
                }

                if (list.Count > list.MaxSize)
                {
                    for (var x = list.MaxSize; x <= list.Count; x++)
                    {
                        list.ListItemVisibility.Add(true);
                    }

                    list.MaxSize = list.Count;
                }
                else if (list.Count < list.MaxSize)
                {
                    for (var x = list.Count; x <= list.MaxSize; x++)
                    {
                        list.ListItemVisibility[x] = false;
                    }
                }

                for (var x = 0; x < list.Count; x++)
                {
                    list.ListItemVisibility[x] = true;
                }

                var visibilities = _converter.ToJson(list.ListItemVisibility);
                NuiSetBind(Player, WindowToken, propertyName + "_RowCount", JsonInt(list.MaxSize));
                NuiSetBind(Player, WindowToken, propertyName + "_RowVisibility", visibilities);
            }

            NuiSetBind(Player, WindowToken, propertyName, json);
        }

        protected GuiWindowType WindowType { get; private set; }

        /// <summary>
        /// Binds a player and window with the associated view model.
        /// </summary>
        /// <param name="player">The player to bind.</param>
        /// <param name="windowToken">The window token to bind.</param>
        /// <param name="initialGeometry">The initial geometry to use in the event the window dimensions aren't set.</param>
        /// <param name="type">The type of window.</param>
        /// <param name="payload">The payload sent in by the caller.</param>
        /// <param name="tetherObject">The object to tether the window to.</param>
        public void Bind(
            uint player,
            int windowToken,
            GuiRectangle initialGeometry,
            GuiWindowType type,
            GuiPayloadBase payload,
            uint tetherObject)
        {
            _bindingGeneration++;
            Player = player;
            WindowToken = windowToken;
            WindowType = type;
            TetherObject = tetherObject;

            if (Geometry.X == 0.0f &&
                Geometry.Y == 0.0f &&
                Geometry.Width == 0.0f &&
                Geometry.Height == 0.0f)
            {
                Geometry = initialGeometry;
            }

            // Rebind any existing properties (in the event the window was closed and reopened)
            foreach (var (name, propertyDetail) in _propertyValues)
            {
                // A null value means the property was never assigned. Serializing it
                // would throw and permanently prevent this window from ever reopening
                // for this player - confirmed via the DebugNuiGallery hazard harness
                // (H6), where recovery required a full server restart. Skip instead.
                if (propertyDetail.Value == null)
                    continue;

                var json = _converter.ToJson(propertyDetail.Value);
                NuiSetBind(Player, WindowToken, name, json);
            }

            WatchOnClient(model => model.Geometry);

            // The input modal's text box only reports typed text back to the server
            // while this bind is watched. Set before watching (layout rule R3).
            ModalInputText = string.Empty;
            WatchOnClient(model => model.ModalInputText);

            ChangePartialView("_window_", "%%WINDOW_MAIN%%");

            // The client can drop the Geometry watch while the initial root layout is
            // applied; until a relayout re-arms it, window moves never reach the server
            // and the stale open position is what gets persisted at close. Re-issue the
            // watch once the root layout has settled so the first move is captured.
            DelayCommand(0.0f, () =>
            {
                if (Gui.IsWindowOpen(Player, WindowType))
                    WatchOnClient(model => model.Geometry);
            });

            var convertedPayload = payload == null ? default : (TPayload)payload;
            Initialize(convertedPayload);
        }


        /// <summary>
        /// Handles updating the view model with changes received from the player's client.
        /// </summary>
        /// <param name="propertyName">The name of the property to update.</param>
        public void UpdatePropertyFromClient(string propertyName)
        {
            var property = _propertyValues[propertyName];
            var json = NuiGetBind(Player, WindowToken, propertyName);
            var value = _converter.ToObject(json, property.Type);
            var currentValue = GetType().GetProperty(propertyName)?.GetValue(this);

            // The client transiently reports a 0x0 geometry while it relayouts a window
            // (e.g. during the ChangePartialView redraw nudges). Accepting it corrupts the
            // server-side geometry: a pending redraw nudge can then push a negative height
            // (client shows "constraint can not be satisfied"), or - if no server push
            // follows - the window permanently collapses to a bare title bar. Reject the
            // degenerate rect and re-assert the last known-good geometry instead.
            if (propertyName == nameof(Geometry) &&
                value is GuiRectangle degenerateCheck &&
                (degenerateCheck.Width < 1f || degenerateCheck.Height < 1f))
            {
                NuiSetBind(Player, WindowToken, nameof(Geometry), Geometry.ToJson());
                return;
            }

            _propertyValues[propertyName].Value = value;
            _propertyValues[propertyName].SkipNotify = true;
            if (!currentValue.Equals(value))
                GetType().GetProperty(propertyName)?.SetValue(this, value);

            _propertyValues[propertyName].SkipNotify = false;

            OnClientPropertyUpdated(propertyName);
        }

        /// <summary>
        /// Called after a client-watched property has been applied to this view model.
        /// Client updates suppress normal property notification to avoid echo loops.
        /// </summary>
        protected virtual void OnClientPropertyUpdated(string propertyName)
        {
        }

        /// <summary>
        /// Watches a property on the client.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        protected void WatchOnClient<TProperty>(Expression<Func<TDerived, TProperty>> expression)
        {
            var propertyName = GuiHelper<TDerived>.GetPropertyName(expression);

            // Watching serializes the property's CURRENT value, so the property must
            // have been assigned first (layout rule R3). Fail fast with a clear
            // message instead of an NRE - and without creating a null-valued entry,
            // which would poison every subsequent Bind/reopen of this window
            // (confirmed via the DebugNuiGallery hazard harness, H6).
            if (!_propertyValues.ContainsKey(propertyName) ||
                _propertyValues[propertyName].Value == null)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' must be assigned a value before WatchOnClient is called " +
                    "(layout rule R3 - see Readmes/NuiLayoutRules.md). Assign it in Initialize first, then watch.");
            }

            var value = _propertyValues[propertyName].Value;
            var json = _converter.ToJson(value);

            NuiSetBind(Player, WindowToken, propertyName, json);
            NuiSetBindWatch(Player, WindowToken, propertyName, true);
        }

        /// <summary>
        /// Displays a modal window on top of the active window being displayed.
        /// </summary>
        /// <param name="prompt">The text to display to the user inside the modal.</param>
        /// <param name="confirmAction">The action to run when the player confirms.</param>
        /// <param name="cancelAction">The action to run when the player cancels.</param>
        /// <param name="confirmText">The confirmation text to display.</param>
        /// <param name="cancelText">The cancel text to display.</param>
        protected void ShowModal(
            string prompt,
            Action confirmAction,
            Action cancelAction = null,
            string confirmText = "Yes",
            string cancelText = "No")
        {
            ModalPromptText = prompt;
            ModalConfirmButtonText = confirmText;
            ModalCancelButtonText = cancelText;
            _callerConfirmAction = confirmAction;
            _callerCancelAction = cancelAction;

            ChangePartialView("_window_", "%%WINDOW_MODAL%%");
        }

        /// <summary>
        /// Displays an input modal on top of the active window. Unlike ShowModal's
        /// yes/no prompt, this presents a multiline text box the player can type into.
        /// The confirm action reads the submitted text from <see cref="ModalInputText"/>.
        /// </summary>
        /// <param name="prompt">The text to display above the text box.</param>
        /// <param name="initialText">The text pre-filled into the text box.</param>
        /// <param name="confirmAction">The action to run when the player confirms.</param>
        /// <param name="cancelAction">The action to run when the player cancels.</param>
        /// <param name="confirmText">The confirmation text to display.</param>
        /// <param name="cancelText">The cancel text to display.</param>
        protected void ShowInputModal(
            string prompt,
            string initialText,
            Action confirmAction,
            Action cancelAction = null,
            string confirmText = "Send",
            string cancelText = "Cancel")
        {
            ModalPromptText = prompt;
            ModalConfirmButtonText = confirmText;
            ModalCancelButtonText = cancelText;
            ModalInputText = initialText ?? string.Empty;
            _callerConfirmAction = confirmAction;
            _callerCancelAction = cancelAction;

            ChangePartialView("_window_", "%%WINDOW_INPUT_MODAL%%");
        }

        /// <inheritdoc />
        public void ChangePartialView(string elementId, string partialName)
        {
            var window = Gui.GetWindowTemplate(WindowType);
            var partial = window.PartialViews[partialName];
            NuiSetGroupLayout(Player, WindowToken, elementId, partial);

            ApplyRefreshBugFix();
        }

        /// <summary>
        /// Swaps a group element's layout for one generated at runtime. Event handlers
        /// remain valid when regenerated elements reuse their registered element IDs.
        /// </summary>
        protected void SetGroupLayout(string elementId, Json layout)
        {
            NuiSetGroupLayout(Player, WindowToken, elementId, layout);
            ApplyRefreshBugFix();
        }

        /// <summary>
        /// Swaps a partial view that is nested inside another partial (e.g. a
        /// tab's content area within a window whose own root is a partial).
        /// NUI can silently drop a nested partial layout while its parent is
        /// being redrawn. This forces a root redraw first, applies the target
        /// partial, then reapplies it once more on the next tick to guarantee
        /// it survives the parent's redraw pass.
        /// </summary>
        /// <param name="elementId">The nested element id to change.</param>
        /// <param name="partialName">The partial view to apply.</param>
        /// <param name="onBeforeApply">
        /// Optional callback run immediately before each apply (e.g. to refresh
        /// the data the partial will display). Runs twice - once per apply -
        /// matching the existing RestoreSelectedTabPartial behavior.
        /// </param>
        /// <param name="onAfterApply">
        /// Optional callback run after each replacement layout is applied, including the
        /// deferred apply. Use this to restore child partials and publish their bindings.
        /// </param>
        /// <remarks>
        /// Public rather than protected: orchestrator helpers like GuiTabGroup
        /// live outside the ViewModel's own type hierarchy and need to call
        /// this from the outside, the same way ChangePartialView already is.
        /// </remarks>
        public void SwapNestedPartialView(string elementId, string partialName, Action onBeforeApply = null,
            Action onAfterApply = null)
        {
            _partialViewGenerations.TryGetValue(elementId, out var previousGeneration);
            var generation = previousGeneration + 1;
            _partialViewGenerations[elementId] = generation;
            var bindingGeneration = _bindingGeneration;
            var windowToken = WindowToken;

            void Apply()
            {
                if (bindingGeneration != _bindingGeneration || windowToken != WindowToken ||
                    _partialViewGenerations[elementId] != generation)
                    return;
                onBeforeApply?.Invoke();
                ChangePartialView(elementId, partialName);
                onAfterApply?.Invoke();
            }

            ChangePartialView("_window_", "%%WINDOW_MAIN%%");
            Apply();

            // The delayed re-apply can fire after the player has already closed (or
            // rapidly toggled) the window; NuiSetGroupLayout against a destroyed window
            // raises a client-side "element id not found" error. Only re-apply while
            // the window is still open.
            DelayCommand(0.0f, () =>
            {
                if (Gui.IsWindowOpen(Player, WindowType))
                    Apply();
            });
        }


        public string ModalPromptText
        {
            get => Get<string>();
            private set => Set(value);
        }

        public string ModalConfirmButtonText
        {
            get => Get<string>();
            private set => Set(value);
        }

        public string ModalCancelButtonText
        {
            get => Get<string>();
            private set => Set(value);
        }

        // Public setter (unlike the other modal properties): the client pushes typed
        // text back through the watch pipeline, which sets this via reflection.
        public string ModalInputText
        {
            get => Get<string>();
            set => Set(value);
        }

        private Action _callerConfirmAction;
        private Action _callerCancelAction;

        public Action OnModalClose() => () =>
        {
            // Reset to default values.
            ModalPromptText = "Are you sure?";

            ModalConfirmButtonText = "Yes";
            ModalCancelButtonText = "No";
            ModalInputText = string.Empty;

            _callerConfirmAction = null;
            _callerCancelAction = null;
        };

        /// <summary>
        /// Called after ANY modal (ShowModal / ShowInputModal) closes - confirm or
        /// cancel - after the caller's confirm/cancel action has run. Closing a
        /// modal swaps %%WINDOW_MAIN%% back into the root, which wipes any partial
        /// currently applied to a nested element (e.g. the selected tab's content).
        /// Tabbed windows should override this and re-apply their current tab
        /// partial. Default: no-op.
        /// </summary>
        protected virtual void OnModalClosedRestore()
        {
        }

        /// <summary>
        /// Called immediately after the static main view is restored. Windows that
        /// install runtime-generated nested layouts should reapply them here.
        /// </summary>
        protected virtual void OnMainViewRestored()
        {
        }

        private void CloseModalAndRestore(Action callerAction)
        {
            ChangePartialView("_window_", "%%WINDOW_MAIN%%");
            OnMainViewRestored();
            try
            {
                callerAction?.Invoke();
            }
            finally
            {
                // A failed confirm action must not leave a tabbed window with
                // its nested content erased by the root-modal swap.
                OnModalClosedRestore();
            }
        }

        public Action OnModalConfirmClick() => () =>
        {
            CloseModalAndRestore(_callerConfirmAction);
        };

        public Action OnModalCancelClick() => () =>
        {
            CloseModalAndRestore(_callerCancelAction);
        };

        public Action OnInputModalConfirmClick() => () =>
        {
            CloseModalAndRestore(_callerConfirmAction);
        };

        public Action OnInputModalCancelClick() => () =>
        {
            CloseModalAndRestore(_callerCancelAction);
        };

        /// <summary>
        /// Default implementation for OnWindowClosed.
        /// Override in derived classes to provide custom cleanup logic.
        /// </summary>
        public virtual Action OnWindowClosed() => () => { };

        // The following method works around a NUI issue where the new partial view won't display on screen until the window resizes.
        // We force a change to the geometry of the window to ensure it redraws appropriately.
        // If/when a fix is implemented by Beamdog, this can be removed.
        private void ApplyRefreshBugFix()
        {
            if (Geometry == null)
                return;

            Geometry.Height++;
            NuiSetBind(Player, WindowToken, nameof(Geometry), Geometry.ToJson());

            DelayCommand(0.0f, () =>
            {
                Geometry.Height--;
                NuiSetBind(Player, WindowToken, nameof(Geometry), Geometry.ToJson());
            });
        }
    }
}
