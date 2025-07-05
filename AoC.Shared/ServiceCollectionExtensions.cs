using AoC.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AoC.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        // Register the TimeProvider as a singleton
        services.AddSingleton<TimeProvider>();
        
        // Register the CalendarService as a singleton
        services.AddSingleton<ICalendarService, CalendarService>();

        return services;
    }
}