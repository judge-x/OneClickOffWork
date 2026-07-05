using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneClickOffWork.Services;

public sealed class WeatherService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public Task<string> GetCurrentWeatherTextAsync()
        => GetCurrentWeatherTextAsync("北京");

    public async Task<string> GetCurrentWeatherTextAsync(string city)
    {
        var snapshot = await GetCurrentWeatherAsync(city);
        return snapshot.Summary;
    }

    public async Task<WeatherSnapshot> GetCurrentWeatherAsync(string city)
    {
        try
        {
            var requestedCity = string.IsNullOrWhiteSpace(city) ? "北京" : city.Trim();
            var location = await GetLocationByCityAsync(requestedCity);
            if (location is null)
            {
                return WeatherSnapshot.Unavailable("城市未找到", requestedCity);
            }

            var weatherUrl =
                string.Create(CultureInfo.InvariantCulture,
                    $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&current=temperature_2m,weather_code&timezone=auto");
            using var weatherResponse = await Client.GetAsync(weatherUrl);
            weatherResponse.EnsureSuccessStatusCode();
            await using var weatherStream = await weatherResponse.Content.ReadAsStreamAsync();
            var weather = await JsonSerializer.DeserializeAsync<OpenMeteoResult>(weatherStream, JsonOptions());

            if (weather?.Current is null)
            {
                return WeatherSnapshot.Unavailable("天气不可用", location.DisplayName);
            }

            var description = DescribeWeather(weather.Current.WeatherCode);
            return new WeatherSnapshot(
                location.DisplayName,
                description,
                $"{weather.Current.Temperature:0.#}°C",
                IconFor(weather.Current.WeatherCode));
        }
        catch
        {
            return WeatherSnapshot.Unavailable("自动获取失败");
        }
    }

    private static async Task<CityLocation?> GetLocationByCityAsync(string city)
    {
        var geocodingUrl =
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=zh&format=json";
        using var response = await Client.GetAsync(geocodingUrl);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<OpenMeteoGeocodingResult>(stream, JsonOptions());
        var location = result?.Results?.FirstOrDefault();

        if (location is null)
        {
            return null;
        }

        var displayName = string.IsNullOrWhiteSpace(location.Admin1)
            ? location.Name
            : $"{location.Admin1}·{location.Name}";

        return new CityLocation(
            displayName,
            location.Latitude,
            location.Longitude);
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private static string DescribeWeather(int code)
    {
        return code switch
        {
            0 => "晴",
            1 or 2 => "多云",
            3 => "阴",
            45 or 48 => "雾",
            51 or 53 or 55 or 56 or 57 => "毛毛雨",
            61 or 63 or 65 or 66 or 67 => "雨",
            71 or 73 or 75 or 77 => "雪",
            80 or 81 or 82 => "阵雨",
            85 or 86 => "阵雪",
            95 or 96 or 99 => "雷雨",
            _ => "天气"
        };
    }

    private static string IconFor(int code)
    {
        return code switch
        {
            0 => "☀️",
            1 or 2 => "🌤️",
            3 => "☁️",
            45 or 48 => "🌫️",
            51 or 53 or 55 or 56 or 57 => "🌦️",
            61 or 63 or 65 or 66 or 67 => "🌧️",
            71 or 73 or 75 or 77 => "❄️",
            80 or 81 or 82 => "🌦️",
            85 or 86 => "🌨️",
            95 or 96 or 99 => "⛈️",
            _ => "🌡️"
        };
    }

    private sealed class OpenMeteoResult
    {
        public CurrentWeather? Current { get; set; }
    }

    private sealed class OpenMeteoGeocodingResult
    {
        public List<OpenMeteoLocation>? Results { get; set; }
    }

    private sealed class OpenMeteoLocation
    {
        public string Name { get; set; } = "";

        [JsonPropertyName("admin1")]
        public string Admin1 { get; set; } = "";

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    private sealed record CityLocation(string DisplayName, double Latitude, double Longitude);

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
}

public sealed record WeatherSnapshot(string City, string Description, string Temperature, string Icon, bool IsAvailable = true)
{
    public string Summary => string.IsNullOrWhiteSpace(City)
        ? $"天气：{Description}"
        : $"{City}：{Description} {Temperature}";

    public static WeatherSnapshot Unavailable(string reason, string city = "")
        => new(city, reason, "--", "🔄", false);
}
