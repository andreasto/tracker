#!/bin/bash

# Test script for Check-In feature with progress tracking
# This script creates a user, completes onboarding, and adds multiple check-ins to test the progress chart

BASE_URL="http://localhost:5000"
USER_ID="test-user-progress-$(date +%s)"

echo "==================================================================="
echo "Testing Progress Tracking Feature"
echo "User ID: ${USER_ID}"
echo "==================================================================="

# 1. Create user
echo ""
echo "1. Creating user..."
curl -X POST "${BASE_URL}/User/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Progress Test User",
    "email": "progress@example.com"
  }' | jq '.'

# 2. Answer questionnaire
echo ""
echo "2. Answering questionnaire..."
curl -X POST "${BASE_URL}/User/${USER_ID}/questionnaire" \
  -H "Content-Type: application/json" \
  -d '{
    "answers": {
      "gender": "male",
      "age": "30",
      "height": "180",
      "activityLevel": "moderate",
      "goal": "lose",
      "mealsPerDay": "4"
    }
  }' | jq '.'

# 3. Provide start values
echo ""
echo "3. Providing start values..."
curl -X POST "${BASE_URL}/User/${USER_ID}/start-values" \
  -H "Content-Type: application/json" \
  -d '{
    "startWeight": 85.0,
    "measurements": {
      "chest": 105.0,
      "waist": 90.0,
      "hips": 100.0
    }
  }' | jq '.'

# 4. Complete onboarding
echo ""
echo "4. Completing onboarding..."
curl -X POST "${BASE_URL}/User/${USER_ID}/complete-onboarding" | jq '.'

# 5. Set check-in day to Monday
echo ""
echo "5. Setting check-in day to Monday..."
curl -X PUT "${BASE_URL}/CheckIn/${USER_ID}/checkin-day" \
  -H "Content-Type: application/json" \
  -d '{
    "checkInDay": 1
  }' | jq '.'

# 6. Add multiple check-ins to show progress
echo ""
echo "6. Adding check-in #1 (Week 1)..."
curl -X POST "${BASE_URL}/CheckIn/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 84.2,
    "thigh": 58.0,
    "glutes": 100.0,
    "hips": 99.0,
    "waist": 89.0,
    "stomach": 88.0,
    "chest": 104.5,
    "overarm": 36.0
  }' | jq '.'

sleep 1

echo ""
echo "7. Adding check-in #2 (Week 2)..."
curl -X POST "${BASE_URL}/CheckIn/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 83.5,
    "thigh": 57.5,
    "glutes": 99.5,
    "hips": 98.5,
    "waist": 88.0,
    "stomach": 87.0,
    "chest": 104.0,
    "overarm": 35.5
  }' | jq '.'

sleep 1

echo ""
echo "8. Adding check-in #3 (Week 3)..."
curl -X POST "${BASE_URL}/CheckIn/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 82.8,
    "thigh": 57.0,
    "glutes": 99.0,
    "hips": 98.0,
    "waist": 87.0,
    "stomach": 86.0,
    "chest": 103.5,
    "overarm": 35.0
  }' | jq '.'

sleep 1

echo ""
echo "9. Adding check-in #4 (Week 4)..."
curl -X POST "${BASE_URL}/CheckIn/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 82.1,
    "thigh": 56.5,
    "glutes": 98.5,
    "hips": 97.5,
    "waist": 86.0,
    "stomach": 85.0,
    "chest": 103.0,
    "overarm": 34.5
  }' | jq '.'

sleep 1

echo ""
echo "10. Adding check-in #5 (Week 5)..."
curl -X POST "${BASE_URL}/CheckIn/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 81.5,
    "thigh": 56.0,
    "glutes": 98.0,
    "hips": 97.0,
    "waist": 85.0,
    "stomach": 84.0,
    "chest": 102.5,
    "overarm": 34.0
  }' | jq '.'

# 11. Get all check-ins
echo ""
echo "11. Getting all check-ins..."
curl -X GET "${BASE_URL}/CheckIn/${USER_ID}" | jq '.'

echo ""
echo "==================================================================="
echo "Progress Tracking Test Complete!"
echo ""
echo "User ID: ${USER_ID}"
echo ""
echo "You can now test the frontend by navigating to:"
echo "http://localhost:5173/dashboard/${USER_ID}"
echo ""
echo "Expected results:"
echo "- Weight decreased from 85.0 kg to 81.5 kg (-3.5 kg)"
echo "- Waist decreased from 90.0 cm to 85.0 cm (-5.0 cm)"
echo "- All measurements show downward trend"
echo "- Progress chart should display 5 data points"
echo "==================================================================="

