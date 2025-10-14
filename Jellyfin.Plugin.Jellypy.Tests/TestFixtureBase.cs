using Xunit;
using Moq;
using MediaBrowser.Controller;
using Jellyfin.Plugin.Jellypy.Configuration;

namespace Jellyfin.Plugin.Jellypy.Tests;

/// <summary>
/// Base class for test fixtures that require EncryptionHelper initialization.
/// </summary>
public class TestFixtureBase : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestFixtureBase"/> class.
    /// Sets up EncryptionHelper with a mock IServerApplicationHost.
    /// </summary>
    public TestFixtureBase()
    {
        // Create a mock IServerApplicationHost
        var mockApplicationHost = new Mock<IServerApplicationHost>();
        mockApplicationHost.Setup(x => x.SystemId).Returns("test-server-id-12345");

        // Initialize EncryptionHelper with the mock
        EncryptionHelper.Initialize(mockApplicationHost.Object);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if disposing from user code, false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Cleanup if needed
        }

        _disposed = true;
    }
}
