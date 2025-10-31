# Troubleshooting: No Journal Records in PostgreSQL

## Problem
You can't find any journal records in your PostgreSQL database even though the application is configured to use PostgreSQL persistence.

## Diagnostic Steps

### 1. Verify PostgreSQL is Running

```bash
# Check if PostgreSQL is running
pg_isready -h localhost -p 5432 -U postgres

# If not running, start it:
# macOS (Homebrew):
brew services start postgresql@15

# Or using Docker:
docker run --name tracker-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
```

### 2. Verify Database Exists

```bash
# Connect to PostgreSQL
PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres

# List databases
\l

# If 'tracker' database doesn't exist, create it:
CREATE DATABASE tracker;

# Connect to the tracker database
\c tracker

# List tables
\dt

# Exit
\q
```

### 3. Check Application Configuration

Verify your `appsettings.Development.json` or `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Port=5432;Database=tracker;Username=postgres;Password=postgres"
  },
  "AkkaSettings": {
    "PersistenceMode": "PostgreSql"  // Must be PostgreSql, not InMemory or Azure
  }
}
```

### 4. Run the Application

The database tables are created automatically when the application starts:

```bash
cd src/tracker.App
dotnet run
```

**Look for these log messages:**
- `Initializing Akka.Persistence.Sql database tables...`
- `Akka.Persistence.Sql database tables initialized successfully`

### 5. Trigger Some Events

The journal table will only have records if you've actually made API calls that trigger persistence events. Run the test script:

```bash
./test-checkin.sh
```

Or manually make API calls:

```bash
# Create a user
curl -X POST http://localhost:5000/user/user123 \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john@example.com"}'

# Submit a check-in
curl -X POST http://localhost:5000/checkin/user123 \
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

### 6. Query the Database

```bash
PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres -d tracker
```

```sql
-- Check if tables exist
\dt

-- Count journal entries
SELECT COUNT(*) FROM journal;

-- View recent journal entries
SELECT 
    persistence_id, 
    sequence_number, 
    manifest,
    to_timestamp(timestamp/1000) as event_time,
    tags
FROM journal 
ORDER BY ordering DESC 
LIMIT 10;

-- Check what persistence IDs exist
SELECT DISTINCT persistence_id FROM journal;

-- Count snapshots
SELECT COUNT(*) FROM snapshot;

-- View snapshots
SELECT persistence_id, sequence_number, to_timestamp(created/1000) as created
FROM snapshot
ORDER BY created DESC;
```

## Common Issues and Solutions

### Issue 1: Tables Don't Exist

**Cause:** Application hasn't been started yet, or database initialization failed.

**Solution:**
1. Run the application: `cd src/tracker.App && dotnet run`
2. Check application logs for errors
3. Verify connection string is correct
4. Ensure PostgreSQL user has CREATE TABLE permissions

### Issue 2: Tables Exist but No Journal Records

**Cause:** No persistence events have been triggered yet.

**Solution:**
1. Make API calls to create users and check-ins
2. Use the test script: `./test-checkin.sh`
3. Check application logs for errors
4. Verify `PersistenceMode` is set to `PostgreSql` in config

### Issue 3: Application is Using InMemory Instead

**Cause:** Wrong persistence mode in configuration.

**Solution:**
1. Check `appsettings.Development.json` for `"PersistenceMode": "PostgreSql"`
2. Check which environment the app is running in (`ASPNETCORE_ENVIRONMENT`)
3. Restart the application after changing config

### Issue 4: Connection Refused

**Cause:** PostgreSQL is not running or connection string is wrong.

**Solution:**
1. Start PostgreSQL: `brew services start postgresql@15`
2. Verify connection string matches your PostgreSQL setup
3. Check port (default 5432)
4. Check password

### Issue 5: Clustering Issues

**Cause:** With `UseClustering: true`, actors might not be started on the current node.

**Solution:**
1. For development, set `"UseClustering": false` in `appsettings.Development.json`
2. Or ensure cluster is properly formed
3. Check Akka.NET logs for sharding/clustering errors

## Expected Persistence IDs

When you create entities, you should see these persistence IDs in the journal:

- **User actors**: `User_<userId>` (e.g., `User_user123`)
- **CheckIn actors**: `CheckIn_<userId>` (e.g., `CheckIn_user123`)
- **Counter actors**: `Counter_<counterId>` (e.g., `Counter_counter1`)

## Quick Verification Query

Run this to see all persistence activity:

```sql
SELECT 
    LEFT(persistence_id, 20) as actor_type,
    COUNT(*) as event_count,
    MAX(sequence_number) as max_sequence,
    MAX(to_timestamp(timestamp/1000)) as last_event
FROM journal 
GROUP BY LEFT(persistence_id, 20)
ORDER BY last_event DESC;
```

## Test Scripts

We've created two helper scripts:

1. **check-database.sh** - Diagnoses database state
2. **test-checkin.sh** - Makes API calls and verifies persistence

Run them to quickly diagnose issues:

```bash
./check-database.sh
./test-checkin.sh
```

## Still Having Issues?

If journal records still don't appear:

1. **Enable debug logging** in `appsettings.Development.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Akka": "Debug"
       }
     }
   }
   ```

2. **Check application logs** when making API calls - you should see:
   - Actor creation messages
   - Persistence messages
   - Event storage confirmations

3. **Verify the actor is actually being created**:
   - Add logging in the actor constructors
   - Check that the sharding/routing is working

4. **Check for exceptions** in the application output

## Next Steps

Once you have journal records appearing:
- Events will persist across application restarts
- Snapshots will be created every 25 events
- You can query historical event data
- Event sourcing is fully operational

