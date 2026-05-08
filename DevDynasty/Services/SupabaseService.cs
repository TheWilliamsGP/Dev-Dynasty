using System.Net.Http.Json;
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

        // Creates Donor
        public async Task<DonorDto?> CreateDonor(object donor)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/donartable")
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

        // Creates Donation
        public async Task CreateDonation(DonationDto donation)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/donationtable")
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

        // Creates Card
        public async Task CreateCard(CardDto card)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/cardtable")
            {
                Content = JsonContent.Create(card)
            };

            request.Headers.Add("Prefer", "return=representation");

            var response = await _http.SendAsync(request);
            var error = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SUPABASE CARD ERROR: {error}");
            }
        }

        // Get Donations
        public async Task<List<DonationDto>> GetDonations()
        {
            var response = await _http.GetAsync("/rest/v1/donationtable?select=*");

            var error = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SUPABASE GET ERROR: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<DonationDto>>();

            return result ?? new List<DonationDto>();
        }

        // Get Donors
        public async Task<List<DonorDto>> GetDonors()
        {
            var response = await _http.GetAsync("/rest/v1/donartable?select=*");

            var error = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SUPABASE DONOR GET ERROR: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<DonorDto>>();

            return result ?? new List<DonorDto>();
        }

        // Get Cards
        public async Task<List<CardDto>> GetCards()
        {
            var response = await _http.GetAsync("/rest/v1/cardtable?select=*");

            var error = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SUPABASE CARD GET ERROR: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<CardDto>>();

            return result ?? new List<CardDto>();
        }
    }
}