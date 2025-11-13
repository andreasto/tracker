#!/bin/bash

# Test script for MealPlan API
# This script demonstrates the complete onboarding flow including mealsPerDay
# and then creates a meal plan based on the user's preferences

BASE_URL="http://localhost:5000"
USER_ID="test-user-123"

echo "==================================================================="
echo "Testing Complete Onboarding Flow with MealPlan Creation"
echo "==================================================================="

# First, ensure the user exists and has completed onboarding
echo ""
echo "1. Creating user..."
curl -X POST "${BASE_URL}/User/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com"
  }' | jq '.'

echo ""
echo "2. Answering questionnaire (including mealsPerDay)..."
curl -X POST "${BASE_URL}/User/${USER_ID}/questionnaire" \
  -H "Content-Type: application/json" \
  -d '{
    "answers": {
      "gender": "male",
      "age": "30",
      "height": "180",
      "activityLevel": "moderate",
      "goal": "maintain",
      "mealsPerDay": "4"
    }
  }' | jq '.'

echo ""
echo "3. Providing start values..."
curl -X POST "${BASE_URL}/User/${USER_ID}/start-values" \
  -H "Content-Type: application/json" \
  -d '{
    "startWeight": 80.0,
    "measurements": {
      "chest": 100.0,
      "waist": 85.0,
      "hips": 95.0
    }
  }' | jq '.'

echo ""
echo "4. Completing onboarding..."
curl -X POST "${BASE_URL}/User/${USER_ID}/complete-onboarding" | jq '.'

echo ""
echo "5. Creating meal plan..."
curl -X POST "${BASE_URL}/MealPlan/${USER_ID}" \
  -H "Content-Type: application/json" \
  -d '{
    "planName": "Weekly Meal Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z"
  }' | jq '.'

echo ""
echo "6. Getting user's meal plans..."
curl -X GET "${BASE_URL}/MealPlan/user/${USER_ID}" | jq '.'

echo ""
echo "==================================================================="
echo "MealPlan API Test Complete"
echo "==================================================================="

