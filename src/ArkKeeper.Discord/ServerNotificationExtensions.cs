namespace ArkKeeper.Discord;

/// <summary>Common ArkKeeper server events, pre-formatted as Discord embeds.</summary>
public static class ServerNotificationExtensions
{
    private const int GreenColor = 0x2ECC71;
    private const int RedColor = 0xE74C3C;
    private const int BlueColor = 0x3498DB;

    public static Task NotifyServerStartedAsync(this DiscordWebhookNotifier notifier, string sessionName, CancellationToken cancellationToken = default) =>
        notifier.SendAsync(null, new DiscordEmbed("Server started", sessionName, GreenColor), cancellationToken);

    public static Task NotifyServerStoppedAsync(this DiscordWebhookNotifier notifier, string sessionName, CancellationToken cancellationToken = default) =>
        notifier.SendAsync(null, new DiscordEmbed("Server stopped", sessionName, RedColor), cancellationToken);

    public static Task NotifyPlayerJoinedAsync(this DiscordWebhookNotifier notifier, string sessionName, string playerName, CancellationToken cancellationToken = default) =>
        notifier.SendAsync(null, new DiscordEmbed($"{playerName} joined", sessionName, BlueColor), cancellationToken);

    public static Task NotifyPlayerLeftAsync(this DiscordWebhookNotifier notifier, string sessionName, string playerName, CancellationToken cancellationToken = default) =>
        notifier.SendAsync(null, new DiscordEmbed($"{playerName} left", sessionName, BlueColor), cancellationToken);
}
