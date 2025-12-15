using Application.DTOs;
using Dapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnection _db;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(IDbConnection db, ILogger<OrderRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Order?> CheckIdempotencyAsync(string requestId, CancellationToken ct = default)
    {
        try
        {
            var result = await _db.QueryAsync<dynamic>(
                "CheckRequestIdempotency",
                new { RequestId = requestId },
                commandType: CommandType.StoredProcedure
            );

            var idempotencyRecord = result.FirstOrDefault();

            if (idempotencyRecord != null)
            {
                _logger.LogInformation("Duplicate request detected: {RequestId}", requestId);

                return await GetOrderByIdAsync((int)idempotencyRecord.OrderId);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idempotency for RequestId: {RequestId}", requestId);
            throw;
        }
    }

    public async Task<Order> CreateOrderAsync(Order request, CancellationToken ct = default)
    {
        try
        {
            // Calculate total amount
            var totalAmount = request.Items.Sum(item => item.Quantity * item.UnitPrice);

            // Prepare items for JSON
            var itemsWithTotal = request.Items.Select(item => new
            {
                item.ProductSku,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            }).ToList();

            var itemsJson = JsonSerializer.Serialize(itemsWithTotal);

            var parameters = new
            {
                RequestId = request.RequestId,
                CustomerEmail = request.Customer.Email,
                CustomerFirstName = request.Customer.FirstName,
                CustomerLastName = request.Customer.LastName,
                CustomerPhone = request.Customer.Phone,
                Platform = request.Platform ?? "Unknown",
                TotalAmount = totalAmount,
                OrderItems = itemsJson
            };

            var orderResult = await _db.QueryAsync<OrderDto>(
                "InsertOrder",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var orderData = orderResult.FirstOrDefault();
            if (orderData == null)
            {
                throw new InvalidOperationException("Failed to create order");
            }

            _logger.LogInformation("Order created successfully. OrderId: {OrderId}", (int)orderData.OrderId);

            //// Fetch complete order with items
            //var order = await GetOrderByIdAsync((int)orderData.OrderId);
            //return order ?? throw new InvalidOperationException("Failed to retrieve created order");
            var order=new Order
            (
                (int)orderData.OrderId,
                orderData.RequestId,
                new Customer
                (
                    orderData.Email,
                    orderData.FirstName,
                    orderData.LastName,
                    orderData.Phone
                ),
                request.Items,
                orderData.Platform
            );
            return order ?? throw new InvalidOperationException("Failed to retrieve created order");
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // Unique constraint violation
        {
            _logger.LogWarning("Duplicate order attempt detected: {RequestId}", request.RequestId);

            //// This is a race condition - another request created the order
            //var existingOrder = await CheckIdempotencyAsync(request.RequestId);
            //if (existingOrder != null)
            //{
            //    return existingOrder;
            //}

            //throw;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for RequestId: {RequestId}", request.RequestId);
            throw;
        }
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        try
        {
            using var multi = await _db.QueryMultipleAsync(
                "GetOrderById",
                new { OrderId = orderId },
                commandType: CommandType.StoredProcedure
            );

            var orderData = await multi.ReadFirstOrDefaultAsync<dynamic>();
            if (orderData == null)
            {
                return null;
            }

            var items = (await multi.ReadAsync<OrderItem>()).ToList();

            var order = new Order
            (
                (int)orderData.OrderId,
                orderData.RequestId,              
                new Customer
                (
                    orderData.Email,
                    orderData.FirstName,
                    orderData.LastName,
                    orderData.Phone
                ),
                items,
                orderData.Platform
            );

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task SaveRawPayloadAsync(Order raw, CancellationToken ct = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@RequestId", raw.RequestId);
            parameters.Add("@Payload", raw.ToString());

            await _db.ExecuteAsync(
                "sp_SaveOrderRaw",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Save Raw Payload: {RequestId}", raw.RequestId);
            throw;
        }
    }
}
