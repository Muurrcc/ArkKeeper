using Avalonia;

namespace ArkKeeper.App.Services;

/// <summary>Turns a rolling window of numeric samples into normalized points for an Avalonia
/// <c>Polyline</c> — used by the Dashboard's CPU/RAM trend cards.</summary>
public static class SparklineMath
{
    public static IReadOnlyList<Point> ToPoints(IReadOnlyList<double> values, double width, double height, double maxValue)
    {
        if (values.Count == 0)
        {
            return [new Point(0, height), new Point(width, height)];
        }

        if (values.Count == 1)
        {
            var y = SingleY(values[0], height, maxValue);
            return [new Point(0, y), new Point(width, y)];
        }

        var points = new List<Point>(values.Count);
        var stepX = width / (values.Count - 1);
        for (var i = 0; i < values.Count; i++)
        {
            points.Add(new Point(i * stepX, SingleY(values[i], height, maxValue)));
        }

        return points;
    }

    private static double SingleY(double value, double height, double maxValue)
    {
        var normalized = maxValue <= 0 ? 0 : Math.Clamp(value / maxValue, 0, 1);
        // Inverted: SVG/Avalonia y grows downward, but a higher value should draw higher up.
        return height - (normalized * height);
    }
}
