using System.Globalization;
using System.Reflection;

namespace ArkKeeper.Core.Ini;

/// <summary>
/// Reads and writes POCOs whose properties are decorated with <see cref="IniSettingAttribute"/>,
/// so a type like <see cref="Profiles.ServerProfile"/> can round-trip through the game's own
/// GameUserSettings.ini / Game.ini files.
/// </summary>
public static class IniSerializer
{
    public static void Apply(object target, IniFile file, IniDocument document)
    {
        foreach (var property in GetIniProperties(target.GetType(), file))
        {
            var (attribute, _) = property;
            var section = document.FindSection(attribute.Section);
            var raw = section?.GetSingle(attribute.Key);
            if (raw is null)
            {
                continue;
            }

            var converted = ConvertFromIni(raw, property.Property.PropertyType);
            if (converted is not null)
            {
                property.Property.SetValue(target, converted);
            }
        }
    }

    public static IniDocument Write(object source, IniFile file)
    {
        var document = new IniDocument();

        foreach (var (attribute, property) in GetIniProperties(source.GetType(), file))
        {
            var value = property.GetValue(source);
            if (value is null)
            {
                continue;
            }

            var section = document.GetOrAddSection(attribute.Section);
            section.SetSingle(attribute.Key, ConvertToIni(value));
        }

        return document;
    }

    private static IEnumerable<(IniSettingAttribute Attribute, PropertyInfo Property)> GetIniProperties(Type type, IniFile file) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Attribute: p.GetCustomAttribute<IniSettingAttribute>(), Property: p))
            .Where(p => p.Attribute is not null && p.Attribute.File == file)
            .Select(p => (p.Attribute!, p.Property));

    private static string ConvertToIni(object value) => value switch
    {
        bool b => b.ToString(),
        float f => f.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString(CultureInfo.InvariantCulture),
        Enum e => e.ToString(),
        _ => value.ToString() ?? string.Empty,
    };

    private static object? ConvertFromIni(string raw, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return raw;
        }

        if (targetType == typeof(bool))
        {
            return bool.TryParse(raw, out var b) ? b : null;
        }

        if (targetType == typeof(int))
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;
        }

        if (targetType == typeof(float))
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : null;
        }

        if (targetType == typeof(double))
        {
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        if (targetType.IsEnum)
        {
            return Enum.TryParse(targetType, raw, ignoreCase: true, out var e) ? e : null;
        }

        return null;
    }
}
