using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Storage;

public static class AddStorageExtention
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        string storageConnectionString,
        TimeProvider? dateTime = null)
    {
        dateTime ??= TimeProvider.System;
        var storageService = new StorageService(storageConnectionString, dateTime);
        services.AddSingleton<StorageService>(sp => storageService);
        services.AddSingleton<IStorageService>(sp => storageService);
        return services;
    }
}
