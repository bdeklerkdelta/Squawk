using Squawker.Application.Common.Interfaces;
using System;

namespace Squawker.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}