#!/bin/bash

# Script to test check-in API and verify persistence

BASE_URL="http://localhost:5000"
USER_ID="test-user-$(date +%s)"

echo "Testing Check-In API and Persistence"
echo "====================================="
echo "Using User ID: $USER_ID"
echo ""

# Test 1: Create a user first
echo "1. Creating user..."
RESPONSE=$(curl -s -X POST "$BASE_URL/user/$USER_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com"
  }')
echo "Response: $RESPONSE"
echo ""

# Test 2: Set check-in day
echo "2. Setting weekly check-in day..."
RESPONSE=$(curl -s -X PUT "$BASE_URL/checkin/$USER_ID/checkin-day" \
  -H "Content-Type: application/json" \
  -d '{
    "checkInDay": "Monday"
  }')
echo "Response: $RESPONSE"
echo ""

# Test 3: Submit a check-in
echo "3. Submitting weekly check-in..."
RESPONSE=$(curl -s -X POST "$BASE_URL/checkin/$USER_ID" \
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
  }')
echo "Response: $RESPONSE"
echo ""

# Test 4: Get check-in history
echo "4. Fetching check-in history..."
RESPONSE=$(curl -s -X GET "$BASE_URL/checkin/$USER_ID")
echo "Response: $RESPONSE"
echo ""

# Test 5: Check database
echo "5. Checking PostgreSQL database..."
export PGPASSWORD=postgres
if command -v psql &> /dev/null; then
    echo ""
    echo "Journal entries for this user:"
    psql -h localhost -p 5432 -U postgres -d tracker -c \
      "SELECT persistence_id, sequence_number, manifest, tags 
       FROM journal 
       WHERE persistence_id LIKE '%$USER_ID%' 
       ORDER BY sequence_number;"
    
    echo ""
    echo "All journal entries:"
    psql -h localhost -p 5432 -U postgres -d tracker -c \
      "SELECT persistence_id, sequence_number, manifest 
       FROM journal 
       ORDER BY ordering DESC 
       LIMIT 10;"
else
    echo "psql command not found - skipping database check"
    echo "Install PostgreSQL client tools or check database manually"
fi

echo ""
echo "====================================="
echo "Test complete!"
echo ""
echo "If you see journal entries above, persistence is working."
echo "If not, check:"
echo "  1. Is the application running? (dotnet run in src/tracker.App)"
echo "  2. Is PostgreSQL running?"
echo "  3. Does the 'tracker' database exist?"
echo "  4. Are there any errors in the application logs?"

