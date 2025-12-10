using Application.DTOs;
using Domain.Entities;

namespace Application.interfaces
{
    public interface IOrderService
    {
        Task<CreateOrderResponse> CreateOrderAsync(Order request);
    }
}
