using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jewel.JPMS.Api.Data;

public sealed class JpmsContextDesignTimeFactory : IDesignTimeDbContextFactory<JpmsContext>
{
    public JpmsContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JpmsContext>()
            .UseSqlServer("Server=localhost;Database=JpmsDesignTime;Trusted_Connection=True;")
            .Options;
        return new JpmsContext(options);
    }
}
