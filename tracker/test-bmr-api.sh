#!/bin/bash

# Test script for BMR calculation in User onboarding flow

BASE_URL="http://localhost:5000"
USER_ID="test-bmr-user-$(date +%s)"

echo "Testing BMR Calculation Feature"
echo "================================"
echo "User ID: $USER_ID"
echo ""

# Step 1: Create User
echo "1. Creating user..."
curl -X POST "$BASE_URL/user/$USER_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com"
  }' | jq '.'

echo ""
echo ""

# Step 2: Answer Questionnaire with BMR-required fields
echo "2. Answering questionnaire with gender, age, height, activity level..."
curl -X POST "$BASE_URL/user/$USER_ID/questionnaire" \
  -H "Content-Type: application/json" \
  -d '{
    "answers": {
      "gender": "male",
      "age": "30",
      "height": "180",
      "activityLevel": "moderate",
      "goals": "weight-loss",
      "dietaryRestrictions": "none"
    }
  }' | jq '.'

echo ""
echo ""

# Step 3: Provide Start Values
echo "3. Providing start values (weight and measurements)..."
curl -X POST "$BASE_URL/user/$USER_ID/start-values" \
  -H "Content-Type: application/json" \
  -d '{
    "startWeight": 75.5,
    "measurements": {
      "chest": 95.0,
      "waist": 85.0,
      "hips": 100.0
    }
  }' | jq '.'

echo ""
echo ""

# Step 4: Complete Onboarding (BMR calculated automatically)
echo "4. Completing onboarding (BMR will be calculated automatically)..."
curl -X POST "$BASE_URL/user/$USER_ID/complete-onboarding" \
  -H "Content-Type: application/json" | jq '.'

echo ""
echo ""

# Step 5: Fetch User to see BMR values
echo "5. Fetching user to verify BMR values..."
curl -X GET "$BASE_URL/user/$USER_ID" | jq '.'

echo ""
echo ""
echo "Expected BMR values for male, 30 years, 180cm, 75.5kg, moderate activity:"
echo "  BMR Base: ~1741.26 kcal/day"
echo "  BMR with Activity: ~2698.96 kcal/day"

