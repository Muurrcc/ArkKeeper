namespace ArkKeeper.Core.Ini;

/// <summary>Which of ARK's config files a setting belongs to.</summary>
public enum IniFile
{
    GameUserSettings,
    Game,
}

/// <summary>
/// Maps a <see cref="Profiles.ServerProfile"/> property to a section/key pair in one
/// of ARK's config files (GameUserSettings.ini or Game.ini).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IniSettingAttribute : Attribute
{
    public IniSettingAttribute(IniFile file, string section, string key)
    {
        File = file;
        Section = section;
        Key = key;
    }

    public IniFile File { get; }

    public string Section { get; }

    public string Key { get; }
}
