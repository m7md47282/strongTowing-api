#!/bin/bash

echo "🛑 Stopping Docker SQL Server container..."

if docker ps | grep -q strongtowing-sqlserver; then
    docker stop strongtowing-sqlserver
    echo "✅ SQL Server container stopped"
else
    echo "ℹ️  SQL Server container is not running"
fi

echo ""
echo "To start it again, run: ./setup-docker-sql.sh"
echo "To remove it completely, run: docker-compose down -v"


