using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using FerroviaApi.Data;
using Microsoft.Extensions.Configuration;

namespace FerroviaApi
{
    public class FerroviaApiDbContextFactory : IDesignTimeDbContextFactory<FerroviaApiDbContext>
    {
        public FerroviaApiDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("FerroviaDb");

            var builder = new DbContextOptionsBuilder<FerroviaApiDbContext>();
            builder.UseNpgsql(connectionString);

            return new FerroviaApiDbContext(builder.Options);
        }
    }
}