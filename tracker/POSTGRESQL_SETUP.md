# PostgreSQL Persistence Setup

## Overview
Your Akka.NET application is now configured to use PostgreSQL for persistence using `Akka.Persistence.Sql.Hosting`.

## Configuration

### Connection Strings
- **Development**: `appsettings.Development.json` - Database: `tracker_dev`
- **Production**: `appsettings.json` - Database: `tracker`

Default connection string format:
```
Host=localhost;Port=5432;Database=tracker;Username=postgres;Password=postgres
```

## Database Setup

### 1. Install PostgreSQL
If you don't have PostgreSQL installed:
```bash
# macOS (using Homebrew)
brew install postgresql@15
brew services start postgresql@15

# Or use Docker
docker run --name tracker-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
```

### 2. Create Database
```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE tracker;
CREATE DATABASE tracker_dev;

# Exit
\q
```

### 3. Initialize Database Tables
The Akka.Persistence.Sql plugin will automatically create the required tables on first run:
- `journal` - Event journal
- `snapshot` - Snapshots
- `metadata` - Metadata

## Running the Application

```bash
cd src/tracker.App
dotnet run
```

The application will automatically create the necessary database tables when it starts.

## Switching Between Persistence Modes

In `appsettings.json` or `appsettings.Development.json`, you can change the `PersistenceMode`:

```json
{
  "AkkaSettings": {
    "PersistenceMode": "PostgreSql"  // Options: "InMemory", "Azure", "PostgreSql"
  }
}
```

## Docker Compose Example

Add this to your `docker-compose.yaml`:

```yaml
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: tracker
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

## Packages Installed
- `Akka.Persistence.Sql.Hosting` (v1.5.53)
- `Akka.Persistence.Sql` (v1.5.53)
- Connection handled by LinqToDB with PostgreSQL 15 provider

## Configuration Details
- **Provider**: `LinqToDB.ProviderName.PostgreSQL15`
- **Database Mapping**: `DatabaseMapping.PostgreSql`
- **Tag Storage Mode**: `TagMode.Csv`
- **Delete Compatibility Mode**: `false`

