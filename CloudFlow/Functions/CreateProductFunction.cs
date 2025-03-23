using Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace AzzurFunctionApp.Functions
{
    public class CreateProductFunction
    {
        private readonly IMediator _mediator;

        public CreateProductFunction(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Function("CreateProduct")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var command = JsonConvert.DeserializeObject<CreateProductCommand>(body);

            var product = await _mediator.Send(command);
            return new OkObjectResult(product);
        }
    }
}
