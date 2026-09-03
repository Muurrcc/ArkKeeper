using System.Collections;
using System.Globalization;
using System.Reflection;

namespace ArkKeeper.Core.Ini;

/// <summary>
/// Reads and writes POCOs whose properties are decorated with <see cref="IniSettingAttribute"/>,
/// so a type like <see cref="Profiles.ServerProfile"/> can round-trip through the game's own
/// GameUserSettings.ini / Game.ini files.
///
/// Scalar properties (string/bool/int/float/double/enum) map to a single "Key=Value" line.
/// <c>List&lt;T&gt;</c> properties map to the key repeated once per list item — this is how ARK
/// itself stores "override list" settings (one line per dino/item/engram override etc.), and
/// IniDocument already preserves repeated keys in order for exactly this. Each item is treated
/// as an opaque value of type T: for the structured overrides (dino class multipliers, engram
/// overrides, supply crate loot...) T is `string` and the value is the whole
/// "(ClassName=...,Multiplier=...)"-shaped text verbatim — this doesn't parse the fields inside
/// each entry, just preserves them, which is enough to round-trip correctly through the real
/// game files without ArkKeeper needing to understand every override struct's exact shape.
/// </summary>
public static class IniSerializer
{
    public static void Apply(object target, IniFile file, IniDocument document)
    {
        foreach (var property in GetIniProperties(target.GetType(), file))
        {
            var (attribute, propertyInfo) = property;
            var section = document.FindSection(attribute.Section);
            if (section is null)
            {
                continue;
            }

            if (TryGetListElementType(propertyInfo.PropertyType, out var elementType))
            {
                var list = CreateTypedList(elementType, section.GetAll(attribute.Key));
                propertyInfo.SetValue(target, list);
                continue;
            }

            var raw = section.GetSingle(attribute.Key);
            if (raw is null)
            {
                continue;
            }

            var converted = ConvertFromIni(raw, propertyInfo.PropertyType);
            if (converted is not null)
            {
                propertyInfo.SetValue(target, converted);
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

            if (value is IEnumerable enumerable and not string)
            {
                section.RemoveAll(attribute.Key);
                foreach (var item in enumerable)
                {
                    if (item is not null)
                    {
                        section.Add(attribute.Key, ConvertToIni(item));
                    }
                }
                continue;
            }

            section.SetSingle(attribute.Key, ConvertToIni(value));
        }

        return document;
    }

    private static IEnumerable<(IniSettingAttribute Attribute, PropertyInfo Property)> GetIniProperties(Type type, IniFile file) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Attribute: p.GetCustomAttribute<IniSettingAttribute>(), Property: p))
            .Where(p => p.Attribute is not null && p.Attribute.File == file)
            .Select(p => (p.Attribute!, p.Property));

    private static bool TryGetListElementType(Type propertyType, out Type elementType)
    {
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            elementType = propertyType.GetGenericArguments()[0];
            return true;
        }

        elementType = typeof(object);
        return false;
    }

    private static IList CreateTypedList(Type elementType, IEnumerable<string> rawValues)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var raw in rawValues)
        {
            var converted = ConvertFromIni(raw, elementType);
            if (converted is not null)
            {
                list.Add(converted);
            }
        }

        return list;
    }

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
