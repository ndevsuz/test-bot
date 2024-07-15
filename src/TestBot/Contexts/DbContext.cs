using Microsoft.EntityFrameworkCore;
using TestBot.Models;

namespace TestBot.Contexts;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
    
    public DbSet<Test> Tests { get; set; }
}