using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;

namespace PHQ9Tracker
{
    public class QuestionnaireManager
    {
        private List<string> questions;
        private List<ComboBox> answerBoxes;

        public event Action AnswerChanged;

        public int TotalQuestions => questions.Count;
        public int AnsweredQuestions => answerBoxes.Count(box => box.SelectedIndex != -1);

        public QuestionnaireManager(StackPanel questionsPanel)
        {
            questions = new List<string>
            {
                "Little interest or pleasure in doing things",
                "Feeling down, depressed, or hopeless",
                "Trouble falling or staying asleep, or sleeping too much",
                "Feeling tired or having little energy",
                "Poor appetite or overeating",
                "Feeling bad about yourself or that you are a failure or have let yourself or your family down",
                "Trouble concentrating on things, such as reading the newspaper or watching television",
                "Moving or speaking so slowly that other people could have noticed. Or the opposite – being so fidgety or restless that you have been moving around a lot more than usual",
                "Thoughts that you would be better off dead, or of hurting yourself"
            };

            answerBoxes = new List<ComboBox>();
            CreateQuestions(questionsPanel);
        }

        private void CreateQuestions(StackPanel questionsPanel)
        {
            for (int i = 0; i < questions.Count; i++)
            {
                Border questionBorder = new Border
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                    CornerRadius = new CornerRadius(5)
                };

                Grid questionGrid = new Grid();
                questionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                questionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

                TextBlock questionText = new TextBlock
                {
                    Text = $"{i + 1}. {questions[i]}",
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(questionText, 0);

                ComboBox answerBox = new ComboBox
                {
                    Width = 150,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center
                };
                answerBox.Items.Add("Not at all");
                answerBox.Items.Add("Several days");
                answerBox.Items.Add("More than half the days");
                answerBox.Items.Add("Nearly every day");
                answerBox.SelectionChanged += (s, e) => AnswerChanged?.Invoke();
                Grid.SetColumn(answerBox, 1);

                questionGrid.Children.Add(questionText);
                questionGrid.Children.Add(answerBox);

                questionBorder.Child = questionGrid;
                questionsPanel.Children.Add(questionBorder);

                answerBoxes.Add(answerBox);
            }
        }

        public int CalculateScore()
        {
            return answerBoxes.Sum(box => box.SelectedIndex != -1 ? box.SelectedIndex : 0);
        }

        public string GetScoreInterpretation(int score)
        {
            if (score >= 0 && score <= 4) return "Minimal depression";
            if (score >= 5 && score <= 9) return "Mild depression";
            if (score >= 10 && score <= 14) return "Moderate depression";
            if (score >= 15 && score <= 19) return "Moderately severe depression";
            return "Severe depression";
        }
    }
}