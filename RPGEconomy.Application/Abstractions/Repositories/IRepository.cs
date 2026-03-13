using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(int id);
    Task<int> SaveAsync(T entity);  // возвращает Id (новый или существующий)
    Task DeleteAsync(int id);
}
