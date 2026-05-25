using Jewel.JPMS;
using Jewel.JPMS.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(serviceProvider => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<IUserDirectory, AllowListUserDirectory>();
builder.Services.AddScoped<IAccessRequestStore, InMemoryAccessRequestStore>();
builder.Services.AddScoped<IProjectStore, InMemoryProjectStore>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SessionService>();

await builder.Build().RunAsync();
