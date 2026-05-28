using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.AccessRequests;
using Jewel.JPMS.Api.Features.Boq;
using Jewel.JPMS.Api.Features.Cashflow;
using Jewel.JPMS.Api.Features.Changes;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Features.CommercialInputs;
using Jewel.JPMS.Api.Features.Cvr;
using Jewel.JPMS.Api.Features.Directory;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Api.Features.Hs;
using Jewel.JPMS.Api.Features.Leads;
using Jewel.JPMS.Api.Features.Mobilisation;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Features.Projects;
using Jewel.JPMS.Api.Features.Rates;
using Jewel.JPMS.Api.Features.Site;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Gates;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString application setting missing.");

        services.AddDbContext<JpmsContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddSingleton<DatabaseInitialiser>();
        services.AddScoped<SignedInUserResolver>();
        services.AddDirectoryFeature();
        services.AddAccessRequestsFeature();
        services.AddProjectsFeature();
        services.AddLeadsFeature();
        services.AddBoqFeature();
        services.AddRatesFeature();
        services.AddDrawingsFeature();
        services.AddProcurementFeature();
        services.AddSubcontractorsFeature();
        services.AddHsFeature();
        services.AddMobilisationFeature();
        services.AddSiteFeature();
        services.AddCommercialFeature();
        services.AddCommercialInputsFeature();
        services.AddCashflowFeature();
        services.AddCvrFeature();
        services.AddCloseoutFeature();
        services.AddChangesFeature();
    })
    .Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>();
    await initialiser.ApplyMigrationsAsync(scope.ServiceProvider);
}

await host.RunAsync();
