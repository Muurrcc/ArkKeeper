using ArkKeeper.Core.Profiles;

namespace ArkKeeper.Core.Launch;

/// <summary>Builds the ShooterGameServer command line for a <see cref="ServerProfile"/>.
/// Most settings live in GameUserSettings.ini/Game.ini (see <see cref="ServerProfile.ToGameUserSettings"/>);
/// this only covers the handful of things ARK reads from the launch command itself
/// (map, session identity, ports, mods).</summary>
public static class LaunchArgumentsBuilder
{
    public static string Build(ServerProfile profile)
    {
        var urlParameters = new List<string>
        {
            profile.MapName,
            "listen",
            $"SessionName=\"{profile.SessionName}\"",
        };

        if (!string.IsNullOrEmpty(profile.ServerPassword))
        {
            urlParameters.Add($"ServerPassword={profile.ServerPassword}");
        }

        urlParameters.Add($"ServerAdminPassword={profile.AdminPassword}");
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
