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
// Swap InMemoryProjectStore -> HttpProjectStore when the SQL API is deployed.
// HttpProjectStore reads/writes via /api/projects (Azure SQL via EF Core).
builder.Services.AddScoped<IProjectStore, InMemoryProjectStore>();
builder.Services.AddScoped<ILeadStore, InMemoryLeadStore>();
builder.Services.AddScoped<IRateLibrary, InMemoryRateLibrary>();
builder.Services.AddScoped<IBoqStore, InMemoryBoqStore>();
builder.Services.AddScoped<IDrawingStore, InMemoryDrawingStore>();
builder.Services.AddScoped<ISubcontractorStore, InMemorySubcontractorStore>();
builder.Services.AddScoped<IHsRegister, InMemoryHsRegister>();
builder.Services.AddScoped<IProcurementStore, InMemoryProcurementStore>();
builder.Services.AddScoped<IMobilisationStore, InMemoryMobilisationStore>();
builder.Services.AddScoped<IChangeRegister, InMemoryChangeRegister>();
builder.Services.AddScoped<ISiteStore, InMemorySiteStore>();
builder.Services.AddScoped<ICommercialStore, InMemoryCommercialStore>();
builder.Services.AddScoped<ICvrStore, InMemoryCvrStore>();
builder.Services.AddScoped<ICloseoutStore, InMemoryCloseoutStore>();
builder.Services.AddScoped<PortalContext>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SessionService>();

await builder.Build().RunAsync();
