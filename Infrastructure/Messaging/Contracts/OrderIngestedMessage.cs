using Domain.Entities;

namespace Infrastructure.Messaging.Contracts;

public class OrderIngestedMessage
{
    public Order? MsgContext { get; set; }
}