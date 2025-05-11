using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Moq;
using NUnit.Framework;
using Squawker.Application.Common.Interfaces;
using Squawker.Application.Squawks.Commands.CreateSquawk;
using Squawker.Domain.Entities;

namespace Squawker.Application.UnitTests.Squawks.Commands;

public class CreateSquawkCommandValidatorTests
{
    private CreateSquawkCommandValidator _validator;
    private Mock<ISquawkRepository> _repositoryMock;
    private Mock<IDateTime> _dateTimeMock;
    private Mock<ITelemetryService> _telemetryMock;
    
    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ISquawkRepository>();
        _dateTimeMock = new Mock<IDateTime>();
        _telemetryMock = new Mock<ITelemetryService>();
        
        // Set current time for testing rate limits
        _dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);
        
        _validator = new CreateSquawkCommandValidator(
            _repositoryMock.Object,
            _dateTimeMock.Object,
            _telemetryMock.Object);
    }
    
    [Test]
    public async Task Should_Have_Error_When_Content_Is_Empty()
    {
        // Arrange
        var command = new CreateSquawkCommand
        {
            Content = "",
            UserId = Guid.NewGuid()
        };
        
        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }
    
    [Test]
    public async Task Should_Have_Error_When_Content_Exceeds_Max_Length()
    {
        // Arrange
        var command = new CreateSquawkCommand
        {
            Content = new string('x', 401), // 401 chars
            UserId = Guid.NewGuid()
        };
        
        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }
    
    [Test]
    public async Task Should_Have_Error_When_Content_Contains_Banned_Terms()
    {
        // Arrange
        var command = new CreateSquawkCommand
        {
            Content = "This is like a Tweet on Twitter",
            UserId = Guid.NewGuid()
        };
        
        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content cannot contain references to 'Tweet' or 'Twitter'.");
    }
    
    [Test]
    public async Task Should_Not_Have_Error_When_Rate_Limit_Is_Respected()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateSquawkCommand
        {
            Content = "Valid content",
            UserId = userId
        };
        
        // Setup recent squawk by the same user (21 seconds ago)
        var oldSquawk = new Squawk
        {
            Id = Guid.NewGuid(),
            Content = "Previous squawk",
            CreatedBy = userId,
            Created = _dateTimeMock.Object.Now.AddSeconds(-21) // Just over the 20 second limit
        };
        
        _repositoryMock
            .Setup(r => r.GetMostRecentSquawkByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldSquawk);
            
        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }
    
    [Test]
    public async Task Should_Have_Error_When_Content_Is_Duplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var duplicateContent = "This is duplicate content";
        
        var command = new CreateSquawkCommand
        {
            Content = duplicateContent,
            UserId = userId
        };
        
        _repositoryMock
            .Setup(r => r.HasUserPostedDuplicateContentAsync(userId, duplicateContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("You've already posted this exact content. Duplicate squawks are not allowed.");
    }
}