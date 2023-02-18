using Microsoft.Extensions.DependencyInjection;
using Soenneker.Utils.FileSync;
using Soenneker.Utils.FileSync.Abstract;

namespace Soenneker.Utils.File.Extensions;

public static class FileUtilRegistrar
{
    /// <summary>
    /// Adds IFileUtil as a scoped service. <para/>
    /// Shorthand for <code>services.AddScoped</code> <para/>
    /// </summary>
    public static void AddFileUtil(this IServiceCollection services)
    {
        services.AddScoped<IFileUtilSync, FileUtilSync>();
    }
}