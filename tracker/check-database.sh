#!/bin/bash

# Script to check PostgreSQL database for Akka persistence data

echo "Checking PostgreSQL database..."
echo "================================"
echo ""

# Connection details
HOST="localhost"
PORT="5432"
DATABASE="tracker"
USER="postgres"
export PGPASSWORD="postgres"

# Check if PostgreSQL is running
echo "1. Checking if PostgreSQL is running..."
if pg_isready -h $HOST -p $PORT -U $USER 2>/dev/null; then
    echo "✓ PostgreSQL is running"
else
    echo "✗ PostgreSQL is NOT running"
    echo ""
    echo "To start PostgreSQL:"
    echo "  - Homebrew: brew services start postgresql@15"
    echo "  - Docker: docker run --name tracker-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15"
    exit 1
fi
echo ""

# Check if database exists
echo "2. Checking if database '$DATABASE' exists..."
if psql -h $HOST -p $PORT -U $USER -lqt | cut -d \| -f 1 | grep -qw $DATABASE; then
    echo "✓ Database '$DATABASE' exists"
else
    echo "✗ Database '$DATABASE' does NOT exist"
    echo ""
    echo "To create database:"
    echo "  psql -h $HOST -p $PORT -U $USER -c 'CREATE DATABASE $DATABASE;'"
    exit 1
fi
echo ""

# Check tables
echo "3. Checking database tables..."
TABLES=$(psql -h $HOST -p $PORT -U $USER -d $DATABASE -t -c "SELECT tablename FROM pg_tables WHERE schemaname='public';")
if [ -z "$TABLES" ]; then
    echo "✗ No tables found in database"
    echo ""
    echo "Tables should be created automatically when the application starts."
    echo "Please run the application first: cd src/tracker.App && dotnet run"
else
    echo "✓ Tables found:"
    echo "$TABLES" | sed 's/^/  - /'
fi
echo ""

# Check journal records
echo "4. Checking journal table..."
if echo "$TABLES" | grep -q "journal"; then
    JOURNAL_COUNT=$(psql -h $HOST -p $PORT -U $USER -d $DATABASE -t -c "SELECT COUNT(*) FROM journal;")
    echo "  Total journal entries: $JOURNAL_COUNT"
    
    if [ "$JOURNAL_COUNT" -gt 0 ]; then
        echo ""
        echo "  Recent journal entries:"
        psql -h $HOST -p $PORT -U $USER -d $DATABASE -c "
            SELECT 
                persistence_id, 
                sequence_number, 
                manifest,
                to_timestamp(timestamp/1000) as timestamp,
                tags
            FROM journal 
            ORDER BY ordering DESC 
            LIMIT 5;
        "
    else
        echo "  ⚠ No journal entries found"
        echo ""
        echo "  This could mean:"
        echo "    - The application hasn't been started yet"
        echo "    - No persistence events have been triggered"
        echo "    - The application is using a different persistence mode"
    fi
else
    echo "✗ Journal table does not exist"
fi
echo ""

# Check snapshot table
echo "5. Checking snapshot table..."
if echo "$TABLES" | grep -q "snapshot"; then
    SNAPSHOT_COUNT=$(psql -h $HOST -p $PORT -U $USER -d $DATABASE -t -c "SELECT COUNT(*) FROM snapshot;")
    echo "  Total snapshots: $SNAPSHOT_COUNT"
    
    if [ "$SNAPSHOT_COUNT" -gt 0 ]; then
        echo ""
        echo "  Recent snapshots:"
        psql -h $HOST -p $PORT -U $USER -d $DATABASE -c "
            SELECT 
                persistence_id, 
                sequence_number,
                to_timestamp(created/1000) as created,
                manifest
            FROM snapshot 
            ORDER BY created DESC 
            LIMIT 5;
        "
    fi
else
    echo "✗ Snapshot table does not exist"
fi
echo ""

echo "================================"
echo "Check complete!"

