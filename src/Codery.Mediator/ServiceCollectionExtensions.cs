using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Codery.Mediator;

/// <summary>
/// Extension methods for registering Codery.Mediator services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Tracks assemblies that have already been scanned to prevent duplicate notification handler registrations
    /// across multiple AddCoderyMediator calls.
    /// </summary>
    private sealed class AssemblyRegistrationMarker
    {
        public HashSet<Assembly> ScannedAssemblies { get; } = [];
        public HashSet<Type> RegisteredBehaviorTypes { get; } = [];
    }
    /// <summary>
    /// Adds Codery.Mediator services and scans the specified assemblies for request and notification handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">One or more assemblies to scan for handler implementations.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCoderyMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return AddCoderyMediator(services, _ => { }, assemblies);
    }

    /// <summary>
    /// Adds Codery.Mediator services with pipeline behavior configuration
    /// and scans the specified assemblies for request and notification handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure pipeline behaviors.</param>
    /// <param name="assemblies">One or more assemblies to scan for handler implementations.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCoderyMediator(
        this IServiceCollection services,
        Action<MediatorOptions> configure,
        params Assembly[] assemblies)
    {
        Internal.ThrowHelper.ThrowIfNull(services);
        Internal.ThrowHelper.ThrowIfNull(configure);
        Internal.ThrowHelper.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
        }

        for (int i = 0; i < assemblies.Length; i++)
        {
            if (assemblies[i] is null)
            {
                throw new ArgumentException("Assemblies array must not contain null elements.", nameof(assemblies));
            }
        }

        // Register core mediator services (TryAdd prevents double registration).
        // Scoped: one instance per DI scope (e.g. per HTTP request), avoids redundant allocations
        // when both ISender and IPublisher are injected into the same class.
        services.TryAddScoped<IMediator, Mediator>();
        services.TryAddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.TryAddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        // Retrieve or create the marker that tracks already-scanned assemblies and behavior types
        // to prevent duplicate registrations across multiple AddCoderyMediator calls.
        var markerDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(AssemblyRegistrationMarker));
        var marker = markerDescriptor?.ImplementationInstance as AssemblyRegistrationMarker ?? new AssemblyRegistrationMarker();
        if (markerDescriptor is null)
        {
            services.AddSingleton(marker);
        }

        // Configure options and register pipeline behaviors (deduplicated across calls)
        var options = new MediatorOptions();
        configure(options);

        foreach (var behaviorType in options.BehaviorTypes)
        {
            if (marker.RegisteredBehaviorTypes.Add(behaviorType))
            {
                services.AddTransient(typeof(IPipelineBehavior<,>), behaviorType);
            }
        }

        // Scan assemblies for handlers (skip already-scanned assemblies)
        foreach (var assembly in assemblies.Distinct())
        {
            if (!marker.ScannedAssemblies.Add(assembly))
            {
                continue; // Already scanned in a previous AddCoderyMediator call
            }

            // Request handlers: one per request type (TryAdd = first wins)
            ScanAndRegister(services, assembly, typeof(IRequestHandler<,>), tryAdd: true);
            // Notification handlers: multiple per notification type (Add = all registered)
            ScanAndRegister(services, assembly, typeof(INotificationHandler<>), tryAdd: false);
        }

        return services;
    }

    private static void ScanAndRegister(
        IServiceCollection services,
        Assembly assembly,
        Type openGenericInterface,
        bool tryAdd)
    {
        var types = GetLoadableTypes(assembly)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false, IsNestedPrivate: false });

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType
                          && i.GetGenericTypeDefinition() == openGenericInterface);

            foreach (var serviceType in interfaces)
            {
                if (tryAdd)
                {
                    services.TryAddTransient(serviceType, type);
                }
                else
                {
                    services.AddTransient(serviceType, type);
                }
            }
        }
    }

    /// <summary>
    /// Gets all loadable types from an assembly, gracefully handling types that cannot be loaded.
    /// </summary>
    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).ToArray()!;
        }
    }
}
