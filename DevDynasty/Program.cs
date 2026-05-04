var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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