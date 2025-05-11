using Squawker.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Squawker.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();

        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsRelational())
            {
                await _context.Database.MigrateAsync();
            }
            else
            {
                await _context.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default data
        // Seed, if necessary
        if (!_context.Squawks.Any())
            {
                _context.Squawks.Add(new Squawk
                {
                    Id = Guid.NewGuid(),
                    Content = "Hello world! This is my first squawk!",
                    CreatedAt = DateTime.UtcNow,
                    Created = DateTimeOffset.UtcNow,
                    LastModified = DateTimeOffset.UtcNow
                });
                
                _context.Squawks.Add(new Squawk
                {
                    Id = Guid.NewGuid(),
                    Content = "Squawker is the next big social media platform!",
                    CreatedAt = DateTime.UtcNow,
                    Created = DateTimeOffset.UtcNow,
                    LastModified = DateTimeOffset.UtcNow
                });

                await _context.SaveChangesAsync();
            }
        }
}
