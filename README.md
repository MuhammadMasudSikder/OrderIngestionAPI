# OrderIngestionAPI

Comprehensive guide for setting up, running, and deploying the **OrderIngestionAPI** project.

---

## 1. Project Overview

The **OrderIngestionAPI** is a .NET Core API designed to ingest order data, store it in SQL Server, and expose endpoints for processing.

This README includes:

* SQL table creation script
* Stored procedure for order insertion
* Project setup instructions
* Running the API locally (IIS Express, Kestrel, Docker Desktop)
* Postman testing instructions
* Environment configuration details

---

## 2. Prerequisites

Ensure the following are installed:

### **Backend Requirements**

* .NET SDK 8.0
* SQL Server 2016+
* Visual Studio 2022

### **for Docker**
I have used docker by default.
* Docker Desktop installed and running

---

## 3. Database Setup

Below are the required SQL scripts.

### 3.1 Create Table

```sql
-- =============================================
-- Order Ingestion API - Database Schema
-- =============================================

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'OrderIngestionDB')
BEGIN
    CREATE DATABASE OrderIngestionDB;
END
GO

USE OrderIngestionDB;
GO

-- =============================================
-- Table: Customers
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Customers] (
        [CustomerId] INT IDENTITY(1,1) PRIMARY KEY,
        [Email] NVARCHAR(255) NOT NULL UNIQUE,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [Phone] NVARCHAR(20) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        INDEX IX_Customers_Email NONCLUSTERED (Email)
    );
END
GO

-- =============================================
-- Table: Orders
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [OrderId] INT IDENTITY(1,1) PRIMARY KEY,
        [RequestId] NVARCHAR(100) NOT NULL UNIQUE,
        [CustomerId] INT NOT NULL,
        [OrderDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [TotalAmount] DECIMAL(18,2) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [Platform] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
        INDEX IX_Orders_RequestId NONCLUSTERED (RequestId),
        INDEX IX_Orders_CustomerId NONCLUSTERED (CustomerId),
        INDEX IX_Orders_OrderDate NONCLUSTERED (OrderDate)
    );
END
GO

-- =============================================
-- Table: OrderItems
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OrderItems] (
        [OrderItemId] INT IDENTITY(1,1) PRIMARY KEY,
        [OrderId] INT NOT NULL,
        [ProductSku] NVARCHAR(100) NOT NULL,
        [ProductName] NVARCHAR(255) NOT NULL,
        [Quantity] INT NOT NULL,
        [UnitPrice] DECIMAL(18,2) NOT NULL,
        [TotalPrice] DECIMAL(18,2) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
        INDEX IX_OrderItems_OrderId NONCLUSTERED (OrderId),
        INDEX IX_OrderItems_ProductSku NONCLUSTERED (ProductSku)
    );
END
GO

-- =============================================
-- Table: IdempotencyLog
-- For tracking request IDs to prevent duplicates
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IdempotencyLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[IdempotencyLog] (
        [RequestId] NVARCHAR(100) PRIMARY KEY,
        [OrderId] INT NOT NULL,
        [ProcessedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt] DATETIME2 NOT NULL,
        INDEX IX_IdempotencyLog_ExpiresAt NONCLUSTERED (ExpiresAt)
    );
END
GO
```

### 3.2 Stored Procedure for Insertion

```sql
-- =============================================
-- Stored Procedure: CheckRequestIdempotency
-- Check if a request has already been processed
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CheckRequestIdempotency]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[CheckRequestIdempotency];
GO

CREATE PROCEDURE [dbo].[CheckRequestIdempotency]
    @RequestId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Clean up expired entries first
    DELETE FROM IdempotencyLog
    WHERE ExpiresAt < GETUTCDATE();

    -- Check if request exists
    SELECT
        OrderId,
        ProcessedAt
    FROM IdempotencyLog
    WHERE RequestId = @RequestId;
END
GO

-- =============================================
-- Stored Procedure: InsertOrder
-- Optimized procedure to insert order with items
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InsertOrder]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[InsertOrder];
GO

CREATE PROCEDURE [dbo].[InsertOrder]
    @RequestId NVARCHAR(100),
    @CustomerEmail NVARCHAR(255),
    @CustomerFirstName NVARCHAR(100),
    @CustomerLastName NVARCHAR(100),
    @CustomerPhone NVARCHAR(20),
    @Platform NVARCHAR(100),
    @TotalAmount DECIMAL(18,2),
    @OrderItems NVARCHAR(MAX) -- JSON array of order items
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CustomerId INT;
    DECLARE @OrderId INT;
    DECLARE @ExistingOrderId INT;

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Check idempotency first
        SELECT @ExistingOrderId = OrderId
        FROM IdempotencyLog
        WHERE RequestId = @RequestId AND ExpiresAt > GETUTCDATE();

        IF @ExistingOrderId IS NOT NULL
        BEGIN
            -- Request already processed, return existing order
            SELECT
                o.OrderId,
                o.RequestId,
                o.CustomerId,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.Platform,
                c.Email,
                c.FirstName,
                c.LastName,
                c.Phone
            FROM Orders o
            INNER JOIN Customers c ON o.CustomerId = c.CustomerId
            WHERE o.OrderId = @ExistingOrderId;

            COMMIT TRANSACTION;
            RETURN;
        END

        -- Upsert Customer
        SELECT @CustomerId = CustomerId
        FROM Customers WITH (UPDLOCK, HOLDLOCK)
        WHERE Email = @CustomerEmail;

        IF @CustomerId IS NULL
        BEGIN
            INSERT INTO Customers (Email, FirstName, LastName, Phone)
            VALUES (@CustomerEmail, @CustomerFirstName, @CustomerLastName, @CustomerPhone);

            SET @CustomerId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE Customers
            SET FirstName = @CustomerFirstName,
                LastName = @CustomerLastName,
                Phone = @CustomerPhone,
                UpdatedAt = GETUTCDATE()
            WHERE CustomerId = @CustomerId;
        END

        -- Insert Order
        INSERT INTO Orders (RequestId, CustomerId, TotalAmount, Platform, Status)
        VALUES (@RequestId, @CustomerId, @TotalAmount, @Platform, 'Pending');

        SET @OrderId = SCOPE_IDENTITY();

        -- Insert Order Items from JSON
        INSERT INTO OrderItems (OrderId, ProductSku, ProductName, Quantity, UnitPrice, TotalPrice)
        SELECT
            @OrderId,
            JSON_VALUE(value, '$.ProductSku'),
            JSON_VALUE(value, '$.ProductName'),
            CAST(JSON_VALUE(value, '$.Quantity') AS INT),
            CAST(JSON_VALUE(value, '$.UnitPrice') AS DECIMAL(18,2)),
            CAST(JSON_VALUE(value, '$.TotalPrice') AS DECIMAL(18,2))
        FROM OPENJSON(@OrderItems);

        -- Record in idempotency log (expires in 24 hours)
        INSERT INTO IdempotencyLog (RequestId, OrderId, ExpiresAt)
        VALUES (@RequestId, @OrderId, DATEADD(HOUR, 24, GETUTCDATE()));

        -- Return the created order
        SELECT
            o.OrderId,
            o.RequestId,
            o.CustomerId,
            o.OrderDate,
            o.TotalAmount,
            o.Status,
            o.Platform,
            c.Email,
            c.FirstName,
            c.LastName,
            c.Phone
        FROM Orders o
        INNER JOIN Customers c ON o.CustomerId = c.CustomerId
        WHERE o.OrderId = @OrderId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO

-- =============================================
-- Stored Procedure: GetOrderById
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetOrderById]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[GetOrderById];
GO

CREATE PROCEDURE [dbo].[GetOrderById]
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Get Order with Customer
    SELECT
        o.OrderId,
        o.RequestId,
        o.CustomerId,
        o.OrderDate,
        o.TotalAmount,
        o.Status,
        o.Platform,
        c.Email,
        c.FirstName,
        c.LastName,
        c.Phone
    FROM Orders o
    INNER JOIN Customers c ON o.CustomerId = c.CustomerId
    WHERE o.OrderId = @OrderId;

    -- Get Order Items
    SELECT
        OrderItemId,
        OrderId,
        ProductSku,
        ProductName,
        Quantity,
        UnitPrice,
        TotalPrice
    FROM OrderItems
    WHERE OrderId = @OrderId;
END
GO

PRINT 'Database schema created successfully!';
```

---

## 4. API Configuration

Update the **Connection String** inside `appsettings.json`:
for the docker container
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=host.docker.internal,1433;Database=OrderIngestionDB;User Id=sa;Password=orion123@;TrustServerCertificate=True;"
}
//for local box
"ConnectionStrings": {
  "DefaultConnection": "Server=(local);Database=OrderIngestionDB;User Id=sa;Password=orion123@;TrustServerCertificate=True;"
}
```

---

## 5. Running the Application

### **Option 1: Run in Visual Studio**

1. Open solution
2. Restore NuGet packages
3. Set `OrderIngestionAPI` as Startup Project
4. Run project (IIS Express or Kestrel)

API will run on:

```
https://localhost:7001
http://localhost:5001
```

---

### **Option 2: Run via Command Line**

```
dotnet restore
dotnet build
dotnet run --project OrderIngestionAPI
```

---

### **Option 3: Run via Docker Desktop**
Note: Visual Studio container tools require Docker to be running.
A `Dockerfile` it has exist in the project. Then run:

```
docker build -t orderingestionapi .
docker run -p 8080:8080 orderingestionapi
```

Application will be available at:

```
http://localhost:8080
```

---

## 6. API Endpoints

### **POST /api/orders/ingest**

Sample JSON request:

```json
{
  "requestId": "REQ-10023",
  "customer": {
    "email": "muhammadmasudsikder@gmail.com",
    "firstName": "Masud",
    "lastName": "Sikder",
    "phone": "+8801731808906"
  },
  "platform": "Shopify",
  "items": [
    {
      "productSku": "TSHIRT-BLK-L",
      "productName": "Black T-Shirt (Large)",
      "quantity": 2,
      "unitPrice": 1500.00
    }
  ]
}
```

Successful Response:

```json
{
  "orderId": 12,
  "isSuccess": true,
  "requestId": "REQ-10023",
  "status": "Pending",
  "totalAmount": 3000,
  "orderDate": "2025-12-11T16:57:13.5033333",
  "message": "Order created successfully",
  "processedAt": "0001-01-01T00:00:00"
}
```

---

## 7. Testing Using Postman

1. Import the API URL
2. Choose POST method
3. Set request body to **raw JSON**
4. Hit **Send**

---

## 8. Project Structure

```
OrderIngestionAPI/
 ├── Domain/
 ├── Application/
 ├── Infrastructure/
 ├── OrderIngestionAPI/
 ├── appsettings.json
 ├── Dockerfile
 └── README.md
```

---

## 9. How to Deploy (Azure / Windows Server)

1. Publish from Visual Studio (Folder or Azure Publish Profile)
2. Configure SQL connection in environment variables
3. Host on:

   * Azure App Service (recommended)
   * Windows IIS
   * Docker Container in Azure Container Apps

---

## 10. Author

**Masud Sikder**

---

This file contains everything required to set up, run, and validate the OrderIngestionAPI system.
