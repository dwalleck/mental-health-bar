using ReactiveUI;

namespace MentalHealthBar.Desktop.ViewModels;

public class ViewModelBase : ReactiveObject
{
    /// <summary>
    /// Sets the property and raises change notification using ReactiveUI's RaiseAndSetIfChanged.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">New value for the property.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>True if the value was changed, false otherwise.</returns>
    protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        return this.RaiseAndSetIfChanged(ref field, value, propertyName);
    }
}
