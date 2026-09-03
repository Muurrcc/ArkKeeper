namespace ArkKeeper.Core.Ini;

/// <summary>
/// A minimal INI reader/writer that matches the quirks of ARK's config files:
/// keys may repeat within a section (list-style settings such as
/// ConfigOverrideItemMaxQuantity), and section/key order must be preserved
/// on write so diffs against the game's own files stay small.
/// </summary>
public sealed class IniDocument
{
    private readonly List<IniSection> _sections = new();

    public IReadOnlyList<IniSection> Sections => _sections;

    public IniSection GetOrAddSection(string name)
    {
        var existing = _sections.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var section = new IniSection(name);
        _sections.Add(section);
        return section;
    }

    public IniSection? FindSection(string name) =>
        _sections.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static IniDocument Parse(string text)
    {
        var document = new IniDocument();
        IniSection? current = null;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();

            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = document.GetOrAddSection(line[1..^1]);
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            current ??= document.GetOrAddSection(string.Empty);
            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            current.Add(key, value);
        }

        return document;
    }

    public override string ToString()
    {
        var writer = new System.Text.StringBuilder();
        foreach (var section in _sections)
        {
            writer.Append('[').Append(section.Name).Append(']').Append('\n');
            foreach (var entry in section.Entries)
            {
                writer.Append(entry.Key).Append('=').Append(entry.Value).Append('\n');
            }
            writer.Append('\n');
        }
        return writer.ToString();
    }
}

public sealed class IniSection
{
    private readonly List<KeyValuePair<string, string>> _entries = new();

    public IniSection(string name) => Name = name;

    public string Name { get; }

    public IReadOnlyList<KeyValuePair<string, string>> Entries => _entries;

    public void Add(string key, string value) => _entries.Add(new(key, value));

    public void RemoveAll(string key) =>
        _entries.RemoveAll(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public void SetSingle(string key, string value)
    {
        RemoveAll(key);
        Add(key, value);
    }

    public string? GetSingle(string key) =>
        _entries.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;

    public IEnumerable<string> GetAll(string key) =>
        _entries.Where(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Select(e => e.Value);
}
