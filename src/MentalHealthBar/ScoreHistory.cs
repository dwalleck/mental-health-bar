using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PHQ9Tracker
{
    public class ScoreHistory
    {
        private List<ScoreEntry> scores;
        private const string FileName = "phq9_scores.json";
        private string filePath;

        public IReadOnlyList<ScoreEntry> Scores => scores.AsReadOnly();

        public ScoreHistory(bool useOneDrive = false)
        {
            scores = new List<ScoreEntry>();
            SetFilePath(useOneDrive);
        }

        private void SetFilePath(bool useOneDrive)
        {
            if (useOneDrive)
            {
                string oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");
                if (!string.IsNullOrEmpty(oneDrivePath))
                {
                    filePath = Path.Combine(oneDrivePath, "PHQ9Tracker", FileName);
                }
                else
                {
                    // Fallback to user-level directory if OneDrive is not available
                    filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHQ9Tracker", FileName);
                }
            }
            else
            {
                filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHQ9Tracker", FileName);
            }

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        public void AddScore(int score)
        {
            scores.Add(new ScoreEntry { Date = DateTime.Now, Score = score });
        }

        public async Task SaveScoreHistoryAsync()
        {
            string json = JsonConvert.SerializeObject(scores);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task LoadScoreHistoryAsync()
        {
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                scores = JsonConvert.DeserializeObject<List<ScoreEntry>>(json);
            }
        }

        // Synchronous methods for backwards compatibility
        public void SaveScoreHistory()
        {
            SaveScoreHistoryAsync().GetAwaiter().GetResult();
        }

        public void LoadScoreHistory()
        {
            LoadScoreHistoryAsync().GetAwaiter().GetResult();
        }
    }

    public class ScoreEntry
    {
        public DateTime Date { get; set; }
        public int Score { get; set; }
    }
}