using Domain.Entities;

namespace Application.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();  // Fetch all products
        Task AddAsync(Product product);  // Add a new product
    }
}
