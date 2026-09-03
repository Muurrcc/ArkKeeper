using Avalonia.Media;

namespace ArkKeeper.App.Services;

public sealed record AccentSwatch(string Name, Color Color)
{
    public IBrush Brush => new SolidColorBrush(Color);


    public static readonly IReadOnlyList<AccentSwatch> Presets = new[]
    {
        new AccentSwatch("Ark Teal", Color.Parse("#0FC2C0")),
        new AccentSwatch("Amber", Color.Parse("#F7A828")),
        new AccentSwatch("Violet", Color.Parse("#7A4FE0")),
        new AccentSwatch("Rose", Color.Parse("#E0507A")),
        new AccentSwatch("Windows Blue", Color.Parse("#0078D4")),
    };
}
