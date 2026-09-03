namespace ArkKeeper.Discord;

internal sealed record WebhookPayload(string? Content, WebhookEmbed[]? Embeds);

internal sealed record WebhookEmbed(string Title, string Description, int Color);
