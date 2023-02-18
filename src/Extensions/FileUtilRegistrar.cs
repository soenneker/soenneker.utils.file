using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.FileSync;
using Soenneker.Utils.FileSync.Abstract;
using Soenneker.Utils.MemoryStream;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.File.Extensions;

public static class FileUtilRegistrar
{
    /// <summary>
    /// Adds IFileUtil as a scoped service. <para/>
    /// Shorthand for <code>services.AddScoped</code> <para/>
    /// </summary>
    public static void AddFileUtil(this IServiceCollection services)
    {
        services.TryAddScoped<IMemoryStreamUtil, MemoryStreamUtil>();
        services.AddScoped<IFileUtilSync, FileUtilSync>();
    }
}