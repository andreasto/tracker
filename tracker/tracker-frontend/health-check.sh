#!/bin/bash

echo "🔍 Frontend Project Health Check"
echo "=================================="
echo ""

# Check Node.js
echo "📦 Checking Node.js..."
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    echo "✅ Node.js installed: $NODE_VERSION"
else
    echo "❌ Node.js not found! Please install Node.js v18+"
    exit 1
fi

# Check npm
echo ""
echo "📦 Checking npm..."
if command -v npm &> /dev/null; then
    NPM_VERSION=$(npm --version)
    echo "✅ npm installed: $NPM_VERSION"
else
    echo "❌ npm not found!"
    exit 1
fi

# Check if node_modules exists
echo ""
echo "📦 Checking dependencies..."
if [ -d "node_modules" ]; then
    echo "✅ node_modules exists"
else
    echo "⚠️  node_modules not found - run 'npm install'"
fi

# Check critical files
echo ""
echo "📄 Checking critical files..."

FILES=(
    "package.json"
    "vite.config.ts"
    "tsconfig.json"
    "tailwind.config.js"
    "src/App.tsx"
    "src/main.tsx"
    "src/index.css"
    "src/vite-env.d.ts"
    "src/services/api.ts"
    "src/components/RegistrationFlow.tsx"
    "src/components/UserRegistration.tsx"
    "src/components/Questionnaire.tsx"
    "src/components/StartValues.tsx"
    "src/components/CompletionScreen.tsx"
    "src/components/Dashboard.tsx"
)

MISSING_FILES=0
for file in "${FILES[@]}"; do
    if [ -f "$file" ]; then
        # Check if file is not empty
        if [ -s "$file" ]; then
            echo "✅ $file"
        else
            echo "⚠️  $file (empty)"
            MISSING_FILES=$((MISSING_FILES + 1))
        fi
    else
        echo "❌ $file (missing)"
        MISSING_FILES=$((MISSING_FILES + 1))
    fi
done

echo ""
if [ $MISSING_FILES -eq 0 ]; then
    echo "✅ All files present and non-empty!"
else
    echo "⚠️  $MISSING_FILES file(s) missing or empty"
fi

# Check if backend is running
echo ""
echo "🔗 Checking backend API..."
if curl -s http://localhost:5000 > /dev/null 2>&1; then
    echo "✅ Backend API is running on port 5000"
else
    echo "⚠️  Backend API not detected on port 5000"
    echo "   Make sure to start it with: cd src/tracker.App && dotnet run"
fi

# Summary
echo ""
echo "=================================="
echo "📋 Summary"
echo "=================================="

if [ $MISSING_FILES -eq 0 ]; then
    echo "✅ Project structure: GOOD"
    echo "✅ All required files: PRESENT"
    echo ""
    echo "🚀 Ready to start! Run:"
    echo "   npm install  (if not done)"
    echo "   npm run dev"
else
    echo "⚠️  Some files are missing or empty"
    echo "   Please review the output above"
fi

echo ""

