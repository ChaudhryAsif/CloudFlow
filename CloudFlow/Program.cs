using Application.Commands;
using Application.Interfaces;
using Application.Mapper;
using Application.Models;
using Application.Persistence;
using Application.Repositories;
using AzzurFunctionApp.Functions;
using Domain.Entities;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        // Load configuration from local.settings.json
        configBuilder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Configure Application Insights for logging
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });

            services.AddSingleton<TelemetryClient>();
        }

        services.Configure<LoggerFilterOptions>(options =>
        {
            options.MinLevel = LogLevel.Information;
        });

        // Database Connection String
        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing.");
        }

        // Register DbContext with DI
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IRequestHandler<GetProductsQuery, List<ProductDto>>, GetProductsQueryHandler>();

        services.AddScoped<IRequestHandler<CreateProductCommand, Product>, CreateProductCommandHandler>();

        // Register IProductRepository
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddSingleton<OrderCommandHandler>();
        services.AddSingleton<ProcessOrderFunction>();

        // Register AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // Register MediatR for CQRS
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    })
    .Build();

host.Run();
