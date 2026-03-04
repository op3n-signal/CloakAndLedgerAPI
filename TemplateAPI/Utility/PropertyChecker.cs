using System.Reflection;

namespace Template.Utility
{
    public class PropertyChecker
    {
        public static bool PropertyHasValue<T>(T obj, string propertyName)
        {
            if (obj == null) return false;

            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) return false;

            var value = prop.GetValue(obj);

            if (value is string s && string.IsNullOrWhiteSpace(s)) return false;

            return true;
        }
    }
}
