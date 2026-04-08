/*
  Aligns production with EF migration: 20260404222303_AddDriverPayrollJobMinutesAndUniqueIndex

  Preferred: deploy the API and run from project directory:
    dotnet ef database update --project StrongTowing.Infrastructure --startup-project StrongTowing.API

  Use this script only when you must patch SQL Server manually (e.g. no EF CLI on the host).

  If CREATE UNIQUE INDEX fails, you may have duplicate (DriverId, PayPeriodStart, PayPeriodEnd)
  rows — resolve duplicates first, then rerun the index section.
*/

SET NOCOUNT ON;

-- 1) Add TotalJobMinutes (int, NOT NULL, default 0)
IF COL_LENGTH(N'dbo.DriverPayrolls', N'TotalJobMinutes') IS NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes i
        INNER JOIN sys.tables t ON i.object_id = t.object_id
        WHERE t.name = N'DriverPayrolls' AND i.name = N'IX_DriverPayrolls_DriverId'
    )
        DROP INDEX [IX_DriverPayrolls_DriverId] ON [dbo].[DriverPayrolls];

    ALTER TABLE [dbo].[DriverPayrolls]
        ADD [TotalJobMinutes] INT NOT NULL CONSTRAINT [DF_DriverPayrolls_TotalJobMinutes] DEFAULT ((0));
END
GO

-- 2) Unique index on (DriverId, PayPeriodStart, PayPeriodEnd)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'DriverPayrolls' AND i.name = N'IX_DriverPayrolls_DriverId_PayPeriodStart_PayPeriodEnd'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DriverPayrolls_DriverId_PayPeriodStart_PayPeriodEnd]
        ON [dbo].[DriverPayrolls] ([DriverId], [PayPeriodStart], [PayPeriodEnd]);
END
GO

-- 3) Record migration so EF does not try to re-apply (adjust ProductVersion to match your __EFMigrationsHistory rows)
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260404222303_AddDriverPayrollJobMinutesAndUniqueIndex'
)
BEGIN
    DECLARE @pv NVARCHAR(32) = N'9.0.0';
    SELECT TOP 1 @pv = [ProductVersion] FROM [__EFMigrationsHistory] ORDER BY [MigrationId] DESC;
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260404222303_AddDriverPayrollJobMinutesAndUniqueIndex', @pv);
END
GO
