using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TestBot.Contexts;
using TestBot.Models;

namespace TestBot.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbSet<User?> table;
    private readonly AppDbContext dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
        table = dbContext.Set<User>();
    }


    public async Task AddAsync(User? entity)
    {
        await dbContext.Users.AddAsync(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<User> UpdateAsync(User entity)
    {
        EntityEntry<User> entry = this.dbContext.Update(entity);

        return entry.Entity;
    }

    public async Task<bool> DeleteAsync(Expression<Func<User?, bool>> expression)
    {
        var entity = await this.SelectAsync(expression);

        if (entity is not null)
        {
            table.Remove(entity);
            return true;
        }

        return false;
    }

    public async Task<User?> SelectAsync(Expression<Func<User?, bool>> expression, string[] includes = null)
        => await this.SelectAll(expression, includes).FirstOrDefaultAsync();


    public IQueryable<User?> SelectAll(Expression<Func<User?, bool>> expression = null, string[] includes = null, bool isTracking = true)
    {
        var query = expression is null ? isTracking ? table : table.AsNoTracking()
            : isTracking ? table.Where(expression) : table.Where(expression).AsNoTracking();

        if (includes is not null)
            foreach (var include in includes)
                query = query.Include(include);

        return query;
    }

    public async Task<int> SaveAsync()
    {
        var result = await dbContext.SaveChangesAsync();
        return result;
    }
}