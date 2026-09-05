using ArkKeeper.Core.Profiles;

namespace ArkKeeper.Core.Launch;

/// <summary>Builds the ShooterGameServer command line for a <see cref="ServerProfile"/>.
/// Most settings live in GameUserSettings.ini/Game.ini (see <see cref="ServerProfile.ToGameUserSettings"/>);
/// this only covers the handful of things ARK reads from the launch command itself
/// (map, ports, mods) — notably NOT session name or passwords, see the comment below.</summary>
public static class LaunchArgumentsBuilder
{
    public static string Build(ServerProfile profile)
    {
        // SessionName/ServerPassword/ServerAdminPassword deliberately do NOT go on the command
        // line — they're already written to GameUserSettings.ini by WriteConfigFiles() (called
        // right before every Start()), matching the original tool's own GetServerArgs(), which
        // never put them here either. Putting them here was a real bug: unlike SessionName (which
        // was at least quoted), ServerPassword/AdminPassword were not, so any password containing
        // a space would fracture this whole url-parameter blob into multiple command-line
        // arguments and corrupt the launch — and either way, a password is then visible in plain
        // text to anything that can read this process's command line (Task Manager, WMI, ...).
        var urlParameters = new List<string>
        {
            profile.MapName,
            "listen",
        };

        urlParameters.Add($"Port={profile.Port}");
        urlParameters.Add($"QueryPort={profile.QueryPort}");

        // ARK's dedicated server is well known to ignore MaxPlayers when it's only set in
        // GameUserSettings.ini — the original tool this is a modernization of always puts it on
        // the launch command line too (confirmed in its own GetServerArgs()), which is what
        // actually makes it take effect. Reported directly: "El maximo de jugadores no se aplica."
        urlParameters.Add($"MaxPlayers={profile.MaxPlayers}");

        if (!string.IsNullOrWhiteSpace(profile.ServerIP))
        {
            // Binds the server to one specific network interface — opt-in only (most servers
            // leave this blank and let ARK listen on all interfaces), matching the original
            // tool's own guard around this same parameter.
            urlParameters.Add($"MultiHome={profile.ServerIP}");
        }

        var flags = new List<string> { "-server", "-log" };

        if (profile.RconEnabled)
        {
            flags.Add($"-RCONPort={profile.RconPort}");
            flags.Add("-RCONEnabled=True");
        }

        if (profile.ModIds.Count > 0)
        {
            flags.Add($"-mods={string.Join(',', profile.ModIds)}");
        }

        if (profile.DisableBattlEye)
        {
            flags.Add("-NoBattlEye");
        }

        return string.Join('?', urlParameters) + " " + string.Join(' ', flags);
    }
}
