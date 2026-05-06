using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevDynasty.Models.AdminEvents;

namespace DevDynasty.Services
{
    public class SupabaseAdminEventsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _serviceRoleKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public SupabaseAdminEventsService(HttpClient httpClient, IConfiguration configuration)
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

        public async Task<List<EventListItemViewModel>> GetEventsAsync()
        {
            var events = await GetAsync<List<EventRow>>(
                "/rest/v1/eventactivitytable?select=eventid,eventname,eventtype,eventdescription,eventstartdate,eventenddate,locationid,requiredvolunteers,eventstatus&order=eventstartdate.asc"
            ) ?? new List<EventRow>();

            var votes = await GetAsync<List<EventVoteRow>>(
                "/rest/v1/eventvolunteervotetable?select=eventid,votestatus&votestatus=eq.joined"
            ) ?? new List<EventVoteRow>();

            var locations = await GetLocationsAsync();

            var joinedCounts = votes
                .GroupBy(v => v.eventid)
                .ToDictionary(g => g.Key, g => g.Count());

            return events.Select(e =>
            {
                var location = e.locationid.HasValue
                    ? locations.FirstOrDefault(l => l.LocationId == e.locationid.Value)
                    : null;

                return new EventListItemViewModel
                {
                    EventId = e.eventid,
                    EventName = e.eventname ?? "Untitled event",
                    EventType = e.eventtype,
                    EventDescription = e.eventdescription,
                    EventStartDate = e.eventstartdate,
                    EventEndDate = e.eventenddate,
                    LocationId = e.locationid,
                    LocationAddress = location?.LocationAddress,
                    RequiredVolunteers = e.requiredvolunteers ?? 1,
                    JoinedVolunteers = joinedCounts.TryGetValue(e.eventid, out var count) ? count : 0,
                    EventStatus = e.eventstatus ?? "active"
                };
            }).ToList();
        }

        public async Task<EventListItemViewModel?> GetEventByIdAsync(Guid eventId)
        {
            var events = await GetEventsAsync();
            return events.FirstOrDefault(e => e.EventId == eventId);
        }

        public async Task<AdminEventFormViewModel?> GetEventFormByIdAsync(Guid eventId)
        {
            var rows = await GetAsync<List<EventRow>>(
                $"/rest/v1/eventactivitytable?select=eventid,eventname,eventtype,eventdescription,eventstartdate,eventenddate,locationid,requiredvolunteers,eventstatus&eventid=eq.{eventId}"
            );

            var row = rows?.FirstOrDefault();

            if (row == null)
                return null;

            return new AdminEventFormViewModel
            {
                EventId = row.eventid,
                EventName = row.eventname ?? string.Empty,
                EventType = row.eventtype,
                EventDescription = row.eventdescription,
                EventStartDate = row.eventstartdate,
                EventEndDate = row.eventenddate,
                LocationId = row.locationid,
                RequiredVolunteers = row.requiredvolunteers ?? 1,
                EventStatus = row.eventstatus ?? "active",
                Locations = await GetLocationsAsync()
            };
        }

        public async Task CreateEventAsync(AdminEventFormViewModel model)
        {
            var payload = new
            {
                eventname = model.EventName,
                eventtype = model.EventType,
                eventdescription = model.EventDescription,
                eventstartdate = EmptyToNull(model.EventStartDate),
                eventenddate = EmptyToNull(model.EventEndDate),
                locationid = model.LocationId,
                requiredvolunteers = model.RequiredVolunteers,
                eventstatus = model.EventStatus
            };

            await SendAsync(HttpMethod.Post, "/rest/v1/eventactivitytable", payload);
        }

        public async Task UpdateEventAsync(AdminEventFormViewModel model)
        {
            if (!model.EventId.HasValue)
                throw new InvalidOperationException("EventId is required for updating.");

            var payload = new
            {
                eventname = model.EventName,
                eventtype = model.EventType,
                eventdescription = model.EventDescription,
                eventstartdate = EmptyToNull(model.EventStartDate),
                eventenddate = EmptyToNull(model.EventEndDate),
                locationid = model.LocationId,
                requiredvolunteers = model.RequiredVolunteers,
                eventstatus = model.EventStatus
            };

            await SendAsync(HttpMethod.Patch, $"/rest/v1/eventactivitytable?eventid=eq.{model.EventId.Value}", payload);
        }

        public async Task DeleteEventAsync(Guid eventId)
        {
            await SendAsync(HttpMethod.Delete, $"/rest/v1/eventactivitytable?eventid=eq.{eventId}", null);
        }

        public async Task<List<LocationOptionViewModel>> GetLocationsAsync()
        {
            var rows = await GetAsync<List<LocationRow>>(
                "/rest/v1/locationtable?select=locationid,locationaddress,locationcapacity&order=locationaddress.asc"
            ) ?? new List<LocationRow>();

            return rows.Select(l => new LocationOptionViewModel
            {
                LocationId = l.locationid,
                LocationAddress = l.locationaddress,
                LocationCapacity = l.locationcapacity
            }).ToList();
        }

        public async Task<List<VolunteerOptionViewModel>> GetVolunteersAsync()
        {
            var rows = await GetAsync<List<VolunteerRow>>(
                "/rest/v1/volunteertable?select=volunteerid,volunteerfname,volunteersname,volunteeremail&order=volunteerfname.asc"
            ) ?? new List<VolunteerRow>();

            return rows.Select(v => new VolunteerOptionViewModel
            {
                VolunteerId = v.volunteerid,
                VolunteerFirstName = v.volunteerfname,
                VolunteerSurname = v.volunteersname,
                VolunteerEmail = v.volunteeremail
            }).ToList();
        }

        public async Task<List<VolunteerOptionViewModel>> GetAssignedVolunteersAsync(Guid eventId)
        {
            var votes = await GetAsync<List<EventVoteRow>>(
                $"/rest/v1/eventvolunteervotetable?select=eventid,volunteerid,votestatus&eventid=eq.{eventId}&votestatus=eq.joined"
            ) ?? new List<EventVoteRow>();

            var volunteers = await GetVolunteersAsync();

            var assignedIds = votes
                .Where(v => v.volunteerid.HasValue)
                .Select(v => v.volunteerid!.Value)
                .ToHashSet();

            return volunteers
                .Where(v => assignedIds.Contains(v.VolunteerId))
                .ToList();
        }

        public async Task<ManageEventVolunteersViewModel?> GetManageVolunteersModelAsync(Guid eventId)
        {
            var selectedEvent = await GetEventByIdAsync(eventId);

            if (selectedEvent == null)
                return null;

            var allVolunteers = await GetVolunteersAsync();
            var assignedVolunteers = await GetAssignedVolunteersAsync(eventId);

            var assignedIds = assignedVolunteers.Select(v => v.VolunteerId).ToHashSet();

            return new ManageEventVolunteersViewModel
            {
                Event = selectedEvent,
                AssignedVolunteers = assignedVolunteers,
                AvailableVolunteers = allVolunteers
                    .Where(v => !assignedIds.Contains(v.VolunteerId))
                    .ToList()
            };
        }

        public async Task AssignVolunteerAsync(Guid eventId, Guid volunteerId)
        {
            var existing = await GetAsync<List<EventVoteRow>>(
                $"/rest/v1/eventvolunteervotetable?select=eventvoteid,eventid,volunteerid,votestatus&eventid=eq.{eventId}&volunteerid=eq.{volunteerId}"
            );

            var existingVote = existing?.FirstOrDefault();

            if (existingVote?.eventvoteid != null)
            {
                var updatePayload = new
                {
                    votestatus = "joined",
                    votedat = DateTimeOffset.UtcNow
                };

                await SendAsync(HttpMethod.Patch, $"/rest/v1/eventvolunteervotetable?eventvoteid=eq.{existingVote.eventvoteid}", updatePayload);
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

        public async Task UnassignVolunteerAsync(Guid eventId, Guid volunteerId)
        {
            await SendAsync(
                HttpMethod.Delete,
                $"/rest/v1/eventvolunteervotetable?eventid=eq.{eventId}&volunteerid=eq.{volunteerId}",
                null
            );
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

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
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
            public int? locationcapacity { get; set; }
        }

        private class VolunteerRow
        {
            public Guid volunteerid { get; set; }
            public string volunteerfname { get; set; } = string.Empty;
            public string volunteersname { get; set; } = string.Empty;
            public string? volunteeremail { get; set; }
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