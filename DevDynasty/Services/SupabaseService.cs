using System.Net.Http.Headers;
using System.Text.Json;
using DevDynasty.Models;

namespace DevDynasty.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _http;

        public SupabaseService(HttpClient http)
        {
            _http = http;
        }

        //Creates Donor
        public async Task<DonorDto> CreateDonor(DonorDto donor)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "donartable")
            {
                Content = JsonContent.Create(donor)
            };

            request.Headers.Add("Prefer", "return=representation");

            var response = await _http.SendAsync(request);

            var error = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SUPABASE DONOR ERROR: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<DonorDto>>();
            return result?.FirstOrDefault();
        }

        //Creates Donation
        public async Task CreateDonation(DonationDto donation)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "donationtable")
            {
                Content = JsonContent.Create(donation)
            };

            request.Headers.Add("Prefer", "return=representation");

            var response = await _http.SendAsync(request);

            var error = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SUPABASE DONATION ERROR: {error}");
            }
        }
    }
}
