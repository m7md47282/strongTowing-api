# Docker SQL Server Setup Guide

This guide helps you set up a local SQL Server instance using Docker for testing migrations locally.

## Prerequisites

1. **Install Docker Desktop**
   - Download from: https://www.docker.com/products/docker-desktop
   - Install and start Docker Desktop
   - Make sure Docker is running (you should see the Docker icon in your menu bar)

## Quick Start

### 1. Start SQL Server Container

```bash
./setup-docker-sql.sh
```

Or manually:
```bash
docker-compose up -d
```

### 2. Wait for SQL Server to be Ready

The script will wait automatically, but if running manually, wait about 20-30 seconds for SQL Server to fully start.

### 3. Run Database Migration

```bash
dotnet ef database update --project StrongTowing.Infrastructure --startup-project StrongTowing.API
```

### 4. Verify It Works

Start your API:
```bash
dotnet run --project StrongTowing.API
```

## Connection Details

- **Server**: `localhost,1433`
- **Database**: `StrongTowingDB` (created automatically on first migration)
- **Username**: `sa`
- **Password**: `YourStrong@Passw0rd123`

## Useful Commands

### Start SQL Server
```bash
./setup-docker-sql.sh
# or
docker-compose up -d
```

### Stop SQL Server
```bash
./stop-docker-sql.sh
# or
docker-compose stop
```

### View SQL Server Logs
```bash
docker logs strongtowing-sqlserver
```

### Remove Everything (including data)
```bash
docker-compose down -v
```

### Check if SQL Server is Running
```bash
docker ps | grep strongtowing-sqlserver
```

## Troubleshooting

### Docker is not running
- Make sure Docker Desktop is started
- Check the Docker icon in your menu bar

### Port 1433 is already in use
- Stop any existing SQL Server instances
- Or change the port in `docker-compose.yml` (e.g., `"1434:1433"`)

### Connection timeout
- Wait a bit longer (SQL Server can take 30+ seconds to start)
- Check logs: `docker logs strongtowing-sqlserver`

### Migration fails
- Make sure SQL Server is fully started (wait 30 seconds)
- Check connection string in `appsettings.Development.json`
- Verify the container is running: `docker ps`

## Notes

- **Data Persistence**: Data is stored in a Docker volume, so it persists even if you stop the container
- **Production**: This is only for local development. Production uses the actual SQL Server on your IIS server
- **Password**: The password is set in `docker-compose.yml`. Change it if needed, but update `appsettings.Development.json` too

