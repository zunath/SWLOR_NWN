using System;
using System.Linq.Expressions;
using System.Reflection;

namespace SWLOR.Game.Server.Service.GuiService
{
    public static class GuiHelper<T>
        where T: IGuiViewModel
    {
        public static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var type = typeof(T);
            
            var member = expression.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{expression}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{expression}' refers to a field, not a property.");

            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException($"Expression '{expression}' refers to a property that is not from type {type}.");

            return propInfo.Name;
        }

        public static MethodInfo GetMethodInfo<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            var constantExpression = (MethodCallExpression)expression.Body;
            var method = constantExpression.Method;

            return method;
        }

    }
}
