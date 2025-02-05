using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.MemoryStream.Registrars;

namespace Soenneker.Utils.File.Registrars;

/// <summary>
/// A utility library encapsulating asynchronous file IO operations
/// </summary>
public static class FileUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IFileUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddFileUtilAsScoped(this IServiceCollection services)
    {
        services.AddMemoryStreamUtilAsSingleton()
                .TryAddScoped<IFileUtil, FileUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IFileUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddFileUtilAsSingleton(this IServiceCollection services)
    {
        services.AddMemoryStreamUtilAsSingleton()
                .TryAddSingleton<IFileUtil, FileUtil>();

        return services;
    }
}