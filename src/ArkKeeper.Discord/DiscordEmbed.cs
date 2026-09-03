namespace ArkKeeper.Discord;

/// <summary>A minimal Discord embed — just the fields ArkKeeper's server notifications need.</summary>
public sealed record DiscordEmbed(string Title, string Description, int ColorHex = 0x2ECC71);
