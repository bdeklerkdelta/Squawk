using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Squawker.Application.Common.Interfaces;
using Squawker.Application.Squawks;
using Squawker.Application.Squawks.Queries.GetSquawks;
using Squawker.Domain.Entities;

namespace Squawker.Application.UnitTests.Squawks.Queries;

public class GetSquawksQueryTests
{
    private Mock<ISquawkRepository> _repositoryMock;
    private Mock<IMapper> _mapperMock;
    private Mock<ITelemetryService> _telemetryMock;
    private GetSquawksQueryHandler _handler;
    
    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ISquawkRepository>();
        _mapperMock = new Mock<IMapper>();
        _telemetryMock = new Mock<ITelemetryService>();
        
        // Setup telemetry mock to return null activity to avoid NullReferenceException
        _telemetryMock
            .Setup(m => m.StartActivity(It.IsAny<string>(), It.IsAny<ActivityKind>()))
            .Returns((System.Diagnostics.Activity?)null);
            
        _handler = new GetSquawksQueryHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _telemetryMock.Object);
    }
    
    [Test]
    public async Task Handle_ReturnsSquawkDtos()
    {
        // Arrange
        var query = new GetSquawksQuery();
        
        var squawks = new List<Squawk>
        {
            new Squawk 
            { 
                Id = Guid.NewGuid(),
                Content = "First squawk",
                CreatedBy = Guid.NewGuid(),
                Created = DateTime.UtcNow.AddDays(-1)
            },
            new Squawk 
            { 
                Id = Guid.NewGuid(),
                Content = "Second squawk",
                CreatedBy = Guid.NewGuid(),
                Created = DateTime.UtcNow
            }
        };
        
        _repositoryMock
            .Setup(r => r.GetAllSquawksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(squawks);
            
        // Setup mapping for each squawk to a DTO
        for (int i = 0; i < squawks.Count; i++)
        {
            var squawk = squawks[i];
            var dto = new SquawkDto
            {
                Id = squawk.Id,
                Content = squawk.Content,
                CreatedBy = squawk.CreatedBy,
                CreatedAt = squawk.Created
            };
            
            _mapperMock
                .Setup(m => m.Map<SquawkDto>(squawk))
                .Returns(dto);
        }
        
        // Act
        _handler.Should().NotBeNull("Handler should be initialized in Setup.");
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        
        // Most recent should be first
        result.First().Content.Should().Be("First squawk");
        
        // Verify telemetry using expression-tree-compatible approach
        _telemetryMock.Verify(
            t => t.AddAttributes(It.Is<Dictionary<string, object?>>(d => 
                d.ContainsKey("query.result_count") && 
                d["query.result_count"] != null && 
                object.Equals(d["query.result_count"], 2))),
            Times.AtLeastOnce);
    }
    
    [Test]
    public async Task Handle_NoSquawks_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetSquawksQuery();
        
        _repositoryMock
            .Setup(r => r.GetAllSquawksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Squawk>());
            
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        // Add telemetry verification for empty results - Azure best practice
        _telemetryMock.Verify(
            t => t.AddAttributes(It.Is<Dictionary<string, object?>>(d => 
                d.ContainsKey("query.result_count") && 
                object.Equals(d["query.result_count"], 0))),
            Times.AtLeastOnce);
    }
}