using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevDynasty.Models.VolunteerDashboard;

namespace DevDynasty.Services
{
    public class SupabaseVolunteerDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _serviceRoleKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public SupabaseVolunteerDashboardService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            _baseUrl = configuration["Supabase:Url"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Supabase:Url is missing.");

            _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is missing.");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _serviceRoleKey);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<VolunteerDashboardViewModel?> GetDashboardAsync(Guid volunteerId)
        {
            var volunteer = await GetVolunteerAsync(volunteerId);

            if (volunteer == null)
                return null;

            var events = await GetEventsForVolunteerAsync(volunteerId);

            return new VolunteerDashboardViewModel
            {
                VolunteerId = volunteer.volunteerid,
                VolunteerName = $"{volunteer.volunteerfname} {volunteer.volunteersname}".Trim(),
                Events = events
            };
        }

        public async Task<VolunteerEventDetailsViewModel?> GetEventDetailsAsync(Guid volunteerId, Guid eventId)
        {
            var dashboard = await GetDashboardAsync(volunteerId);

            if (dashboard == null)
                return null;

            var selectedEvent = dashboard.Events.FirstOrDefault(e => e.EventId == eventId);

            if (selectedEvent == null)
                return null;

            return new VolunteerEventDetailsViewModel
            {
                VolunteerId = volunteerId,
                VolunteerName = dashboard.VolunteerName,
                Event = selectedEvent
            };
        }

        public async Task VolunteerForEventAsync(Guid volunteerId, Guid eventId)
        {
            var existingVotes = await GetAsync<List<EventVoteRow>>(
                $"/rest/v1/eventvolunteervotetable?select=eventvoteid,eventid,volunteerid,votestatus&eventid=eq.{eventId}&volunteerid=eq.{volunteerId}"
            ) ?? new List<EventVoteRow>();

            var existingVote = existingVotes.FirstOrDefault();

            if (existingVote != null && existingVote.eventvoteid.HasValue)
            {
                var updatePayload = new
                {
                    votestatus = "joined",
                    votedat = DateTimeOffset.UtcNow
                };

                await SendAsync(
                    HttpMethod.Patch,
                    $"/rest/v1/eventvolunteervotetable?eventvoteid=eq.{existingVote.eventvoteid.Value}",
                    updatePayload
                );

                return;
            }

            var createPayload = new
            {
                eventid = eventId,
                volunteerid = volunteerId,
                votestatus = "joined"
            };

            await SendAsync(HttpMethod.Post, "/rest/v1/eventvolunteervotetable", createPayload);
        }

        private async Task<List<VolunteerEventCardViewModel>> GetEventsForVolunteerAsync(Guid volunteerId)
        {
            var events = await GetAsync<List<EventRow>>(
                "/rest/v1/eventactivitytable?select=eventid,eventname,eventtype,eventdescription,eventstartdate,eventenddate,locationid,requiredvolunteers,eventstatus&eventstatus=neq.cancelled&order=eventstartdate.asc"
            ) ?? new List<EventRow>();

            var locations = await GetAsync<List<LocationRow>>(
                "/rest/v1/locationtable?select=locationid,locationaddress"
            ) ?? new List<LocationRow>();

            var votes = await GetAsync<List<EventVoteRow>>(
                "/rest/v1/eventvolunteervotetable?select=eventid,volunteerid,votestatus&votestatus=eq.joined"
            ) ?? new List<EventVoteRow>();

            var joinedCounts = votes
                .GroupBy(v => v.eventid)
                .ToDictionary(g => g.Key, g => g.Count());

            var volunteerJoinedEventIds = votes
                .Where(v => v.volunteerid == volunteerId)
                .Select(v => v.eventid)
                .ToHashSet();

            return events.Select(e =>
            {
                var location = e.locationid.HasValue
                    ? locations.FirstOrDefault(l => l.locationid == e.locationid.Value)
                    : null;

                return new VolunteerEventCardViewModel
                {
                    EventId = e.eventid,
                    EventName = string.IsNullOrWhiteSpace(e.eventname) ? "Untitled event" : e.eventname,
                    EventType = e.eventtype,
                    EventDescription = e.eventdescription,
                    EventStartDate = e.eventstartdate,
                    EventEndDate = e.eventenddate,
                    LocationAddress = location?.locationaddress,
                    RequiredVolunteers = e.requiredvolunteers ?? 1,
                    JoinedVolunteers = joinedCounts.TryGetValue(e.eventid, out var count) ? count : 0,
                    HasJoined = volunteerJoinedEventIds.Contains(e.eventid)
                };
            }).ToList();
        }

        private async Task<VolunteerRow?> GetVolunteerAsync(Guid volunteerId)
        {
            var volunteers = await GetAsync<List<VolunteerRow>>(
                $"/rest/v1/volunteertable?select=volunteerid,volunteerfname,volunteersname,volunteeremail&volunteerid=eq.{volunteerId}"
            );

            return volunteers?.FirstOrDefault();
        }

        private async Task<T?> GetAsync<T>(string path)
        {
            var response = await _httpClient.GetAsync(_baseUrl + path);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Supabase GET failed: {response.StatusCode} - {content}");

            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }

        private async Task SendAsync(HttpMethod method, string path, object? payload)
        {
            using var request = new HttpRequestMessage(method, _baseUrl + path);
            request.Headers.Add("Prefer", "return=minimal");

            if (payload != null)
            {
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Supabase request failed: {response.StatusCode} - {content}");
        }

        private class VolunteerRow
        {
            public Guid volunteerid { get; set; }
            public string volunteerfname { get; set; } = string.Empty;
            public string volunteersname { get; set; } = string.Empty;
            public string? volunteeremail { get; set; }
        }

        private class EventRow
        {
            public Guid eventid { get; set; }
            public string? eventname { get; set; }
            public string? eventtype { get; set; }
            public string? eventdescription { get; set; }
            public string? eventstartdate { get; set; }
            public string? eventenddate { get; set; }
            public Guid? locationid { get; set; }
            public int? requiredvolunteers { get; set; }
            public string? eventstatus { get; set; }
        }

        private class LocationRow
        {
            public Guid locationid { get; set; }
            public string? locationaddress { get; set; }
        }

        private class EventVoteRow
        {
            public Guid? eventvoteid { get; set; }
            public Guid eventid { get; set; }
            public Guid? volunteerid { get; set; }
            public string? votestatus { get; set; }
        }
    }
}