using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Application.Persistence
{
    //public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    //{
    //    public AppDbContext CreateDbContext(string[] args)
    //    {
    //        var basePath = Directory.GetCurrentDirectory();

    //        var configuration = new ConfigurationBuilder()
    //            .SetBasePath(basePath) // Ensure we point to the correct project
    //            .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
    //            .Build();

    //        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
    //        var connectionString = configuration.GetConnectionString("DefaultConnection");

    //        if (string.IsNullOrEmpty(connectionString))
    //        {
    //            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    //        }

    //        optionsBuilder.UseSqlServer(connectionString);

    //        return new AppDbContext(optionsBuilder.Options);
    //    }
    //}
}
