using Application.Commands.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.interfaces
{
    public interface IIngestOrderMessage
    {
        int OrderId { get; }
        string RequestId { get; }
        string Status { get; }
        decimal TotalAmount { get; }
    }
}
