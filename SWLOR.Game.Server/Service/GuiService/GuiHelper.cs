using NRediSearch.QueryBuilder;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SWLOR.Game.Server.Service.GuiService
{
    public static class GuiHelper<T>
        where T: IGuiViewModel
    {
        /// <summary>
        /// Retrieves the name of the property targeted in an expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of property being targeted.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        /// <returns>The name of the property.</returns>
        public static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var type = typeof(T);
            
            var member = expression.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{expression}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{expression}' refers to a field, not a property.");

            //if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
            //    throw new ArgumentException($"Expression '{expression}' refers to a property that is not from type {type}.");

            return propInfo.Name;
        }

        /// <summary>
        /// Retrieves the method info for a given targeted action.
        /// </summary>
        /// <typeparam name="TMethod">The type of method being targeted.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        /// <returns>Method info of the targeted action.</returns>
        public static GuiMethodDetail GetMethodInfo<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            var method = body.Method;
            var values = new List<KeyValuePair<Type, object>>();

            foreach (var argument in body.Arguments)
            {
                var value = Expression.Lambda(argument).Compile().DynamicInvoke();
                values.Add(new KeyValuePair<Type, object>(value.GetType(), value));
            }

            return new GuiMethodDetail(method, values);
        }
    }
}
