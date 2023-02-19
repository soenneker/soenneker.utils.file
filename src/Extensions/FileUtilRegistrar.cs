using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.MemoryStream.Extensions;

namespace Soenneker.Utils.File.Extensions;

public static class FileUtilRegistrar
{
    /// <summary>
    /// Adds IFileUtil as a scoped service. <para/>
    /// Shorthand for <code>services.AddScoped</code> <para/>
    /// </summary>
    public static void AddFileUtil(this IServiceCollection services)
    {
        services.AddMemoryStreamUtil();
        services.TryAddScoped<IFileUtil, FileUtil>();
    }
}