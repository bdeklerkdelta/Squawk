using System.Reflection;
using Squawker.Application.Common.Behaviours;
using Microsoft.Extensions.Hosting;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TelemetryBehaviour<,>));

        builder.Logging.AddConsole();
    }
}
