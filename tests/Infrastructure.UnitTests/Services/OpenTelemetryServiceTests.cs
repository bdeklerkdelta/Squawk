using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using Squawker.Infrastructure.Services;

namespace Squawker.Infrastructure.UnitTests.Services;

public class OpenTelemetryServiceTests
{
    private OpenTelemetryService _telemetryService;
    
    [SetUp]
    public void Setup()
    {
        _telemetryService = new OpenTelemetryService();
    }
    
    [Test]
    public void StartActivity_ShouldCreateActivityWithCorrectName()
    {
        // Arrange
        var activityName = "TestActivity";
        var activitySource = typeof(OpenTelemetryService)
            .GetField("_activitySource", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) as ActivitySource;
            
        activitySource.Should().NotBeNull();
        
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        
        // Act
        using var activity = _telemetryService.StartActivity(activityName, ActivityKind.Internal);
        
        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be(activityName);
        activity.Kind.Should().Be(ActivityKind.Internal);
    }
    
    [Test]
    public void AddAttributes_ShouldAddTagsToCurrentActivity()
    {
        // Arrange
        using var activity = new Activity("TestActivity");
        activity.Start();
        
        var attributes = new Dictionary<string, object?>
        {
            ["test.key1"] = "test-value",
            ["test.key2"] = 42
        };
        
        // Act
        _telemetryService.AddAttributes(attributes);
        
        // Assert
        activity.GetTagItem("test.key1").Should().Be("test-value");
        activity.GetTagItem("test.key2").Should().Be(42);
        
        activity.Stop();
    }
    
    [Test]
    public void SetStatus_ShouldSetActivityStatus()
    {
        // Arrange
        using var activity = new Activity("TestActivity");
        activity.Start();
        
        // Act
        _telemetryService.SetStatus(ActivityStatusCode.Ok, "Success");
        
        // Assert
        activity.GetTagItem("status.description").Should().Be("Success");
        
        activity.Stop();
    }
    
    [Test]
    public void RecordException_ShouldAddExceptionDetailsToActivity()
    {
        // Arrange
        using var activity = new Activity("TestActivity");
        activity.Start();
        
        var exception = new InvalidOperationException("Test exception");
        
        // Act
        _telemetryService.RecordException(exception);
        
        // Assert
        activity.GetTagItem("exception.type").Should().Be("System.InvalidOperationException");
        activity.GetTagItem("exception.message").Should().Be("Test exception");
        
        activity.Stop();
    }
    
    [Test]
    public void CreateCounter_ShouldReturnCounter()
    {
        // Act
        var counter = _telemetryService.CreateCounter<int>("test.counter", "items", "Test counter");
        
        // Assert
        counter.Should().NotBeNull();
    }
    
    [Test]
    public void CreateHistogram_ShouldReturnHistogram()
    {
        // Act
        var histogram = _telemetryService.CreateHistogram<double>("test.histogram", "ms", "Test histogram");
        
        // Assert
        histogram.Should().NotBeNull();
    }
}