using Application.DTOs;
using Dapper;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {

        private readonly IDbConnection _db;
        public Repository(IDbConnection db)
        {
            _db = db;
        }
        
        //public IEnumerable<T> GetAll() => _context.Set<T>().ToList();
        public async Task<IEnumerable<T>> CheckIdempotency(string requestId)
        {
            var result = await _db.QueryAsync<T>(
                "CheckRequestIdempotency",
                new { RequestId = requestId },
                commandType: CommandType.StoredProcedure
            );
            return result;
        }
        public async Task<SqlMapper.GridReader> GetById(int id)
        {
            var multi = await _db.QueryMultipleAsync(
                "GetOrderById",
                new { OrderId = id },
                commandType: CommandType.StoredProcedure
            );
            // Assuming some logic here to map the multi result to T
            return multi;
        }

        public async Task<T> Add(DynamicParameters parameters)
        {
            var orderResult = await _db.QueryAsync<T>(
                "InsertOrder",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return orderResult.FirstOrDefault();
        }

        public async Task<T> Update(DynamicParameters parameters)
        {
            var orderUpdate = await _db.QueryAsync<T>(
                "UpdateOrder",
                parameters, 
                commandType: CommandType.StoredProcedure
                );

            return orderUpdate.FirstOrDefault();
        }

        public async Task Delete(int id)
        {
            var entity = await GetById(id);
            if (entity != null)
            {
                await _db.ExecuteAsync("DeleteOrder", new { OrderId = id }, commandType: CommandType.StoredProcedure);
            }
        }
        
    }

}
