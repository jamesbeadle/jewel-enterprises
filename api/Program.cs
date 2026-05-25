using Jewel.JPMS.Api.Data;
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
    })
    .Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>();
    await initialiser.EnsureCreatedAsync(scope.ServiceProvider);
}

await host.RunAsync();
