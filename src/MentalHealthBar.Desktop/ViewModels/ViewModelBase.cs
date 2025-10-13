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
    /// <returns>The new value.</returns>
    protected T SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        this.RaiseAndSetIfChanged(ref field, value, propertyName);
        return value;
    }
}
