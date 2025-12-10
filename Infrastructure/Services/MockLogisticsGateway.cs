using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class MockLogisticsGateway : ILogisticsGateway
    {
        public async Task<bool> SendToLogisticsAsync(string orderId)
        {
            // Simulate slow external API (2 seconds)
            await Task.Delay(2000);
            return true;
        }
    }

}
