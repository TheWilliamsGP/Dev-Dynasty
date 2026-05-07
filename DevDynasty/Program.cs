using DevDynasty.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<SupabaseService>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();

    var baseUrl = config["Supabase:Url"];
    var apiKey = config["Supabase:ApiKey"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Supabase:Url is missing.");

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("Supabase:ApiKey is missing.");

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("apikey", apiKey);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
});

builder.Services.AddHttpClient<SupabaseAdminEventsService>();
builder.Services.AddHttpClient<SupabaseVolunteerDashboardService>();
builder.Services.AddHttpClient<SupabaseVolunteerAuthService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});