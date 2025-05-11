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
using Squawker.Application.Common.Interfaces;
using Squawker.Application.Squawks.Commands.CreateSquawk;
using Squawker.Domain.Entities;

namespace Squawker.Application.UnitTests.Squawks.Commands;

public class CreateSquawkCommandTests
{
    private Mock<ISquawkRepository> _repositoryMock;
    private Mock<ITelemetryService> _telemetryMock;
    private CreateSquawkCommandHandler _handler;
    
    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ISquawkRepository>();
        _telemetryMock = new Mock<ITelemetryService>();
        
        // Setup telemetry mock to return null activity to avoid NullReferenceException
        _telemetryMock
            .Setup(m => m.StartActivity(It.IsAny<string>(), It.IsAny<ActivityKind>()))
            .Returns((Activity?)null);
            
        _handler = new CreateSquawkCommandHandler(
            _repositoryMock.Object,
            _telemetryMock.Object);
    }
    
    [Test]
    public async Task Handle_ValidCommand_ReturnsSquawkId()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = new CreateSquawkCommand
        {
            Content = "Test squawk content",
            UserId = Guid.NewGuid()
        };
        
        _repositoryMock
            .Setup(r => r.CreateSquawkAsync(It.IsAny<Squawk>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGuid);
            
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.Should().Be(expectedGuid);
        
        // Verify telemetry was used properly
        _telemetryMock.Verify(
            t => t.StartActivity("CreateSquawk.Handle", ActivityKind.Internal),
            Times.Once);
            
        // Fix: Use a null-safe approach to verify dictionary values
        _telemetryMock.Verify(
            t => t.AddAttributes(It.Is<Dictionary<string, object?>>(d => 
                d.ContainsKey("squawk.user_id") && 
                d["squawk.user_id"] != null &&
                object.Equals(d["squawk.user_id"]!.ToString(), command.UserId.ToString()))),
            Times.Once);
    }
    
    [Test]
    public async Task Handle_ExceptionInRepository_RecordsExceptionInTelemetry()
    {
        // Arrange
        var command = new CreateSquawkCommand
        {
            Content = "Test squawk content",
            UserId = Guid.NewGuid()
        };
        
        var testException = new InvalidOperationException("Test exception");
        _repositoryMock
            .Setup(r => r.CreateSquawkAsync(It.IsAny<Squawk>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(testException);
            
        // Act & Assert
        await FluentActions.Invoking(() => 
            _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
            
        // Verify exception was recorded in telemetry
        _telemetryMock.Verify(
            t => t.RecordException(testException),
            Times.Once);
            
        _telemetryMock.Verify(
            t => t.SetStatus(ActivityStatusCode.Error, It.IsAny<string>()),
            Times.Once);
    }
}