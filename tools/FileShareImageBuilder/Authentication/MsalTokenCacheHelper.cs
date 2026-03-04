using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace FileShareImageBuilder.Authentication;

internal static class MsalTokenCacheHelper
{
    private static readonly object FileLock = new();

    internal static string CacheFilePath { get; } = GetCacheFilePath();

    private static string GetCacheFilePath()
    {
        try
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "fss-msalcache.bin");
        }
        catch (PlatformNotSupportedException)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fss-msalcache.bin");
        }
    }

    public static void EnableSerialization(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(BeforeAccessNotification);
        tokenCache.SetAfterAccess(AfterAccessNotification);
    }

    private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        lock (FileLock)
        {
            args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser)
                : null);
        }
    }

    private static void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged) return;

        lock (FileLock)
        {
            File.WriteAllBytes(CacheFilePath, ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser));
        }
    }
}