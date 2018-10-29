using System;
using System.Linq;

namespace SWLOR.Tools.Editor.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Returns the first attribute of a given type on an object.
        /// If there are more than one of a given attribute type, only one will be returned and the rest will be ignored.
        /// </summary>
        /// <typeparam name="T">The type of attribute</typeparam>
        /// <param name="obj">The object to retrieve from</param>
        /// <returns></returns>
        public static T GetAttributeByType<T>(this object obj)
            where T: Attribute
        {
            T attr = (T)obj.GetType().GetCustomAttributes(typeof(T), false).FirstOrDefault();

            if (attr == null)
            {
                throw new NullReferenceException("Unable to locate attribute of type " + nameof(T) + " on object.");
            }

            return attr;
        }
    }
}
