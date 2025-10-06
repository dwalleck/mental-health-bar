# Quick Start Guide - Mental Health Bar

**Target Time**: 5 minutes from clone to running application
**Prerequisites**: .NET 10 RC SDK, Docker, Git

---

## Installation

### 1. Clone and Setup (1 minute)

```bash
# Clone repository
git clone https://github.com/[username]/mental-health-bar.git
cd mental-health-bar

# Verify .NET 10 RC installed
dotnet --version  # Should show 10.0.x

# Restore packages
dotnet restore
```

### 2. Start Database (1 minute)

```bash
# Start PostgreSQL via Docker Compose
docker-compose up -d

# Verify database is running
docker-compose ps
# Should show postgres container with status "Up"
```

### 3. Initialize Database (30 seconds)

```bash
# Run migrations and seed assessment templates
cd backend/src/MentalHealthBar.Api
dotnet ef database update

# Seed will automatically run on first launch
```

### 4. Start Backend API (30 seconds)

```bash
# From backend/src/MentalHealthBar.Api directory
dotnet run

# API should start on https://localhost:5001
# Swagger UI available at https://localhost:5001/swagger
```

### 5. Start Desktop App (2 minutes)

Open a new terminal:

```bash
cd frontend/src/MentalHealthBar.Desktop
dotnet run

# Desktop app should launch and connect to API
```

---

## First Use - Validation Checklist

### ✅ Backend API Health Check
1. Open browser to `https://localhost:5001/swagger`
2. Verify all endpoint groups visible:
   - Assessments
   - MoodEntries
   - HealthMetrics
   - EventLabels
   - Export

### ✅ Desktop App Launch
1. Desktop window opens (Avalonia UI)
2. Dashboard view displays with:
   - Welcome message
   - Quick action buttons (Log Mood, Take Assessment, View Trends)
   - Empty state graphics (no data yet)

### ✅ Complete First User Flow

**Scenario: Log a mood entry and view it**

1. **Create Event Label**
   - Click "Manage Tags" (or from mood entry screen)
   - Click "+ New Tag"
   - Enter name: "testing"
   - Click Save
   - ✅ Tag appears in list

2. **Log Mood Entry**
   - Click "Log Mood" button
   - Select mood score: 4 (Above Average)
   - Select tag: "testing"
   - Enter notes (optional): "First test entry"
   - Click Save
   - ✅ Success message appears
   - ✅ Returns to dashboard

3. **View Mood History**
   - Navigate to "Mood Trends" or "History" tab
   - ✅ Table shows one entry:
     - Date: Today
     - Score: 4 (Above Average)
     - Tags: testing
   - ✅ Graph shows single data point

4. **Complete PHQ-9 Assessment**
   - Click "Take Assessment"
   - Select "PHQ-9" from list
   - Answer all 9 questions (use 0-1 for testing, low severity)
   - Click Submit
   - ✅ Score calculated and displayed (0-9 = Minimal to Mild depression)
   - ✅ Assessment saved

5. **View Assessment History**
   - Navigate to "Assessments" tab
   - ✅ Table shows one PHQ-9 entry with score and date
   - ✅ Click entry to see detailed responses

6. **Export Data**
   - Navigate to "Export" tab
   - Select format: CSV
   - Click Export
   - ✅ File save dialog opens
   - Save file
   - ✅ Open CSV file - verify mood entry and assessment data present

---

## Troubleshooting

### Database Connection Failed
```bash
# Check PostgreSQL status
docker-compose ps

# View logs
docker-compose logs postgres

# Restart if needed
docker-compose restart postgres
```

### API Won't Start
```bash
# Check port 5001 not in use
lsof -i :5001  # macOS/Linux
netstat -ano | findstr :5001  # Windows

# Check appsettings.json connection string
cat backend/src/MentalHealthBar.Api/appsettings.json
```

### Desktop App Can't Connect to API
1. Verify API is running (`dotnet run` in backend terminal)
2. Check API URL in desktop app settings (should be `https://localhost:5001`)
3. Check firewall allows localhost connections

### Migrations Failed
```bash
# Reset database (WARNING: Deletes all data)
docker-compose down -v
docker-compose up -d

# Re-run migrations
cd backend/src/MentalHealthBar.Api
dotnet ef database update
```

---

## Development Workflow

### Making Changes

1. **Backend Changes** (API endpoints, domain logic)
   ```bash
   cd backend/src/MentalHealthBar.Api
   dotnet watch run  # Auto-reload on file changes
   ```

2. **Frontend Changes** (UI, ViewModels)
   ```bash
   cd frontend/src/MentalHealthBar.Desktop
   dotnet watch run  # Auto-reload on file changes
   ```

3. **Database Schema Changes**
   ```bash
   cd backend/src/MentalHealthBar.Api
   dotnet ef migrations add <MigrationName>
   dotnet ef database update
   ```

### Running Tests

```bash
# Backend tests
cd backend/tests/MentalHealthBar.Api.Tests
dotnet test

# Frontend tests
cd frontend/tests/MentalHealthBar.Desktop.Tests
dotnet test
```

---

## Configuration

### Database Connection (Backend)

Edit `backend/src/MentalHealthBar.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mentalhealthbar;Username=postgres;Password=postgres"
  }
}
```

### API Endpoint (Desktop App)

Edit `frontend/src/MentalHealthBar.Desktop/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:5001"
}
```

---

## Sample Data (Optional)

For testing with realistic data:

```bash
# Run seed script (creates 30 days of sample data)
cd backend/src/MentalHealthBar.Api
dotnet run --seed-sample-data

# Sample data includes:
# - 30 mood entries (random scores and tags)
# - 4 assessments (one of each type)
# - Daily health metrics (sleep and water)
# - 10 event labels
```

---

## Success Criteria

You've successfully set up Mental Health Bar if:

1. ✅ Backend API responds at https://localhost:5001/swagger
2. ✅ Desktop app launches and shows dashboard
3. ✅ Can create a mood entry and see it in history table
4. ✅ Can complete PHQ-9 assessment and see calculated score
5. ✅ Can export data to CSV and verify contents
6. ✅ **Total time from clone to working app: < 5 minutes**

---

## Next Steps

- Review [architecture documentation](./plan.md) to understand vertical slice structure
- Read [data model](./data-model.md) for entity relationships
- Explore [API contracts](./contracts/) for endpoint specifications
- Check [tasks.md](./tasks.md) for implementation task breakdown

---

## Support

**Found an issue?**
- Check logs: `docker-compose logs` (database), console output (API/Desktop)
- Review [troubleshooting](#troubleshooting) section above
- Consult [constitution.md](../../.specify/memory/constitution.md) for development principles

**Need help?**
- Open issue on GitHub repository
- Review test files for usage examples
