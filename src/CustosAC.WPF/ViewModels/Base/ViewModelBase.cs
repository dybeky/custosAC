using CommunityToolkit.Mvvm.ComponentModel;

namespace CustosAC.WPF.ViewModels.Base;

/// <summary>
/// Base class for all ViewModels with IDisposable support
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
    }
}
