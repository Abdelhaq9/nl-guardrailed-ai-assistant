using System.Globalization;

namespace GuardrailedAiAssistant.Tools;

public sealed class WorldTimeTool
{
    public string GetCityTime(string city)
    {
        city = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(city.Trim().ToLowerInvariant());

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Zurich", "Central European Standard Time"},
            {"Geneva", "Central European Standard Time"},
            {"London", "GMT Standard Time"},
            {"New York", "Eastern Standard Time"},
            {"Tokyo", "Tokyo Standard Time"},
            {"Sydney", "AUS Eastern Standard Time"}
        };

        if (!map.TryGetValue(city, out var tzId))
            return "I don't know.";

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var local = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            return $"It is {local:HH:mm} in {city}.";
        }
        catch
        {
            return "I don't know.";
        }
    }
}
