#!/bin/bash

echo "🚀 Setting up Docker SQL Server for StrongTowing API..."
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed or not in PATH"
    echo "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop"
    exit 1
fi

# Check if Docker is running
if ! docker info &> /dev/null; then
    echo "❌ Docker is not running"
    echo "Please start Docker Desktop and try again"
    exit 1
fi

echo "✅ Docker is installed and running"
echo ""

# Check if container already exists
if docker ps -a | grep -q strongtowing-sqlserver; then
    echo "📦 SQL Server container already exists"
    read -p "Do you want to remove and recreate it? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "🗑️  Removing existing container..."
        docker stop strongtowing-sqlserver 2>/dev/null
        docker rm strongtowing-sqlserver 2>/dev/null
    else
        echo "🔄 Starting existing container..."
        docker start strongtowing-sqlserver
        echo "⏳ Waiting for SQL Server to be ready (15 seconds)..."
        sleep 15
        echo "✅ SQL Server is ready!"
        exit 0
    fi
fi

# Start SQL Server using docker-compose
echo "🐳 Starting SQL Server container..."
docker-compose up -d

if [ $? -ne 0 ]; then
    echo "❌ Failed to start SQL Server container"
    exit 1
fi

echo "⏳ Waiting for SQL Server to be ready (20 seconds)..."
sleep 20

# Test connection
echo "🔍 Testing SQL Server connection..."
if docker exec strongtowing-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd123 -Q "SELECT @@VERSION" &> /dev/null; then
    echo "✅ SQL Server is ready and accepting connections!"
    echo ""
    echo "📋 Connection Details:"
    echo "   Server: localhost,1433"
    echo "   Database: StrongTowingDB (will be created on first migration)"
    echo "   Username: sa"
    echo "   Password: YourStrong@Passw0rd123"
    echo ""
    echo "🚀 You can now run migrations with:"
    echo "   dotnet ef database update --project StrongTowing.Infrastructure --startup-project StrongTowing.API"
else
    echo "⚠️  SQL Server is starting but not ready yet. Please wait a few more seconds and try again."
fi

