using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Serializable]
    public abstract class DBObjectViewModelBase
        : PropertyChangedBase, IDBObjectViewModel
    {
        private readonly Dictionary<string, object> _trackedProperties;

        protected DBObjectViewModelBase()
        {
            InternalEditorID = Guid.NewGuid().ToString();
            _trackedProperties = new Dictionary<string, object>();
            PropertyChanged += OnPropertyChanged;
        }

        private string _internalEditorID;
        [JsonProperty(nameof(InternalEditorID))]
        public string InternalEditorID
        {
            get => _internalEditorID;
            set
            {
                _internalEditorID = value;
                NotifyOfPropertyChange(() => InternalEditorID);
            }
        }

        private string _fileName;

        [JsonIgnore]
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                NotifyOfPropertyChange(() => FileName);
            }
        }

        public abstract string DisplayName { get; set; }
        
        private bool _isDirty;

        [JsonIgnore]
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                NotifyOfPropertyChange(() => IsDirty);

                OnDirty?.Invoke(this, new EventArgs());
            }
        }

        private PropertyInfo ValidateProperty<TProperty>(
            Expression<TProperty> propertyLambda)
        {
            Type type = GetType();

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a property that is not from type {type}.");


            return propInfo;
        }


        protected void TrackProperty<T, TProperty>(T obj, Expression<Func<T, TProperty>> expression)
        {
            var propInfo = ValidateProperty(expression);
            _trackedProperties.Add(propInfo.Name, propInfo.GetValue(obj));
        }

        public virtual void DiscardChanges()
        {
            foreach (var tracked in _trackedProperties)
            {
                var type = GetType();
                var prop = type.GetProperty(tracked.Key);
                prop.SetValue(this, tracked.Value);
            }

            IsDirty = false;
        }

        public virtual void RefreshTrackedProperties()
        {
            foreach (var tracked in _trackedProperties.Keys.ToList())
            {
                var type = GetType();
                var prop = type.GetProperty(tracked);
                var value = prop.GetValue(this);
                
                _trackedProperties[tracked] = value;
            }
        }
        
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_trackedProperties.ContainsKey(e.PropertyName))
            {
                var oldValue = _trackedProperties[e.PropertyName];
                var newValue = GetType().GetProperty(e.PropertyName).GetValue(this);

                if (oldValue != newValue)
                {
                    IsDirty = true;
                }
            }
        }

        public event EventHandler OnDirty;
    }
}
