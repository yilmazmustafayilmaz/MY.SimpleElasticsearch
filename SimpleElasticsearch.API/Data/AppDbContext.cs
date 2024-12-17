using Microsoft.EntityFrameworkCore;

namespace SimpleElasticsearch.API.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(".");
    }
}
