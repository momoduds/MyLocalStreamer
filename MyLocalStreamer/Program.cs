using MyLocalStreamer.Components;
using MyLocalStreamer.Endpoints;
using MyLocalStreamer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Singleton: one library / watch-history shared by every browser tab (circuit).
builder.Services.AddSingleton<IVideoLibrary, VideoLibrary>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// NOTE: HTTPS redirection / HSTS are intentionally omitted - this is a local/LAN app
// and redirecting to https without a trusted certificate breaks other devices.

app.UseAntiforgery();

app.MapStaticAssets();
app.MapMediaEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
