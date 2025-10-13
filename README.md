# Mental Health Bar 🧠

A personal wellness tracking desktop application for monitoring mental health through mood logging, standardized clinical assessments, and health metrics visualization.

## Features

- **📝 Mood Tracking**: Log daily mood scores (1-5 scale) with optional tags and notes
- **📋 Clinical Assessments**: Complete standardized questionnaires (PHQ-9, BDI, GAD-7, BAI)
- **💪 Health Metrics**: Track sleep hours and water intake
- **📈 Data Visualization**: View trends over time with interactive charts
- **🏷️ Event Labels**: Tag entries with life events to identify patterns
- **💾 Data Export**: Export all data to CSV or JSON formats

## Quick Start (< 5 minutes)

### Prerequisites

- [.NET 10 RC SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Git

### Installation

#### 1. Clone Repository
```bash
git clone https://github.com/dwalleck/mental-health-bar.git
cd mental-health-bar
```

#### 2. Start Database
```bash
docker-compose up -d
```

#### 3. Initialize Database
```bash
cd src/MentalHealthBar.Api
dotnet ef database update
```

#### 4. Start Backend API
```bash
# From src/MentalHealthBar.Api
dotnet run
```
API will start at `https://localhost:5001` with Swagger at `/swagger`

#### 5. Start Desktop Application
Open a new terminal:
```bash
cd src/MentalHealthBar.Desktop
dotnet run
```

The desktop application will launch and connect to the API automatically.

## Architecture

### Technology Stack

- **Frontend**: Avalonia UI (cross-platform desktop framework)
- **Backend**: ASP.NET Core 10 RC Minimal API
- **Database**: PostgreSQL with Entity Framework Core
- **Patterns**: Vertical Slice Architecture with MediatR
- **Testing**: TUnit with contract, integration, and unit tests

### Project Structure

```
mental-health-bar/
├── src/
│   ├── MentalHealthBar.Api/         # Backend API
│   │   ├── Domain/                  # Domain models
│   │   ├── Features/                # Vertical slices
│   │   └── Infrastructure/          # EF Core, persistence
│   ├── MentalHealthBar.Desktop/     # Desktop application
│   │   ├── Views/                   # Avalonia XAML views
│   │   ├── ViewModels/              # MVVM ViewModels
│   │   └── Services/                # API client, charting
│   └── MentalHealthBar.Contracts/   # Shared DTOs
├── tests/
│   ├── MentalHealthBar.Api.Tests/
│   └── MentalHealthBar.Desktop.Tests/
├── docker-compose.yml                # PostgreSQL setup
└── Directory.Packages.props         # Centralized package management
```

## Usage Guide

### First Time Setup

1. **Launch the application** - The dashboard will open
2. **Navigate with tabs** - Use the left sidebar to switch between features
3. **Start tracking** - Begin with a simple mood entry

### Core Workflows

#### Log a Mood Entry
1. Click "Log Mood" tab
2. Select your mood score (1-5)
3. Optionally add tags for context
4. Add notes if desired
5. Click "Save Entry"

#### Take an Assessment
1. Click "Assessments" tab
2. Select assessment type (PHQ-9, BDI, GAD-7, or BAI)
3. Answer all questions
4. Submit to see your score and severity level

#### View Trends
1. Click "Trends" tab
2. Select date range (7, 30, 90 days)
3. Toggle between mood, assessments, and health metrics
4. Use "Show Daily Average" for mood aggregation

#### Export Data
1. Click "Export" tab
2. Select date range
3. Choose data types to include
4. Select format (CSV or JSON)
5. Click "Export Data" and choose save location

## Development

### Build from Source
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Database Migrations
```bash
cd src/MentalHealthBar.Api
dotnet ef migrations add [MigrationName]
dotnet ef database update
```

## Configuration

### API Settings
Edit `src/MentalHealthBar.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mentalhealthbar;Username=postgres;Password=postgres"
  }
}
```

### Desktop Settings
The API URL is configured in `src/MentalHealthBar.Desktop/App.axaml.cs`:
```csharp
client.BaseAddress = new Uri("https://localhost:5001/");
```

## Performance Targets

- **UI Responsiveness**: < 100ms for data entry
- **Chart Rendering**: < 500ms for graphs
- **Data Export**: < 1 second for typical datasets

## Data Privacy

- All data stored locally on your machine
- No cloud synchronization
- No external API calls
- Single-user design (no authentication needed)

## Troubleshooting

### Database Connection Issues
- Ensure Docker is running: `docker-compose ps`
- Check PostgreSQL logs: `docker-compose logs postgres`
- Verify connection string in appsettings.json

### API Not Starting
- Check port 5001 is available: `netstat -an | grep 5001`
- Verify .NET 10 RC installed: `dotnet --version`
- Check for build errors: `dotnet build`

### Desktop App Not Connecting
- Ensure API is running first
- Check API URL in App.axaml.cs
- Verify no firewall blocking localhost connections

## Assessment Information

### PHQ-9 (Patient Health Questionnaire)
- 9 questions about depression symptoms
- Score range: 0-27
- Severity levels: Minimal, Mild, Moderate, Moderately Severe, Severe

### GAD-7 (Generalized Anxiety Disorder)
- 7 questions about anxiety symptoms
- Score range: 0-21
- Severity levels: Minimal, Mild, Moderate, Severe

### BDI (Beck Depression Inventory)
- 21 questions about depression
- Score range: 0-63
- Severity levels: Minimal, Mild, Moderate, Severe

### BAI (Beck Anxiety Inventory)
- 21 questions about anxiety
- Score range: 0-63
- Severity levels: Minimal, Mild, Moderate, Severe

## Contributing

This is a personal project, but suggestions are welcome. Please open an issue for discussion before submitting PRs.

## License

MIT License - See LICENSE file for details

## Disclaimer

This application is for personal tracking only and is not a substitute for professional medical advice, diagnosis, or treatment. Always seek the advice of your physician or other qualified health provider with any questions you may have regarding a medical condition.

---

**Version**: 0.1.0
**Status**: MVP Complete ✅
**Last Updated**: October 2025

🤖 Generated with [Claude Code](https://claude.com/claude-code)