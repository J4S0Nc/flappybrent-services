using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Flappy.Brent.Services.Models;
using System.Collections.Generic;
using System.Linq;

namespace Flappy.Brent.Services
{
    public static class flappyFunctions
    {
        [FunctionName("FlappyPlayerStats_GetAllPlayerStats")]
        public static async Task<HttpResponseMessage> GetAllPlayerStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players")] HttpRequestMessage req,
            [Table("Data", Connection = "AzureWebJobsStorage")] CloudTable dataTable,
            ILogger log)
        {
            var q = req.RequestUri.ParseQueryString();
            var sort = q["sort"] ?? "score";
            string partition = "FlappyPlayers";
            TableQuery<PlayerEntity> query = new TableQuery<PlayerEntity>()
                .Where(TableQuery.GenerateFilterCondition(nameof(PlayerEntity.PartitionKey), QueryComparisons.Equal, partition));
            List<PlayerEntity> entities = await GetRange(dataTable, query);
            var sortBy = getSort(sort);

            var sorted = entities.OrderByDescending(sortBy);
            var models = sorted.Select(e => e.ToModel());
            return req.CreateResponse(models);
        }

        private static Func<PlayerEntity, object> getSort(string sort)
        {
            switch (sort)
            {
                case "drinks":
                    return e => e.drinks;
                case "brentDeaths":
                    return e => e.brentDeaths;
                case "fallingDeaths":
                    return e => e.fallingDeaths;
                case "deaths":
                    return e => e.brentDeaths + e.fallingDeaths;
                case "flaps":
                    return e => e.flaps;
                case "playTime":
                    return e => e.playTime;
                case "songs":
                    return e => e.songs;
                case "lastSeen":
                    return e => e.lastSeen;
                case "name":
                    return e => e.name;
                case "score":
                default:
                    return e => e.highScore;
            }
        }
        [FunctionName("FlappyPlayerStats_GetPlayerStatsById")]
        public static async Task<HttpResponseMessage> GetPlayerStatsById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/{id}")] HttpRequestMessage req, string id,
            [Table("Data", Connection = "AzureWebJobsStorage")] CloudTable dataTable,
            ILogger log)
        {
            var q = req.RequestUri.ParseQueryString();

            string partition = "FlappyPlayers";
            string rowId = id;
            var retrieveOperation = TableOperation.Retrieve<PlayerEntity>(partition, rowId);
            var currentRecord = await dataTable.ExecuteAsync(retrieveOperation);
            var result = currentRecord.Result as PlayerEntity;
            var model = result.ToModel(true);
            return req.CreateResponse(model);
        }

        [FunctionName("FlappyPlayerStats_PostPlayerStats")]
        public static async Task<HttpResponseMessage> PostPlayerStats(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "player/stats")] HttpRequestMessage req,
           [Table("Data", Connection = "AzureWebJobsStorage")] CloudTable dataTable,
           ILogger log)
        {
            var body = await req.Content.ReadAsStringAsync();
            var model = JsonConvert.DeserializeObject<PlayerModel>(body);// await req.Content.ReadAsAsync<PlayerModel>();
            if (model.user == null || string.IsNullOrWhiteSpace(model.user.id) || model.user.id == "0")
            {
                model.user = new User
                {
                    id = Guid.NewGuid().ToString("N"),
                    key = Guid.NewGuid().ToString("N")
                };
            }
            else
            {
                byte[] hashbytes = System.Convert.FromBase64String(model.hash);
                var hashedkey = System.Text.ASCIIEncoding.ASCII.GetString(hashbytes);
                if (model.user.key != hashedkey)
                    return req.CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, "You is a bad brent..");
            }

            string partition = "FlappyPlayers";
            string rowId = model.user.id;
            var data = model.ToEntity();
            data.PartitionKey = partition;
            var saveOperation = TableOperation.InsertOrMerge(data);
            await dataTable.ExecuteAsync(saveOperation);

            return req.CreateResponse(new { model.user.id, model.user.key });
        }



        private static async Task<List<T>> GetRange<T>(CloudTable dataTable, TableQuery<T> query) where T : ITableEntity, new()
        {
            List<T> entities = new List<T>();
            TableContinuationToken next = null;
            do
            {
                var results = await dataTable.ExecuteQuerySegmentedAsync(query, next);
                entities.AddRange(results.Results);
                next = results.ContinuationToken;

            } while (next != null);
            return entities;
        }
    }
}
