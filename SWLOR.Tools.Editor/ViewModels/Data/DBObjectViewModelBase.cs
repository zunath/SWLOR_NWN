using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Serializable]
    public abstract class DBObjectViewModelBase: PropertyChangedBase, IDBObjectViewModel
    {
        private Dictionary<string, object> _originalValues;

        protected DBObjectViewModelBase()
        {
            _originalValues = new Dictionary<string, object>();
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

        public void TrackProperty<TSource, TProperty>(
            TSource source,
            Expression<Func<TSource, TProperty>> propertyLambda)
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

            _originalValues.Add(propInfo.Name, propInfo.GetValue(this));
        }


        public virtual void DiscardChanges()
        {
            foreach (var prop in _originalValues)
            {
                GetType().GetProperty(prop.Key).SetValue(this, prop.Value);
            }            
        }

        public virtual void SetTrackedValues()
        {
            foreach (var prop in _originalValues.Keys.ToList())
            {
                var value = GetType().GetProperty(prop).GetValue(this);
                _originalValues[prop] = value;
            }
        }

        public event EventHandler OnDirty;
    }
}
