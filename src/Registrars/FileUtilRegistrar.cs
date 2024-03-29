﻿using Microsoft.Extensions.DependencyInjection;
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
    public static void AddFileUtilAsScoped(this IServiceCollection services)
    {
        services.AddMemoryStreamUtil();
        services.TryAddScoped<IFileUtil, FileUtil>();
    }

    /// <summary>
    /// Adds <see cref="IFileUtil"/> as a singleton service. <para/>
    /// </summary>
    public static void AddFileUtilAsSingleton(this IServiceCollection services)
    {
        services.AddMemoryStreamUtil();
        services.TryAddSingleton<IFileUtil, FileUtil>();
    }
}