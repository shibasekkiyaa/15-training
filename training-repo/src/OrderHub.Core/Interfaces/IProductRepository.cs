using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();
    Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold, DateTime soldSinceUtc);
    Task<Product?> GetByIdAsync(int id);
    Task SaveChangesAsync();
}
