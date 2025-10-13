using System;
using System.Threading;
using ReactiveUI;

namespace MentalHealthBar.Desktop.ViewModels;

public class ViewModelBase : ReactiveObject, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    /// <summary>
    /// CancellationToken that can be used to cancel long-running operations.
    /// Automatically cancelled when the ViewModel is disposed.
    /// </summary>
    protected CancellationToken CancellationToken
    {
        get
        {
            _cancellationTokenSource ??= new CancellationTokenSource();
            return _cancellationTokenSource.Token;
        }
    }

    /// <summary>
    /// Sets the property and raises change notification using ReactiveUI's RaiseAndSetIfChanged.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">New value for the property.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>The new value.</returns>
    protected T SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        this.RaiseAndSetIfChanged(ref field, value, propertyName);
        return value;
    }

    /// <summary>
    /// Disposes the ViewModel and cancels any pending operations.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }

            _disposed = true;
        }
    }
}
