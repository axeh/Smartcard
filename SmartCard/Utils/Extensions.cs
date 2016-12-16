using System;
using System.ComponentModel;
using System.Reflection;

namespace SmartCard.Utils
{
    internal static class Extensions
    {
        public static string GetDisplayAttributeFrom(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                        Attribute.GetCustomAttribute(field,
                            typeof(DescriptionAttribute)) as DescriptionAttribute;
                    return attr?.Description;
                }
            }
            return null;
        }
    }
}
