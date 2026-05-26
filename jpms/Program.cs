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

builder.Services.AddScoped<IProjectStore, HttpProjectStore>();
builder.Services.AddScoped<ILeadStore, HttpLeadStore>();
builder.Services.AddScoped<IRateLibrary, HttpRateLibrary>();
builder.Services.AddScoped<IBoqStore, HttpBoqStore>();
builder.Services.AddScoped<IDrawingStore, HttpDrawingStore>();
builder.Services.AddScoped<ISubcontractorStore, HttpSubcontractorStore>();
builder.Services.AddScoped<IHsRegister, HttpHsRegister>();
builder.Services.AddScoped<IProcurementStore, HttpProcurementStore>();
builder.Services.AddScoped<IMobilisationStore, HttpMobilisationStore>();
builder.Services.AddScoped<IChangeRegister, HttpChangeRegister>();
builder.Services.AddScoped<ISiteStore, HttpSiteStore>();
builder.Services.AddScoped<ICommercialStore, HttpCommercialStore>();
builder.Services.AddScoped<ICvrStore, HttpCvrStore>();
builder.Services.AddScoped<ICloseoutStore, HttpCloseoutStore>();

builder.Services.AddScoped<PortalContext>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ActiveRoleStorage>();
builder.Services.AddScoped<SessionService>();

await builder.Build().RunAsync();
