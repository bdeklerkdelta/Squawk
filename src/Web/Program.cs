using Squawker.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Squawker.Application.Squawks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Squawker.Infrastructure.Services;
using Squawker.Application.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Squawker")
        .AddTelemetrySdk()
        .AddEnvironmentVariableDetector())
    .WithTracing(tracing => tracing
        .AddSource("Squawker.Application")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter()) // For sending to an OpenTelemetry collector
    .WithMetrics(metrics => metrics
        .AddMeter("Squawker.Metrics")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter()); // For sending to an OpenTelemetry collector

// Register the telemetry service
builder.Services.AddSingleton<ITelemetryService, OpenTelemetryService>();

// Enhanced Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add XML comments support
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
    
    // Add security definition for JWT Bearer authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.MapType<SquawkDto>(() => new OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["id"] = new OpenApiSchema { Type = "string", Format = "uuid", Example = new OpenApiString(Guid.NewGuid().ToString()) },
            ["content"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("This is an example squawk!") },
            ["createdAt"] = new OpenApiSchema { Type = "string", Format = "date-time", Example = new OpenApiString(DateTime.UtcNow.ToString("o")) },
            ["createdBy"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("user@example.com") }
        },
        Required = new HashSet<string> { "id", "content", "createdAt" }
    });

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Squawker API",
        Description = "A simple social media API for posting short messages (squawks)",
        Contact = new OpenApiContact
        {
            Name = "Squawker Support",
            Email = "support@squawker.example.com"
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

app.Run();

public partial class Program { }
