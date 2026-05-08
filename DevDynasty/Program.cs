using DevDynasty.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<SupabaseService>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();

    var baseUrl = config["Supabase:Url"];
    var apiKey = config["Supabase:ApiKey"] ?? config["Supabase:AnonKey"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Supabase:Url is missing.");

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("Supabase:ApiKey or Supabase:AnonKey is missing.");

    client.BaseAddress = new Uri($"{baseUrl}/rest/v1/");
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/404");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "about",
    pattern: "about",
    defaults: new { controller = "Home", action = "About" });

app.MapControllerRoute(
    name: "community",
    pattern: "community",
    defaults: new { controller = "Home", action = "Community" });

app.MapControllerRoute(
    name: "contact",
    pattern: "contact",
    defaults: new { controller = "Home", action = "Contact" });

app.MapControllerRoute(
    name: "donate",
    pattern: "donate",
    defaults: new { controller = "Home", action = "Donate" });

app.MapControllerRoute(
    name: "donations",
    pattern: "donations",
    defaults: new { controller = "Home", action = "Donations" });

app.MapControllerRoute(
    name: "events",
    pattern: "events",
    defaults: new { controller = "Home", action = "Events" });

app.MapControllerRoute(
    name: "resources",
    pattern: "resources",
    defaults: new { controller = "Home", action = "Resources" });

app.MapControllerRoute(
    name: "services",
    pattern: "services",
    defaults: new { controller = "Home", action = "Services" });

app.MapControllerRoute(
    name: "volunteer",
    pattern: "volunteer",
    defaults: new { controller = "Home", action = "Volunteer" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();