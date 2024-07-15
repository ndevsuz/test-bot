using System.Linq.Expressions;
using TestBot.Models;

namespace TestBot.Repositories;

public interface ITestRepository
{
    Task<Test> AddAsync(Test entity);
    Task<Test> UpdateAsync(Test entity);
    Task<bool> DeleteAsync(Expression<Func<Test, bool>> expression);
    Task<Test> SelectAsync(Expression<Func<Test, bool>> expression, string[] includes = null);
    IQueryable<Test> SelectAll(Expression<Func<Test, bool>> expression = null, string[] includes = null, bool isTracking = true);
    Task<int> SaveAsync();
}