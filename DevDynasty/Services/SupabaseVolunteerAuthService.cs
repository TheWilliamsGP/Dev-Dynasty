using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevDynasty.Models.VolunteerAuth;

namespace DevDynasty.Services
{
    public class SupabaseVolunteerAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _anonKey;
        private readonly string _serviceRoleKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public SupabaseVolunteerAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            _baseUrl = configuration["Supabase:Url"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Supabase:Url is missing.");

            _anonKey = configuration["Supabase:AnonKey"]
                ?? throw new InvalidOperationException("Supabase:AnonKey is missing.");

            _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is missing.");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<AuthResult> RegisterVolunteerAsync(VolunteerRegisterViewModel model)
        {
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();

            var existingVolunteer = await GetVolunteerByEmailAsync(normalizedEmail);

            if (existingVolunteer != null)
            {
                return AuthResult.Failed("A volunteer profile already exists for this email. Please log in instead.");
            }

            var authUser = await SignUpWithSupabaseAuthAsync(normalizedEmail, model.Password);

            if (authUser?.user?.id == null)
            {
                return AuthResult.Failed("Could not create Supabase Auth user.");
            }

            var volunteerId = authUser.user.id.Value;

            await CreateVolunteerProfileAsync(volunteerId, model);

            return AuthResult.Success(volunteerId, authUser.access_token);
        }

        public async Task<AuthResult> LoginVolunteerAsync(VolunteerLoginViewModel model)
        {
            var login = await SignInWithSupabaseAuthAsync(model.Email, model.Password);

            if (login?.user?.id == null)
            {
                return AuthResult.Failed("Invalid email or password.");
            }

            var volunteer = await GetVolunteerByEmailAsync(model.Email);

            if (volunteer == null)
            {
                return AuthResult.Failed("Login worked, but no volunteer profile was found for this email.");
            }

            return AuthResult.Success(volunteer.volunteerid, login.access_token);
        }

        private async Task<SupabaseAuthResponse?> SignUpWithSupabaseAuthAsync(string email, string password)
        {
            var payload = new
            {
                email,
                password
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/auth/v1/signup");
            request.Headers.Add("apikey", _anonKey);

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Supabase signup failed: {response.StatusCode} - {content}");

            return JsonSerializer.Deserialize<SupabaseAuthResponse>(content, _jsonOptions);
        }

        private async Task<SupabaseAuthResponse?> SignInWithSupabaseAuthAsync(string email, string password)
        {
            var payload = new
            {
                email,
                password
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/auth/v1/token?grant_type=password");
            request.Headers.Add("apikey", _anonKey);

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<SupabaseAuthResponse>(content, _jsonOptions);
        }

        private async Task CreateVolunteerProfileAsync(Guid volunteerId, VolunteerRegisterViewModel model)
        {
            var payload = new
            {
                volunteerid = volunteerId,
                volunteerfname = model.FirstName.Trim(),
                volunteersname = model.Surname.Trim(),
                volunteerpno = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                volunteeremail = model.Email.Trim().ToLowerInvariant(),
                volunteerpassword = "SUPABASE_AUTH"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/rest/v1/volunteertable");

            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            request.Headers.Add("Prefer", "return=minimal");

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Supabase Auth user was created, but creating the volunteer profile failed. " +
                    $"Status: {response.StatusCode}. Supabase response: {content}"
                );
            }
        }

        private async Task<VolunteerRow?> GetVolunteerByEmailAsync(string email)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_baseUrl}/rest/v1/volunteertable?select=volunteerid,volunteerfname,volunteersname,volunteeremail&volunteeremail=eq.{Uri.EscapeDataString(email)}"
            );

            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Volunteer lookup failed: {response.StatusCode} - {content}");

            var rows = JsonSerializer.Deserialize<List<VolunteerRow>>(content, _jsonOptions);

            return rows?.FirstOrDefault();
        }

        public class AuthResult
        {
            public bool IsSuccess { get; set; }
            public Guid? VolunteerId { get; set; }
            public string? AccessToken { get; set; }
            public string? ErrorMessage { get; set; }

            public static AuthResult Success(Guid volunteerId, string? accessToken)
            {
                return new AuthResult
                {
                    IsSuccess = true,
                    VolunteerId = volunteerId,
                    AccessToken = accessToken
                };
            }

            public static AuthResult Failed(string message)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = message
                };
            }
        }

        private class SupabaseAuthResponse
        {
            public string? access_token { get; set; }
            public SupabaseAuthUser? user { get; set; }
        }

        private class SupabaseAuthUser
        {
            public Guid? id { get; set; }
            public string? email { get; set; }
        }

        private class VolunteerRow
        {
            public Guid volunteerid { get; set; }
            public string volunteerfname { get; set; } = string.Empty;
            public string volunteersname { get; set; } = string.Empty;
            public string? volunteeremail { get; set; }
        }
    }
}