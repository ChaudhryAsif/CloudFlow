using Application.Models;
using MediatR;
using System.Collections.Generic;

public class GetProductsQuery : IRequest<List<ProductDto>> // ✅ Make sure to implement IRequest<T>
{
}
