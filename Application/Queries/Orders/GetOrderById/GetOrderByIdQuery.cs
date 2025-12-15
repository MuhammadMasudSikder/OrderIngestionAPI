using Application.DTOs;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Orders.GetOrderById
{
    public class GetOrderByIdQuery : IRequest<Order>
    {
        public int OrderId { get; set; }
    }
}
