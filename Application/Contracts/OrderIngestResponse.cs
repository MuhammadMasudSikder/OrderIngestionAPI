using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public class OrderIngestResponse
    {
        public string? OrderId { get; set; }
        public bool IsSuccess { get; set; }
        public string? Status { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
