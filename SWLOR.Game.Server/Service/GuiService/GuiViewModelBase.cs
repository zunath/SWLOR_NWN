using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Annotations;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiViewModelBase<TDerived, TPayload>: IGuiViewModel, INotifyPropertyChanged
        where TDerived: GuiViewModelBase<TDerived, TPayload>
        where TPayload: GuiPayloadBase
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
        protected T Get<T>([CallerMemberName]string propertyName = null)
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
        protected void Set<T>(T value, [CallerMemberName]string propertyName = null)
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

            if(!_propertyValues[propertyName].SkipNotify)
                OnPropertyChanged(propertyName);
        }

        private void OnListChanged(object sender, ListChangedEventArgs e)
        {
            var list = ((IGuiBindingList) sender);
            OnPropertyChanged(list.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies subscribers of changes.
        /// </summary>
        /// <param name="propertyName">The name of the property to notify about.</param>
        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
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
                var json = _converter.ToJson(propertyDetail.Value);
                NuiSetBind(Player, WindowToken, name, json);
            }

            WatchOnClient(model => model.Geometry);

            ChangePartialView("_window_", "%%WINDOW_MAIN%%");
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

            _propertyValues[propertyName].Value = value;

            _propertyValues[propertyName].SkipNotify = true;
            GetType().GetProperty(propertyName)?.SetValue(this, value);
            _propertyValues[propertyName].SkipNotify = false;
        }

        /// <summary>
        /// Watches a property on the client.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        protected void WatchOnClient<TProperty>(Expression<Func<TDerived, TProperty>> expression)
        {
            var propertyName = GuiHelper<TDerived>.GetPropertyName(expression);
            if (!_propertyValues.ContainsKey(propertyName))
                _propertyValues[propertyName] = new PropertyDetail();
            
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

        /// <inheritdoc />
        public void ChangePartialView(string elementId, string partialName)
        {
            var window = Gui.GetWindowTemplate(WindowType);
            var partial = window.PartialViews[partialName];
            NuiSetGroupLayout(Player, WindowToken, elementId, partial);
            
            ApplyRefreshBugFix();
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

        private Action _callerConfirmAction;
        private Action _callerCancelAction;

        public Action OnModalClose() => () =>
        {
            // Reset to default values.
            ModalPromptText = "Are you sure?";

            ModalConfirmButtonText = "Yes";
            ModalCancelButtonText = "No";

            _callerConfirmAction = null;
            _callerCancelAction = null;
        };

        public Action OnModalConfirmClick() => () =>
        {
            ChangePartialView("_window_", "%%WINDOW_MAIN%%");
            
            if (_callerConfirmAction != null)
                _callerConfirmAction();
        };

        public Action OnModalCancelClick() => () =>
        {
            ChangePartialView("_window_", "%%WINDOW_MAIN%%");

            if (_callerCancelAction != null)
                _callerCancelAction();
        };

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
