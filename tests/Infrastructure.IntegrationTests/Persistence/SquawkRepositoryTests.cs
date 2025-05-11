using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Squawker.Application.Common.Interfaces;
using Squawker.Domain.Entities;
using Squawker.Infrastructure.Data;
using Squawker.Infrastructure.Persistence.Repositories;

namespace Squawker.Infrastructure.IntegrationTests.Persistence;

public class SquawkRepositoryTests
{
    private ApplicationDbContext _context;
    private SquawkRepository _repository;
    private Mock<ITelemetryService> _telemetryMock;
    
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"SquawkDb_{Guid.NewGuid()}")
            .Options;
            
        _context = new ApplicationDbContext(options);
        
        _telemetryMock = new Mock<ITelemetryService>();
        // Setup telemetry mock to return null activity to avoid NullReferenceException
        _telemetryMock
            .Setup(m => m.StartActivity(It.IsAny<string>(), It.IsAny<ActivityKind>()))
            .Returns((Activity?)null);
            
        _repository = new SquawkRepository(_context, _telemetryMock.Object);
    }
    
    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
    
    [Test]
    public async Task CreateSquawkAsync_AddsSquawkToDatabase()
    {
        // Arrange
        var squawk = new Squawk
        {
            Id = Guid.NewGuid(),
            Content = "Test squawk",
            CreatedBy = Guid.NewGuid(),
            Created = DateTime.UtcNow
        };
        
        // Act
        var id = await _repository.CreateSquawkAsync(squawk, CancellationToken.None);
        
        // Assert
        id.Should().Be(squawk.Id);
        
        // Fix: Use FindAsync with proper array parameter and null check
        var dbSquawk = await _context.Squawks.FindAsync(new object[] { id }, CancellationToken.None);
        
        // Use FluentAssertions with descriptive message
        dbSquawk.Should().NotBeNull("because the squawk should be saved to the database");
        
        // Fix: Use null-conditional operator to avoid CS8602
        dbSquawk?.Content.Should().Be("Test squawk", "because the content should be saved correctly");
        
        // Verify telemetry was recorded - with null-safe checks
        _telemetryMock.Verify(
            t => t.AddAttributes(It.Is<Dictionary<string, object?>>(d => 
                d.ContainsKey("db.operation") && 
                d["db.operation"] != null && 
                d["db.operation"]!.ToString() == "insert")),
            Times.Once);
    }
    
    [Test]
    public async Task GetSquawkByIdAsync_WithExistingId_ReturnsSquawk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var squawk = new Squawk
        {
            Id = id,
            Content = "Test squawk for retrieval",
            CreatedBy = Guid.NewGuid(),
            Created = DateTime.UtcNow
        };
        
        _context.Squawks.Add(squawk);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetSquawkByIdAsync(id, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Content.Should().Be("Test squawk for retrieval");
        
        // Verify telemetry attributes - Azure best practice for null safety
        _telemetryMock.Verify(
            t => t.AddAttributes(It.Is<Dictionary<string, object?>>(d => 
                d.ContainsKey("result.found") &&
                d["result.found"] != null && 
                object.Equals(d["result.found"], true))),
            Times.Once);
    }
    
    [Test]
    public async Task GetSquawkByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        
        // Act
        var result = await _repository.GetSquawkByIdAsync(id, CancellationToken.None);
        
        // Assert
        result.Should().BeNull();
        
        // Verify telemetry attributes - Azure best practice for null safety
        _telemetryMock.Verify(
            t => t.AddAttributes(It.Is<Dictionary<string, object?>>(d => 
                d.ContainsKey("result.found") && 
                d["result.found"] != null &&
                object.Equals(d["result.found"], false))),
            Times.Once);
    }
    
    [Test]
    public async Task HasUserPostedDuplicateContentAsync_WithDuplicate_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var content = "This is a unique squawk";
        
        var squawk = new Squawk
        {
            Id = Guid.NewGuid(),
            Content = content,
            CreatedBy = userId,
            Created = DateTime.UtcNow.AddMinutes(-5)
        };
        
        _context.Squawks.Add(squawk);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.HasUserPostedDuplicateContentAsync(userId, content, CancellationToken.None);
        
        // Assert
        result.Should().BeTrue();
        
        // Verify telemetry
        _telemetryMock.Verify(
            t => t.RecordEvent("squawk.duplicate_detected", It.IsAny<Dictionary<string, object?>>()),
            Times.Once);
    }
    
    [Test]
    public async Task GetMostRecentSquawkByUserAsync_ReturnsLatestSquawk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var olderSquawk = new Squawk
        {
            Id = Guid.NewGuid(),
            Content = "Older squawk",
            CreatedBy = userId,
            Created = DateTime.UtcNow.AddMinutes(-10)
        };
        
        var newerSquawk = new Squawk
        {
            Id = Guid.NewGuid(),
            Content = "Newer squawk",
            CreatedBy = userId,
            Created = DateTime.UtcNow.AddMinutes(-2)
        };
        
        _context.Squawks.AddRange(olderSquawk, newerSquawk);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetMostRecentSquawkByUserAsync(userId, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(newerSquawk.Id);
        result.Content.Should().Be("Newer squawk");
    }
}