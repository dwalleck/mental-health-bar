using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PHQ9Tracker
{
    public class DarkModeManager
    {
        private MainWindow mainWindow;

        public DarkModeManager(MainWindow window)
        {
            mainWindow = window;
        }

        public void ApplyDarkMode()
        {
            mainWindow.MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C2C2C"));
            mainWindow.QuestionsBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C3C3C"));
            mainWindow.ChartBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C3C3C"));
            
            foreach (var child in mainWindow.QuestionsPanel.Children)
            {
                if (child is Border border)
                {
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C4C4C"));
                    if (border.Child is Grid grid)
                    {
                        foreach (var gridChild in grid.Children)
                        {
                            if (gridChild is TextBlock textBlock)
                            {
                                textBlock.Foreground = Brushes.White;
                            }
                        }
                    }
                }
            }

            mainWindow.ScoreChart.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C3C3C"));
            mainWindow.ScoreChart.Foreground = Brushes.White;
        }

        public void ApplyLightMode()
        {
            mainWindow.MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
            mainWindow.QuestionsBorder.Background = Brushes.White;
            mainWindow.ChartBorder.Background = Brushes.White;

            foreach (var child in mainWindow.QuestionsPanel.Children)
            {
                if (child is Border border)
                {
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
                    if (border.Child is Grid grid)
                    {
                        foreach (var gridChild in grid.Children)
                        {
                            if (gridChild is TextBlock textBlock)
                            {
                                textBlock.Foreground = Brushes.Black;
                            }
                        }
                    }
                }
            }

            mainWindow.ScoreChart.Background = Brushes.White;
            mainWindow.ScoreChart.Foreground = Brushes.Black;
        }
    }
}