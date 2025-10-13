using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MentalHealthBar.Desktop.ViewModels;

namespace MentalHealthBar.Desktop.Views;

public partial class MoodEntryView : UserControl
{
    public MoodEntryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Initialize the ViewModel when the view is loaded
        if (DataContext is MoodEntryViewModel viewModel)
        {
            try
            {
                await viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                // Log error - exceptions are already handled within the ViewModel
                Console.WriteLine($"Error initializing MoodEntryView: {ex.Message}");
            }
        }
    }
}