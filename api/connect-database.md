# How to Connect to Database on IIS Server

## Connection Information

Based on your configuration, your database connection details are:

- **Server**: `SERVERNAME\SQLEXPRESS` (replace SERVERNAME with your IIS server name)
- **Database**: `StrongTowingDB`
- **Authentication**: Windows Authentication (Trusted Connection)

## Method 1: SQL Server Management Studio (SSMS)

1. **Download and Install SSMS**
   - Download from: https://aka.ms/ssmsfullsetup
   - Install on your local machine or on the IIS server

2. **Connect to Database**
   - Open SSMS
   - Server name: `YOUR_SERVER_NAME\SQLEXPRESS` or `YOUR_SERVER_IP\SQLEXPRESS`
   - Authentication: Windows Authentication (if on same domain) or SQL Server Authentication
   - Click "Connect"

3. **Browse Database**
   - Expand "Databases" → `StrongTowingDB`
   - View tables, data, roles, etc.

## Method 2: Azure Data Studio

1. **Download**: https://aka.ms/azuredatastudio
2. **Connect**: Same connection details as SSMS
3. **View Data**: Modern interface with query editor

## Method 3: Remote Desktop to IIS Server

If you have RDP access to the IIS server:

1. **Remote Desktop** to the IIS server
2. **Open SSMS** directly on the server
3. **Connect** using: `localhost\SQLEXPRESS` or `.\SQLEXPRESS`
4. **Database**: `StrongTowingDB`

## Method 4: Command Line (sqlcmd)

If you have command-line access to the server:

```bash
sqlcmd -S SERVERNAME\SQLEXPRESS -d StrongTowingDB -E
```

Then run SQL queries:
```sql
USE StrongTowingDB;
GO
SELECT * FROM AspNetRoles;
GO
SELECT * FROM AspNetUsers;
GO
```

## Method 5: Check Connection String

Your current connection string in `appsettings.Production.json`:
```
Server=localhost\SQLEXPRESS;Database=StrongTowingDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

For remote connection, update to:
```
Server=YOUR_SERVER_NAME\SQLEXPRESS;Database=StrongTowingDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Or with SQL Authentication:
```
Server=YOUR_SERVER_NAME\SQLEXPRESS;Database=StrongTowingDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true
```

## Viewing Roles

To verify your role IDs are set correctly:

```sql
USE StrongTowingDB;
GO

-- View all roles with their IDs
SELECT Id, Name, NormalizedName 
FROM AspNetRoles
ORDER BY Id;

-- Expected results:
-- Id  | Name          | NormalizedName
-- ----|---------------|---------------
-- 69  | SuperAdmin    | SUPERADMIN
-- 1   | Administrator | ADMINISTRATOR
-- 2   | Dispatcher    | DISPATCHER
-- 3   | Driver        | DRIVER
-- 4   | User          | USER
```

## Troubleshooting

### Cannot connect to server
- Check if SQL Server is running on IIS server
- Verify firewall allows SQL Server port (1433 for default instance)
- Check if SQL Server Browser service is running (for named instances)

### Access denied
- Ensure your Windows account has SQL Server permissions
- Or use SQL Server Authentication with sa account

### Find SQL Server instance name
On the IIS server, run:
```powershell
Get-Service | Where-Object {$_.Name -like "*SQL*"}
```

Or check SQL Server Configuration Manager for instance names.

