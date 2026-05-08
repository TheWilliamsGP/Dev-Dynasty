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

            var authUser = await CreateSupabaseAuthUserAsAdminAsync(
                normalizedEmail,
                model.Password
            );

            if (authUser?.id == null)
            {
                return AuthResult.Failed("Could not create Supabase Auth user.");
            }

            var volunteerId = authUser.id.Value;

            await CreateVolunteerProfileAsync(volunteerId, model);

            var login = await SignInWithSupabaseAuthAsync(normalizedEmail, model.Password);

            return AuthResult.Success(
                userId: volunteerId,
                accessToken: login?.access_token,
                role: UserRole.Volunteer
            );
        }

        public async Task<AuthResult> LoginVolunteerAsync(VolunteerLoginViewModel model)
        {
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();

            var login = await SignInWithSupabaseAuthAsync(normalizedEmail, model.Password);

            if (login?.user?.id == null)
            {
                return AuthResult.Failed("Invalid email or password.");
            }

            var admin = await GetAdminByEmailAsync(normalizedEmail);

            if (admin != null)
            {
                return AuthResult.Success(
                    userId: admin.adminid,
                    accessToken: login.access_token,
                    role: UserRole.Admin
                );
            }

            var volunteer = await GetVolunteerByEmailAsync(normalizedEmail);

            if (volunteer == null)
            {
                return AuthResult.Failed("Login worked, but no volunteer or admin profile was found for this email.");
            }

            return AuthResult.Success(
                userId: volunteer.volunteerid,
                accessToken: login.access_token,
                role: UserRole.Volunteer
            );
        }

        public async Task SendPasswordResetEmailAsync(string email, string redirectTo)
        {
            var payload = new
            {
                email = email.Trim().ToLowerInvariant()
            };

            var url = $"{_baseUrl}/auth/v1/recover?redirect_to={Uri.EscapeDataString(redirectTo)}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("apikey", _anonKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _anonKey);

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Supabase password reset failed. Status: {response.StatusCode}. Response: {content}"
                );
            }
        }

        public async Task UpdatePasswordWithRecoveryTokenAsync(string accessToken, string newPassword)
        {
            var payload = new
            {
                password = newPassword
            };

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/auth/v1/user");

            request.Headers.Add("apikey", _anonKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Supabase password update failed. Status: {response.StatusCode}. Response: {content}"
                );
            }
        }

        private async Task<SupabaseAdminUserResponse?> CreateSupabaseAuthUserAsAdminAsync(string email, string password)
        {
            var payload = new
            {
                email,
                password,
                email_confirm = true,
                user_metadata = new
                {
                    role = "volunteer"
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/auth/v1/admin/users");

            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Supabase admin user creation failed. Status: {response.StatusCode}. Response: {content}"
                );
            }

            return JsonSerializer.Deserialize<SupabaseAdminUserResponse>(content, _jsonOptions);
        }

        private async Task<SupabaseAuthResponse?> SignInWithSupabaseAuthAsync(string email, string password)
        {
            var payload = new
            {
                email,
                password
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/auth/v1/token?grant_type=password"
            );

            request.Headers.Add("apikey", _anonKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _anonKey);

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Supabase login failed. Status: {response.StatusCode}. Response: {content}"
                );
            }

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
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            request.Headers.Add("Prefer", "return=minimal");

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Supabase Auth user was created, but creating the volunteer profile failed. " +
                    $"Status: {response.StatusCode}. Response: {content}"
                );
            }
        }

        private async Task<VolunteerRow?> GetVolunteerByEmailAsync(string email)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_baseUrl}/rest/v1/volunteertable?select=volunteerid,volunteerfname,volunteersname,volunteeremail&volunteeremail=eq.{Uri.EscapeDataString(email.Trim().ToLowerInvariant())}"
            );

            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Volunteer lookup failed. Status: {response.StatusCode}. Response: {content}"
                );
            }

            var rows = JsonSerializer.Deserialize<List<VolunteerRow>>(content, _jsonOptions);

            return rows?.FirstOrDefault();
        }

        private async Task<AdminRow?> GetAdminByEmailAsync(string email)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_baseUrl}/rest/v1/admintable?select=adminid,adminfname,adminsname,adminemail,isactive&adminemail=eq.{Uri.EscapeDataString(email.Trim().ToLowerInvariant())}&isactive=eq.true"
            );

            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Admin lookup failed. Status: {response.StatusCode}. Response: {content}"
                );
            }

            var rows = JsonSerializer.Deserialize<List<AdminRow>>(content, _jsonOptions);

            return rows?.FirstOrDefault();
        }

        public enum UserRole
        {
            Volunteer,
            Admin
        }

        public class AuthResult
        {
            public bool IsSuccess { get; set; }
            public Guid? UserId { get; set; }
            public Guid? VolunteerId => Role == UserRole.Volunteer ? UserId : null;
            public Guid? AdminId => Role == UserRole.Admin ? UserId : null;
            public string? AccessToken { get; set; }
            public string? ErrorMessage { get; set; }
            public UserRole Role { get; set; }

            public bool IsAdmin => Role == UserRole.Admin;
            public bool IsVolunteer => Role == UserRole.Volunteer;

            public static AuthResult Success(Guid userId, string? accessToken, UserRole role)
            {
                return new AuthResult
                {
                    IsSuccess = true,
                    UserId = userId,
                    AccessToken = accessToken,
                    Role = role
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

        private class SupabaseAdminUserResponse
        {
            public Guid? id { get; set; }
            public string? email { get; set; }
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

        private class AdminRow
        {
            public Guid adminid { get; set; }
            public string adminfname { get; set; } = string.Empty;
            public string adminsname { get; set; } = string.Empty;
            public string adminemail { get; set; } = string.Empty;
            public bool isactive { get; set; }
        }
    }
}