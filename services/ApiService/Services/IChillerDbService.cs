using BackendService.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiService.Services
{
    public interface IChillerDbService
    {
        Task<IEnumerable<ChillerMessage>> GetMessageQueryAsync(string query);

        Task<ChillerMessage> GetMessageAsync(string id);

        Task AddMessageAsync(ChillerMessage msg);

        Task UpdateMessageAsync(string id, ChillerMessage msg);

        Task DeleteMessageAsync(string id);
    }
}
