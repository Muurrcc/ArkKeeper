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

        return string.Join('?', urlParameters) + " " + string.Join(' ', flags);
    }
}
