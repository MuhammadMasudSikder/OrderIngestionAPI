using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{

    /// <summary>
    /// Mock implementation of a third-party logistics gateway
    /// Simulates a 2-second processing delay
    /// </summary>
    public class MockLogisticsGateway : ILogisticsGateway
    {
        private readonly ILogger<MockLogisticsGateway> _logger;

        public MockLogisticsGateway(ILogger<MockLogisticsGateway> logger)
        {
            _logger = logger;
        }

        public async Task<bool> NotifyLogisticsAsync(int orderId, string requestId)
        {
            _logger.LogInformation("Notifying logistics gateway for OrderId: {OrderId}, RequestId: {RequestId}",
                orderId, requestId);

            // Simulate third-party API call with 2-second delay
            await Task.Delay(2000);

            // Simulate successful response
            _logger.LogInformation("Logistics gateway notification successful for OrderId: {OrderId}", orderId);

            return true;
        }
    }

}
