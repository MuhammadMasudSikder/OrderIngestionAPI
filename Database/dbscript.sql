-- 1. Create the database if it doesn't exist
IF DB_ID('OrderIngestDb') IS NULL
BEGIN
    CREATE DATABASE OrderIngestDb;
END
GO

USE OrderIngestDb;
GO

-- 2. Create Orders table
IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ExternalOrderId NVARCHAR(100) NOT NULL,
        Source NVARCHAR(50) NOT NULL,
        CustomerEmail NVARCHAR(200),
        Amount DECIMAL(18,2),
        CorrelationId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );

    CREATE UNIQUE INDEX IX_Orders_ExternalOrderId_Source
        ON dbo.Orders (ExternalOrderId, Source);
END
GO

-- 3. Create OrderRaws table
IF OBJECT_ID('dbo.OrderRaws', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderRaws (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CorrelationId UNIQUEIDENTIFIER NOT NULL,
        ExternalOrderId NVARCHAR(100),
        Source NVARCHAR(50),
        Payload NVARCHAR(MAX),
        IngestedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- 4. Stored Procedure: Check if order exists
IF OBJECT_ID('dbo.sp_OrderExistsByExternalId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_OrderExistsByExternalId;
GO

CREATE PROCEDURE dbo.sp_OrderExistsByExternalId
    @ExternalOrderId NVARCHAR(100),
    @Source NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1)
    FROM dbo.Orders
    WHERE ExternalOrderId = @ExternalOrderId
      AND Source = @Source;
END
GO

-- 5. Stored Procedure: Add new order
IF OBJECT_ID('dbo.sp_AddOrder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AddOrder;
GO

CREATE PROCEDURE dbo.sp_AddOrder
    @ExternalOrderId NVARCHAR(100),
    @Source NVARCHAR(50),
    @CustomerEmail NVARCHAR(200),
    @Amount DECIMAL(18,2),
    @CorrelationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Orders (ExternalOrderId, Source, CustomerEmail, Amount, CorrelationId)
    VALUES (@ExternalOrderId, @Source, @CustomerEmail, @Amount, @CorrelationId);

    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- 6. Stored Procedure: Save raw payload
IF OBJECT_ID('dbo.sp_SaveOrderRaw', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SaveOrderRaw;
GO

CREATE PROCEDURE dbo.sp_SaveOrderRaw
    @CorrelationId UNIQUEIDENTIFIER,
    @ExternalOrderId NVARCHAR(100),
    @Source NVARCHAR(50),
    @Payload NVARCHAR(MAX),
    @IngestedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OrderRaws (CorrelationId, ExternalOrderId, Source, Payload, IngestedAt)
    VALUES (@CorrelationId, @ExternalOrderId, @Source, @Payload, @IngestedAt);
END
GO

-- 7. Stored Procedure: Save failed payload
IF OBJECT_ID('dbo.sp_SaveOrderRawFailed', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SaveOrderRawFailed;
GO

CREATE PROCEDURE dbo.sp_SaveOrderRawFailed
    @CorrelationId UNIQUEIDENTIFIER,
    @ExternalOrderId NVARCHAR(100),
    @Source NVARCHAR(50),
    @Payload NVARCHAR(MAX),
    @IngestedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OrderRaws (CorrelationId, ExternalOrderId, Source, Payload, IngestedAt)
    VALUES (@CorrelationId, @ExternalOrderId, @Source, @Payload, @IngestedAt);
END
GO
