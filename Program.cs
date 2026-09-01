using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ManyControl_Web;
using ManyControl_Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Serviços da Aplicação ManyControl PWA
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<FinanceWebService>();
builder.Services.AddScoped<BackupService>();

await builder.Build().RunAsync();
