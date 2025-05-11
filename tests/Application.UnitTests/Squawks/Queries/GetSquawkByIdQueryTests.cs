using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Squawker.Application.Common.Exceptions;
using Squawker.Application.Common.Interfaces;
using Squawker.Application.Squawks;
using Squawker.Application.Squawks.Queries.GetSquawkById;
using Squawker.Domain.Entities;

namespace Squawker.Application.UnitTests.Squawks.Queries;

public class GetSquawkByIdQueryTests
{
    private Mock<ISquawkRepository> _repositoryMock;
    private Mock<IMapper> _mapperMock;
    private Mock<ITelemetryService> _telemetryMock;
    private GetSquawkByIdQueryHandler _handler;
    
    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ISquawkRepository>();
        _mapperMock = new Mock<IMapper>();
        _telemetryMock = new Mock<ITelemetryService>();
        
        // Setup telemetry mock to return null activity to avoid NullReferenceException
        // Use explicit parameters instead of optional arguments for Expression Trees
        _telemetryMock
            .Setup(m => m.StartActivity(It.IsAny<string>(), It.IsAny<ActivityKind>()))
            .Returns((Activity?)null);
            
        _handler = new GetSquawkByIdQueryHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _telemetryMock.Object);
    }
    
    [Test]
    public async Task Handle_ExistingSquawk_ReturnsSquawkDto()
    {
        // Arrange
        var squawkId = Guid.NewGuid();
        var query = new GetSquawkByIdQuery(squawkId);
        
        var squawk = new Squawk
        {
            Id = squawkId,
            Content = "Test content",
            CreatedBy = Guid.NewGuid(),
            Created = DateTime.UtcNow
        };
        
        var expectedDto = new SquawkDto
        {
            Id = squawkId,
            Content = "Test content",
            CreatedBy = squawk.CreatedBy,
            CreatedAt = squawk.Created
        };
        
        _repositoryMock
            .Setup(r => r.GetSquawkByIdAsync(squawkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(squawk);
            
        _mapperMock
            .Setup(m => m.Map<SquawkDto>(squawk))
            .Returns(expectedDto);
            
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(squawkId);
        
        // Verify telemetry using null-safe approach without optional arguments
        _telemetryMock.Verify(
            t => t.AddAttributes(It.Is<Dictionary<string, object?>>(d => 
                d.ContainsKey("squawk.id") && 
                d["squawk.id"] != null && 
                d["squawk.id"]!.ToString() == squawkId.ToString())),
            Times.AtLeastOnce());
    }
}