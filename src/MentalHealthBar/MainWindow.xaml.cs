using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using ScottPlot;
using ScottPlot.WPF;

namespace PHQ9Tracker
{
    public partial class MainWindow : Window
    {
        private QuestionnaireManager questionnaireManager;
        private ScoreHistory scoreHistory;
        private DarkModeManager darkModeManager;

        public MainWindow()
        {
            InitializeComponent();
            questionnaireManager = new QuestionnaireManager(QuestionsPanel);
            scoreHistory = new ScoreHistory(useOneDrive: true);
            darkModeManager = new DarkModeManager(this);

            questionnaireManager.AnswerChanged += UpdateProgressBar;
            LoadScoreHistory();
            InitializeChart();
            UpdateProgressBar();
        }

        private void InitializeChart()
        {
            ScoreChart.Plot.Title("PHQ-9 Scores Over Time");
            ScoreChart.Plot.XLabel("Date");
            ScoreChart.Plot.YLabel("Score");
            ScoreChart.Refresh();
        }

        private void UpdateProgressBar()
        {
            int answeredQuestions = questionnaireManager.AnsweredQuestions;
            QuestionProgressBar.Value = (double)answeredQuestions / questionnaireManager.TotalQuestions * 100;
            ProgressText.Text = $"Progress: {answeredQuestions}/{questionnaireManager.TotalQuestions} questions answered";
        }

        private async void LoadScoreHistory()
        {
            await scoreHistory.LoadScoreHistoryAsync();
            UpdateChart();
        }

        private async void CalculateScore_Click(object sender, RoutedEventArgs e)
        {
            int totalScore = questionnaireManager.CalculateScore();
            string interpretation = questionnaireManager.GetScoreInterpretation(totalScore);

            MessageBox.Show($"Your PHQ-9 score is: {totalScore}\n\n{interpretation}", "PHQ-9 Score Result");

            scoreHistory.AddScore(totalScore);
            await scoreHistory.SaveScoreHistoryAsync();
            UpdateChart();
        }

        private async void SaveResults_Click(object sender, RoutedEventArgs e)
        {
            await scoreHistory.SaveScoreHistoryAsync();
            MessageBox.Show("Results saved successfully!", "Save Results");
        }

        private void UpdateChart()
        {
            ScoreChart.Plot.Clear();

            if (scoreHistory.Scores.Any())
            {
                var scores = scoreHistory.Scores.Select(s => (double)s.Score).ToArray();
                var dates = scoreHistory.Scores.Select(s => s.Date.ToOADate()).ToArray();

                ScoreChart.Plot.Add.Scatter(dates, scores);
                ScoreChart.Plot.Axes.DateTimeX(dateStyle: ScottPlot.TickDateStyle.ShortDate);

                ScoreChart.Plot.SetAxisLimits(yMin: 0, yMax: 27);
                ScoreChart.Plot.Title("PHQ-9 Scores Over Time");
                ScoreChart.Plot.XLabel("Date");
                ScoreChart.Plot.YLabel("Score");
            }
            else
            {
                ScoreChart.Plot.Title("No data available");
            }

            ScoreChart.Refresh();
        }

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            darkModeManager.ApplyDarkMode();
            UpdateChartColors(true);
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            darkModeManager.ApplyLightMode();
            UpdateChartColors(false);
        }

        private void UpdateChartColors(bool isDarkMode)
        {
            var plt = ScoreChart.Plot;
            if (isDarkMode)
            {
                plt.Style.Background = System.Drawing.ColorTranslator.FromHtml("#2C2C2C");
                plt.Style.DataBackground = System.Drawing.ColorTranslator.FromHtml("#3C3C3C");
                plt.ForeColor = System.Drawing.Color.White;
            }
            else
            {
                plt.Style.Background = System.Drawing.Color.White;
                plt.Style.DataBackground = System.Drawing.Color.White;
                plt.ForeColor = System.Drawing.Color.Black;
            }
            ScoreChart.Refresh();
        }
    }
}