using Application.Queries.Orders.GetOrderById;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Handlers
{
    public class GetOrderByIdQueryHandler
    : IRequestHandler<GetOrderByIdQuery, Order>
    {
        private readonly IOrderRepository _repo;

        public GetOrderByIdQueryHandler(IOrderRepository repo)
        {
            _repo = repo;
        }

        public async Task<Order> Handle(
            GetOrderByIdQuery request,
            CancellationToken cancellationToken)
        {
            var order = await _repo.GetOrderByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                throw new Exception($"Order with ID {request.OrderId} not found.");
            }
            return order;
        }
    }

}
