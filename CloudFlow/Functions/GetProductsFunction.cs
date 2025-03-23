using Application.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace AzzurFunctionApp.Functions
{
    public class GetProductsFunction
    {
        private readonly IMediator _mediator;
        private readonly IProductRepository _repository;
        private readonly IMapper _mapper;

        public GetProductsFunction(IMediator mediator, IProductRepository repository, IMapper mapper)
        {
            _mediator = mediator;
            _repository = repository;
            _mapper = mapper;
        }

        [Function("GetProducts")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            var query = new GetProductsQuery();
            var products = await _mediator.Send(query); // ✅ Ensure this matches `IRequest<T>`

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(products);
            return response;
        }
    }
}
