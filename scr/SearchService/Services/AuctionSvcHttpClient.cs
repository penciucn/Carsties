using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Entities;

namespace SearchService.Services
{
    public class AuctionSvcHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
       
        public AuctionSvcHttpClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;           
        }

        public async Task<List<Item>> GetAItemsFroSearchDb()
        {
             var latestItemResult = await DB.Default.PagedSearch<Item, Item>()
                 .Sort(x => x.Descending(i => i.UpdatedAt))
                 .PageNumber(1)
                 .PageSize(1)
                 .ExecuteAsync();

             var lastUpdated = latestItemResult.Results.FirstOrDefault()?.UpdatedAt.ToString("o");

             var baseUrl = _configuration["AuctionServiceUrl:BaseUrl"];
             if (string.IsNullOrWhiteSpace(baseUrl))
             {
                 throw new InvalidOperationException("Configuration key 'AuctionServiceUrl:BaseUrl' is missing.");
             }

           return await _httpClient.GetFromJsonAsync<List<Item>>(
            $"{baseUrl}/api/auctions?date={lastUpdated}") ?? new List<Item>();

    }
    }
}