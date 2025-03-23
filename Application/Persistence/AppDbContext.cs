using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; } // Products Table
    }
}
