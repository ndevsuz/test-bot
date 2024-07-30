using System.Linq.Expressions;
using TestBot.Models;

namespace AnswerBot.Repositories;

public interface IAnswerRepository
{
    Task<Answer> AddAsync(Answer entity);
    Task<Answer> UpdateAsync(Answer entity);
    Task<bool> DeleteAsync(Expression<Func<Answer, bool>> expression);
    Task<Answer> SelectAsync(Expression<Func<Answer, bool>> expression, string[] includes = null);
    IQueryable<Answer> SelectAll(Expression<Func<Answer, bool>> expression = null, string[] includes = null, bool isTracking = true);
    Task<int> SaveAsync();
}