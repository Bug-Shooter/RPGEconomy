namespace RPGEconomy.Testing;

public sealed class GlobalTestDatabaseLock : IAsyncDisposable
{
    private static readonly string LockFilePath =
        Path.Combine(Path.GetTempPath(), "RPGEconomy.IntegrationTests.Database.lock");

    private FileStream? _lockStream;

    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LockFilePath)!);

        while (_lockStream is null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _lockStream = new FileStream(
                    LockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_lockStream is not null)
        {
            _lockStream.Dispose();
            _lockStream = null;
        }

        return ValueTask.CompletedTask;
    }
}
