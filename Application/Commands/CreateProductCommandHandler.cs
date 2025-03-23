using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Commands
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Product>
    {
        private readonly IProductRepository _repository;

        public CreateProductCommandHandler(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<Product> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var product = new Product { Id = Guid.NewGuid(), Name = request.Name, Price = request.Price };
            await _repository.AddAsync(product);
            return product;
        }
    }
}
