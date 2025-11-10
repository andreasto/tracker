#!/bin/bash

# Tracker Frontend - Quick Start Script

echo "🚀 Starting Tracker Frontend Setup..."
echo ""

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
    
    if [ $? -ne 0 ]; then
        echo "❌ Failed to install dependencies"
        exit 1
    fi
    
    echo "✅ Dependencies installed successfully"
else
    echo "✅ Dependencies already installed"
fi

echo ""
echo "🔧 Configuration:"
echo "   - API URL: ${VITE_API_URL:-http://localhost:5000}"
echo "   - Frontend URL: http://localhost:3000"
echo ""

echo "📋 Prerequisites:"
echo "   1. Ensure the backend API is running on port 5000"
echo "   2. PostgreSQL should be running (if using persistence)"
echo ""

echo "🎯 Starting development server..."
npm run dev

