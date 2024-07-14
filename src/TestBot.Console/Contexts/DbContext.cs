using Microsoft.EntityFrameworkCore;
using TestBot.Console.Models;

namespace TestBot.Console.Contexts;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
    
    public DbSet<Test> Tests { get; set; }
}