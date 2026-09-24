using System.ComponentModel.DataAnnotations;

namespace DB.Entity.Enum
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this System.Enum value)
        {
            if (value == null) return string.Empty;

            var fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return value.ToString();

            var displayAttribute = fieldInfo
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttribute?.Name ?? value.ToString();
        }
    }
}
