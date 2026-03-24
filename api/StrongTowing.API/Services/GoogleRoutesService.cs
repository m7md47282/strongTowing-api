using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StrongTowing.API.Options;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;

namespace StrongTowing.API.Services;

public interface IGoogleRoutesService
{
    Task<RouteResponseDto?> ComputeRouteAsync(RouteRequestDto request, CancellationToken cancellationToken = default);
}

public class GoogleRoutesService : IGoogleRoutesService
{
    private const string ComputeRoutesUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";
    private const double MetersToMiles = 0.000621371;

    private readonly HttpClient _httpClient;
    private readonly GoogleMapsOptions _mapsOptions;
    private readonly IOfficeLocationResolver _officeLocationResolver;
    private readonly ILogger<GoogleRoutesService> _logger;

    public GoogleRoutesService(
        HttpClient httpClient,
        IOptions<GoogleMapsOptions> mapsOptions,
        IOfficeLocationResolver officeLocationResolver,
        ILogger<GoogleRoutesService> logger)
    {
        _httpClient = httpClient;
        _mapsOptions = mapsOptions.Value;
        _officeLocationResolver = officeLocationResolver;
        _logger = logger;
    }

    public async Task<RouteResponseDto?> ComputeRouteAsync(RouteRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_mapsOptions.ApiKey))
        {
            _logger.LogWarning("Google Maps API key is not configured.");
            return null;
        }

        var (officeLat, officeLng) = await _officeLocationResolver.GetOfficeCoordinatesAsync(cancellationToken);

        var payload = new ComputeRoutesRequest
        {
            Origin = Waypoint.FromLatLng(officeLat, officeLng),
            Destination = Waypoint.FromLatLng(request.Latitude, request.Longitude),
            TravelMode = "DRIVE"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ComputeRoutesUrl);
        httpRequest.Headers.TryAddWithoutValidation("X-Goog-Api-Key", _mapsOptions.ApiKey);
        httpRequest.Headers.TryAddWithoutValidation("X-Goog-FieldMask", "routes.distanceMeters,routes.duration");
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        httpRequest.Content = JsonContent.Create(payload, options: jsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Google Routes API.");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Google Routes API returned {StatusCode}: {Body}", response.StatusCode, body);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var computeResponse = await JsonSerializer.DeserializeAsync<ComputeRoutesResponse>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, cancellationToken);

        var route = computeResponse?.Routes?.FirstOrDefault();
        if (route == null || route.DistanceMeters is null)
        {
            _logger.LogWarning("Google Routes API returned no routes.");
            return null;
        }

        var miles = Math.Round(route.DistanceMeters.Value * (decimal)MetersToMiles, 2);
        var durationMinutes = ParseDurationToMinutes(route.Duration);

        return new RouteResponseDto
        {
            DistanceInMiles = (double)miles,
            DurationInMinutes = durationMinutes
        };
    }

    private static int ParseDurationToMinutes(string? duration)
    {
        if (string.IsNullOrEmpty(duration))
            return 0;

        var s = duration.TrimEnd();
        if (s.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            s = s[..^1];

        if (!double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
            return 0;

        if (seconds <= 0)
            return 0;
        var minutes = (int)Math.Ceiling(seconds / 60.0);
        return Math.Max(1, minutes);
    }

    private sealed class ComputeRoutesRequest
    {
        [JsonPropertyName("origin")]
        public Waypoint? Origin { get; set; }

        [JsonPropertyName("destination")]
        public Waypoint? Destination { get; set; }

        [JsonPropertyName("travelMode")]
        public string TravelMode { get; set; } = "DRIVE";
    }

    private sealed class Waypoint
    {
        [JsonPropertyName("location")]
        public LatLngLocation? Location { get; set; }

        public static Waypoint FromLatLng(double lat, double lng) => new()
        {
            Location = new LatLngLocation
            {
                LatLng = new LatLng { Latitude = lat, Longitude = lng }
            }
        };
    }

    private sealed class LatLngLocation
    {
        [JsonPropertyName("latLng")]
        public LatLng? LatLng { get; set; }
    }

    private sealed class LatLng
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class ComputeRoutesResponse
    {
        [JsonPropertyName("routes")]
        public List<RouteItem>? Routes { get; set; }
    }

    private sealed class RouteItem
    {
        [JsonPropertyName("distanceMeters")]
        public int? DistanceMeters { get; set; }

        [JsonPropertyName("duration")]
        public string? Duration { get; set; }
    }
}
