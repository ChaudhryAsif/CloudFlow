using Domain.Entities;
using MediatR;

namespace Application.Commands
{
    public class CreateProductCommand : IRequest<Product>
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
