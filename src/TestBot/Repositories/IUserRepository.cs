using System.Linq.Expressions;
using TestBot.Models;

namespace TestBot.Repositories;

public interface IUserRepository
{
    
    Task<User> AddAsync(User entity);
    Task<User> UpdateAsync(User entity);
    Task<bool> DeleteAsync(Expression<Func<User, bool>> expression);
    Task<User> SelectAsync(Expression<Func<User, bool>> expression, string[] includes = null);
    IQueryable<User> SelectAll(Expression<Func<User, bool>> expression = null, string[] includes = null, bool isTracking = true);
    Task<int> SaveAsync();
}