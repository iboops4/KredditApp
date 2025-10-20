using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using kreddit_app;
using kreddit_app.Data;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient til Blazor (ikke til API-kald)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// ---- Load appsettings.json + appsettings.{Environment}.json manuelt ----
using var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

async Task TryLoadConfig(string file)
{
    var resp = await http.GetAsync(file);
    if (resp.IsSuccessStatusCode)
    {
        using var st = await resp.Content.ReadAsStreamAsync();
        builder.Configuration.AddJsonStream(st);
    }
}
await TryLoadConfig("appsettings.json");
await TryLoadConfig($"appsettings.{builder.HostEnvironment.Environment}.json");

// ApiService bruger configuration["base_api"]
builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();
