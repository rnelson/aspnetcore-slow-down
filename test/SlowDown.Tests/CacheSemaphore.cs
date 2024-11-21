namespace SlowDown.Tests;

internal static class CacheSemaphore
{
    public static readonly SemaphoreSlim Semaphore = new(1,1);
}