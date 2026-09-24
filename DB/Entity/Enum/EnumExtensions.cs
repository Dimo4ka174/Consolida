using System.ComponentModel.DataAnnotations;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        if (value == null) return string.Empty;

        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null) return value.ToString();

        var displayAttribute = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                                        .FirstOrDefault() as DisplayAttribute;

        return displayAttribute?.Name ?? value.ToString();
    }
}