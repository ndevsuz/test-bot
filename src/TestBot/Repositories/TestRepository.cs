using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TestBot.Contexts;
using TestBot.Models;

namespace TestBot.Repositories;

public class TestRepository : ITestRepository
{
    private readonly DbSet<Test> table;
    private readonly AppDbContext dbContext;

    public TestRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Test> AddAsync(Test entity)
    {
        await table.AddAsync(entity);

        return entity;
    }

    public async Task<Test> UpdateAsync(Test entity)
    {
        EntityEntry<Test> entry = this.dbContext.Update(entity);

        return entry.Entity;
    }

    public async Task<bool> DeleteAsync(Expression<Func<Test, bool>> expression)
    {
        var entity = await this.SelectAsync(expression);

        if (entity is not null)
        {
            table.Remove(entity);
            return true;
        }

        return false;
    }

    public async Task<Test> SelectAsync(Expression<Func<Test, bool>> expression, string[] includes = null)
        => await this.SelectAll(expression, includes).FirstOrDefaultAsync();

    public IQueryable<Test> SelectAll(Expression<Func<Test, bool>> expression = null, string[] includes = null, bool isTracking = true)
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