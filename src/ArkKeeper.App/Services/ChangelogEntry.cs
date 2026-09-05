namespace ArkKeeper.App.Services;

/// <summary>One shipped version's headline changes, shown in Settings' "Version history" section.
/// Newest first — add a new entry here (not edit an old one) whenever a version ships.</summary>
public sealed record ChangelogEntry(string Version, string Date, IReadOnlyList<string> Highlights);

public static class Changelog
{
    public static IReadOnlyList<ChangelogEntry> Entries { get; } =
    [
        new ChangelogEntry("1.2.0", "2026-09-05",
        [
            "Added automatic scheduled world backups (interval or daily-at, with compression and an old-backup keep count), configurable from the Backups page.",
            "Fixed several crashes/data-loss bugs: a corrupted profile, settings, or schedule file could crash the whole app on launch; a corrupted backup could wipe a server's live world with nothing to restore; a locked save file could hide every player/tribe on the Players page.",
            "Fixed rare IOExceptions when saving profiles, schedules, or settings in quick succession (e.g. rapid mod add/remove).",
            "Fixed server passwords being duplicated onto the launch command line unquoted — a password containing a space could corrupt the server's launch, and any password was visible to anything reading the process's command line.",
        ]),
        new ChangelogEntry("1.1.0", "2026-09-04",
        [
            "Redesigned Dashboard: real CPU/RAM usage per server with trend sparklines, an activity feed of real events (starts, stops, backups), and Resource Metrics cards.",
            "Fixed mods never actually reaching the server — Workshop downloads used the wrong Steam app id and were never copied into ShooterGame/Content/Mods, so a configured mod silently did nothing.",
            "Fixed the Dashboard's CPU% readout appearing frozen while its trend graph kept moving.",
            "Fixed switching back to the Light theme leaving every card tinted with the previous theme's color.",
        ]),
        new ChangelogEntry("1.0.0", "2026-09-04",
        [
            "First release — full server management covering all ~226 GameUserSettings.ini/Game.ini settings, real start/stop/kill process control, and config that's actually written to the server's own files.",
            "RCON console, players & tribes, world backup/restore, scheduled RCON tasks, Steam Workshop mods, and Discord notifications.",
            "Anti-cheat toggle and per-server CPU priority/core-affinity tuning.",
            "Three themes (Light, OLED Black, Navy Blue) with five accent colors, a Mica-backed window, and light motion throughout.",
        ]),
    ];
}
