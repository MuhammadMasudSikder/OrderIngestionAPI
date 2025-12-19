using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public interface IRepository<T> where T : class
    {
        //IEnumerable<T> GetAll();
        Task<IEnumerable<T>> CheckIdempotency(string requestId);
        Task<SqlMapper.GridReader> GetById(int id);
        Task<T> Add(DynamicParameters parameters);
        Task<T> Update(DynamicParameters parameters);
        Task Delete(int id);
    }
}
