# PostgreSQL Persistence - Quick Start

## The Problem You're Experiencing

You can't find journal records in PostgreSQL. This is likely because:

1. **No events have been triggered yet** - Journal records are only created when actors persist events
2. **Application hasn't been started** - Tables are created on first run
3. **PostgreSQL isn't running** - Database must be available
4. **Wrong persistence mode** - Config might be set to InMemory instead of PostgreSQL

## Quick Fix

### Step 1: Ensure PostgreSQL is Running

```bash
# Check if running
pg_isready -h localhost -p 5432 -U postgres

# If not, start it (macOS with Homebrew)
brew services start postgresql@15

# Or use Docker
docker run --name tracker-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
```

### Step 2: Create the Database (if needed)

```bash
PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE tracker;"
```

### Step 3: Start the Application

```bash
cd src/tracker.App
dotnet run
```

**Look for this in the logs:**
```
Initializing Akka.Persistence.Sql database tables...
Akka.Persistence.Sql database tables initialized successfully
```

### Step 4: Trigger Some Events

**Option A: Use the test script**
```bash
./test-checkin.sh
```

**Option B: Manual API calls**
```bash
# Create a user
curl -X POST http://localhost:5000/user/testuser \
  -H "Content-Type: application/json" \
  -d '{"name": "Test User", "email": "test@example.com"}'

# Submit a check-in
curl -X POST http://localhost:5000/checkin/testuser \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 75.5,
    "thigh": 60.0,
    "glutes": 95.0,
    "hips": 100.0,
    "waist": 80.0,
    "stomach": 85.0,
    "chest": 95.0,
    "overarm": 35.0
  }'
```

### Step 5: Verify Journal Records

```bash
PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres -d tracker \
  -c "SELECT persistence_id, sequence_number, manifest FROM journal ORDER BY ordering DESC LIMIT 5;"
```

You should see entries like:
- `CheckIn_testuser`
- `User_testuser`

## What Creates Journal Records?

Journal records are created when:
- ✅ **User is created** → `UserCreated` event
- ✅ **User is updated** → `UserNameUpdated`, `UserEmailUpdated` events
- ✅ **Check-in day is set** → `WeeklyCheckInDaySet` event
- ✅ **Check-in is submitted** → `WeeklyCheckInSubmitted` event
- ✅ **Counter is incremented** → `CounterIncremented` event

Journal records are **NOT** created when:
- ❌ Just querying/reading data (GET requests)
- ❌ Application is running but no API calls are made
- ❌ PersistenceMode is set to "InMemory"

## Diagnostic Tools

We've created helper scripts:

1. **check-database.sh** - Checks database status, tables, and records
   ```bash
   ./check-database.sh
   ```

2. **test-checkin.sh** - Makes API calls and verifies persistence
   ```bash
   ./test-checkin.sh
   ```

## Expected Database Schema

After first run, you should have 3 tables:

| Table | Purpose |
|-------|---------|
| `journal` | Stores all persisted events (event sourcing) |
| `snapshot` | Stores actor state snapshots (every 25 events) |
| `metadata` | Stores persistence metadata |

## Useful Database Queries

```sql
-- Count all journal entries
SELECT COUNT(*) FROM journal;

-- View recent events
SELECT persistence_id, sequence_number, manifest, 
       to_timestamp(timestamp/1000) as event_time
FROM journal 
ORDER BY ordering DESC LIMIT 10;

-- Group by actor type
SELECT 
    SPLIT_PART(persistence_id, '_', 1) as actor_type,
    COUNT(*) as event_count
FROM journal 
GROUP BY actor_type;

-- View all persistence IDs
SELECT DISTINCT persistence_id FROM journal ORDER BY persistence_id;
```

## Still No Records?

See **TROUBLESHOOTING_PERSISTENCE.md** for detailed diagnostic steps.

## Configuration Files

Your persistence config is in:
- `appsettings.json` → `"PersistenceMode": "PostgreSql"`
- `appsettings.Development.json` → `"PersistenceMode": "PostgreSql"`
- `ConnectionStrings:PostgreSql` → Database connection string

## Architecture

- **CheckInActor** → Persistence ID: `CheckIn_{userId}`
- **UserActor** → Persistence ID: `User_{userId}`
- **CounterActor** → Persistence ID: `Counter_{counterId}`

Each actor instance persists its own events to the journal table.

