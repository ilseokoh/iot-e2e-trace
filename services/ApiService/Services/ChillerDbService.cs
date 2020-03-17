using BackendService.Data;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiService.Services
{
    public class ChillerDbService : IChillerDbService
    {
        private Container _container;

        public ChillerDbService(CosmosClient dbClient, string databaseName, string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddMessageAsync(ChillerMessage msg)
        {
            await this._container.CreateItemAsync<ChillerMessage>(msg, new PartitionKey(msg.DeviceId));
        }

        public async Task DeleteMessageAsync(string id)
        {
            await this._container.DeleteItemAsync<ChillerMessage>(id, new PartitionKey(id));
        }

        public async Task<ChillerMessage> GetMessageAsync(string id)
        {
            try
            {
                ItemResponse<ChillerMessage> response = await this._container.ReadItemAsync<ChillerMessage>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<ChillerMessage>> GetMessageQueryAsync(string querystring)
        {
            var query = this._container.GetItemQueryIterator<ChillerMessage>(new QueryDefinition(querystring));
            List<ChillerMessage> results = new List<ChillerMessage>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task UpdateMessageAsync(string id, ChillerMessage msg)
        {
            await this._container.UpsertItemAsync<ChillerMessage>(msg, new PartitionKey(id));
        }
    }
}
