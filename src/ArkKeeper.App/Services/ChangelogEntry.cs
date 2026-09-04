namespace ArkKeeper.App.Services;

/// <summary>One shipped version's headline changes, shown in Settings' "Version history" section.
/// Newest first — add a new entry here (not edit an old one) whenever a version ships.</summary>
public sealed record ChangelogEntry(string Version, string Date, IReadOnlyList<string> Highlights);

public static class Changelog
{
    public static IReadOnlyList<ChangelogEntry> Entries { get; } =
    [
        new ChangelogEntry("1.0.0", "2026-09-04",
        [
            "First release — full server management covering all ~226 GameUserSettings.ini/Game.ini settings, real start/stop/kill process control, and config that's actually written to the server's own files.",
            "RCON console, players & tribes, world backup/restore, scheduled RCON tasks, Steam Workshop mods, and Discord notifications.",
            "Anti-cheat toggle and per-server CPU priority/core-affinity tuning.",
            "Three themes (Light, OLED Black, Navy Blue) with five accent colors, a Mica-backed window, and light motion throughout.",
        ]),
    ];
}
