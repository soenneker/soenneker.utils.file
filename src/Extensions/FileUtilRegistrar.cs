using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.MemoryStream.Extensions;

namespace Soenneker.Utils.File.Extensions;

public static class FileUtilRegistrar
{
    /// <summary>
    /// Adds IFileUtil as a scoped service. <para/>
    /// </summary>
    public static void AddFileUtilAsScoped(this IServiceCollection services)
    {
        services.AddMemoryStreamUtil();
        services.TryAddScoped<IFileUtil, FileUtil>();
    }

    /// <summary>
    /// Adds IFileUtil as a singleton service. <para/>
    /// </summary>
    public static void AddFileUtilAsSingleton(this IServiceCollection services)
    {
        services.AddMemoryStreamUtil();
        services.TryAddSingleton<IFileUtil, FileUtil>();
    }
}