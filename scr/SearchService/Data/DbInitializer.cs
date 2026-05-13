using MongoDB.Entities;
using MongoDB.Driver;
using SearchService;

using DB = MongoDB.Entities.DB;
using System.Text.Json;
using SearchService.Services;
namespace SearchService
{
    public class DbInitializer
    {
        public static async Task InitDb(WebApplication app)
        {
            var connectionString = app.Configuration.GetConnectionString("MongoDbConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'MongoDbConnection' is missing. Configure it in appsettings or environment variables.");
            }

        var db = await DB.InitAsync(
                "SearchDb",
                MongoClientSettings.FromConnectionString(connectionString));

            await db.Index<Item>()
                .Key(i => i.Make, KeyType.Text)
                .Key(i => i.Model, KeyType.Text)
                .Key(i => i.Color, KeyType.Text)
                .CreateAsync();

            var count = await db.CountAsync<Item>();

            if (count > 0)
            {
                Console.WriteLine($"Search DB already contains {count} items. Skipping initial sync.");
                return;
            }

            using var scope = app.Services.CreateScope();
            var httpClient = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();

            try
            {
                var items = await httpClient.GetAItemsFroSearchDb();

                Console.WriteLine($"Fetched {items.Count} items from Auction Service.");

                if (items.Count > 0)
                {
                    await db.SaveAsync(items);
                    Console.WriteLine("Items saved to MongoDB.");
                }
                else
                {
                    Console.WriteLine("No new items to save.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Auction service is unavailable during startup sync: {ex.Message}");
                Console.WriteLine("Search service will continue to start without initial sync.");
            }
        }
        }
    }