using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Data;

public sealed class DatabaseInitialiser
{
    public async Task EnsureCreatedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<JpmsContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
