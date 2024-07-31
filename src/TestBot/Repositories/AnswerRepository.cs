using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TestBot.Contexts;
using TestBot.Models;

namespace AnswerBot.Repositories;

public class AnswerRepository : IAnswerRepository
{
    private readonly DbSet<Answer> table;
    private readonly AppDbContext dbContext;

    public AnswerRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
        table = dbContext.Set<Answer>();
    }

    public async Task<Answer> AddAsync(Answer entity)
    {
        await table.AddAsync(entity);

        return entity;
    }

    public async Task<Answer> UpdateAsync(Answer entity)
    {
        EntityEntry<Answer> entry = this.dbContext.Update(entity);

        return entry.Entity;
    }

    public async Task<bool> DeleteAsync(Expression<Func<Answer, bool>> expression)
    {
        var entity = await this.SelectAsync(expression);

        if (entity is not null)
        {
            table.Remove(entity);
            return true;
        }

        return false;
    }

    public async Task<Answer> SelectAsync(Expression<Func<Answer, bool>> expression, string[] includes = null)
        => await this.SelectAll(expression, includes).FirstOrDefaultAsync();

    public async Task<List<string>> SelectAnswersAsync(Expression<Func<Answer, bool>> expression)
    {
        return await SelectAll(expression)
            .Select(a => a.Answers)
            .ToListAsync();
    }
    public IQueryable<Answer> SelectAll(Expression<Func<Answer, bool>> expression = null, string[] includes = null, bool isTracking = true)
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