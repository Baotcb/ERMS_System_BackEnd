using ERMS.Application.Interface;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERMS.Infrastructure.Services;

public class ZoomService : IZoomService
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public ZoomService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _accountId = configuration["Zoom:AccountId"] ?? throw new ArgumentNullException("Zoom:AccountId");
        _clientId = configuration["Zoom:ClientId"] ?? throw new ArgumentNullException("Zoom:ClientId");
        _clientSecret = configuration["Zoom:ClientSecret"] ?? throw new ArgumentNullException("Zoom:ClientSecret");

        _httpClient.BaseAddress = new Uri("https://api.zoom.us/v2/");
    }

    public async Task<ZoomMeetingResponse> CreateMeetingAsync(ZoomMeetingRequest request, CancellationToken cancellationToken = default)
    {
        // Get OAuth token
        var token = await GetOAuthTokenAsync(cancellationToken);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var zoomRequest = new
        {
            topic = request.Topic,
            type = 2, // Scheduled meeting
            start_time = request.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            duration = request.Duration,
            timezone = request.Timezone,
            agenda = request.Agenda,
            settings = new
            {
                host_video = true,
                participant_video = true,
                join_before_host = false,
                mute_upon_entry = true,
                waiting_room = true,
                audio = "both",
                auto_recording = "none"
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"users/me/meetings", zoomRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        return new ZoomMeetingResponse
        {
            MeetingId = result.GetProperty("id").ToString(),
            JoinUrl = result.GetProperty("join_url").GetString() ?? string.Empty,
            StartUrl = result.GetProperty("start_url").GetString() ?? string.Empty,
            Password = result.TryGetProperty("password", out var pwd) ? pwd.GetString() ?? string.Empty : string.Empty
        };
    }

    private async Task<string> GetOAuthTokenAsync(CancellationToken cancellationToken)
    {
        using var tokenClient = new HttpClient();

        var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        tokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "account_credentials"),
            new KeyValuePair<string, string>("account_id", _accountId)
        });

        var response = await tokenClient.PostAsync("https://zoom.us/oauth/token", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.GetProperty("access_token").GetString() ?? throw new Exception("Không lấy được access token");
    }
}
