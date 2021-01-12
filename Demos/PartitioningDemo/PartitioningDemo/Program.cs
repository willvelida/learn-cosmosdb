using Bogus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PartitioningDemo
{
    class Program
    {
        private static readonly string databaseId = "HotelDB";
        private static readonly string readHeavyId = "Hotels";

        static async Task Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = configuration["CosmosConnectionString"];

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException("Please specify a connection string in the appSettings.json file");
                }

                using (CosmosClient client = new CosmosClient(connectionString))
                {
                    var hotelContainer = await CreateDemoEnvironment(client);
                    await GenerateData(hotelContainer, 100000);
                    await QueryDataWithinPartition(hotelContainer);
                    await QueryDataWithinPartitionWithFilter(hotelContainer);
                    await QueryDataWithCrossPartitionQuery(hotelContainer);
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception throw: {ex.Message}");
                throw;
            }
        }

        private static async Task<Container> CreateDemoEnvironment(CosmosClient client)
        {
            ContainerResponse hotelContainer = null;

            try
            {
                // Set up a database
                Microsoft.Azure.Cosmos.Database database = await client.CreateDatabaseIfNotExistsAsync(databaseId);

                // Container and Throughput Properties
                ContainerProperties containerProperties = new ContainerProperties(readHeavyId, "/CityName");
                ThroughputProperties throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(20000);

                // Create a read heavy environment
                hotelContainer = await database.CreateContainerAsync(
                    containerProperties,
                    throughputProperties);              
            }
            catch (CosmosException ce)
            {
                Console.WriteLine($"Exception thrown by Cosmos DB: {ce.StatusCode}, {ce.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception thrown: {ex.Message}");
                throw;
            }

            return hotelContainer;
        }

        private static async Task GenerateData(Container readContainer, int itemsToGenerate)
        {
            try
            {
                // Generate Data
                var fakeHotelData = new Faker<Hotel>()
                    .RuleFor(h => h.HotelId, (fake) => Guid.NewGuid().ToString())
                    .RuleFor(h => h.HotelName, (fake) => fake.PickRandom(new List<string> { "Easy Motel", "Grand Hotel", "Plaza", "Abis", "Hotel Centre", "Hotel Old", "Pub", "Motel", "Hostel", "Backpackers" }))
                    .RuleFor(h => h.StarRating, (fake) => fake.Random.Number(1, 5))
                    .RuleFor(h => h.CityName, (fake) => fake.PickRandom(new List<string> { "Auckland", "London", "Paris", "Redmond", "Sydney" }))
                    .GenerateLazy(itemsToGenerate);

                // Add to read container
                foreach (var hotel in fakeHotelData)
                {
                    await readContainer.CreateItemAsync(
                        hotel,
                        new PartitionKey(hotel.CityName));
                    Console.WriteLine($"HotelId: {hotel.HotelId}");
                }               
            }
            catch (CosmosException ce)
            {
                Console.WriteLine($"CosmosDB Exception thrown: {ce.StatusCode}, {ce.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown: {ex.Message}");
                throw;
            }                      
        }

        private static async Task QueryDataWithinPartition(Container readContainer)
        {
            // Querry within a partition
            Console.WriteLine("Searching for all hotels that are in Auckland...");
            QueryDefinition aucklandQuery = new QueryDefinition(
                "SELECT * FROM Hotels c WHERE c.CityName = 'Auckland'");

            FeedIterator<Hotel> aucklandIterator = readContainer.GetItemQueryIterator<Hotel>(
                aucklandQuery);

            while (aucklandIterator.HasMoreResults)
            {
                FeedResponse<Hotel> aucklandResponse = await aucklandIterator.ReadNextAsync();
                foreach (var hotel in aucklandResponse)
                {
                    PrintHotel(hotel);
                }

                Console.WriteLine($"Total of {aucklandResponse.Count} results");
                Console.WriteLine($"This query cost: {aucklandResponse.RequestCharge} RU's");
                Console.WriteLine("=======================================================");
                Console.WriteLine();
            }           
        }

        private static async Task QueryDataWithinPartitionWithFilter(Container readContainer)
        {
            // Perform a range query within a partition
            Console.WriteLine("Searching for all hotels that are in Auckland AND have a star rating less than 3");
            QueryDefinition aucklandFilterQuery = new QueryDefinition(
                "SELECT * FROM Hotels c WHERE c.CityName = 'Auckland' AND c.StarRating < 3");

            FeedIterator<Hotel> aucklandFilterIterator = readContainer.GetItemQueryIterator<Hotel>(
                aucklandFilterQuery);

            while (aucklandFilterIterator.HasMoreResults)
            {
                FeedResponse<Hotel> aucklandFilterResponse = await aucklandFilterIterator.ReadNextAsync();
                foreach (var hotel in aucklandFilterResponse)
                {
                    PrintHotel(hotel);
                }

                Console.WriteLine($"Total of {aucklandFilterResponse.Count} results");
                Console.WriteLine($"This query cost: {aucklandFilterResponse.RequestCharge} RU's");
                Console.WriteLine("=======================================================");
                Console.WriteLine();
            }
        }

        private static async Task QueryDataWithCrossPartitionQuery(Container readContainer)
        {
            // Perform a Cross-Partition Query
            Console.WriteLine("Searching for all hotels with a star rating less than 3");
            QueryDefinition starRatingQuery = new QueryDefinition(
                "SELECT * FROM Hotels c WHERE c.StarRating < 3");

            FeedIterator<Hotel> starRatingIterator = readContainer.GetItemQueryIterator<Hotel>(
                queryDefinition: starRatingQuery,
                requestOptions: new QueryRequestOptions()
                {
                    MaxConcurrency = -1,
                    MaxBufferedItemCount = -1
                });

            while (starRatingIterator.HasMoreResults)
            {
                FeedResponse<Hotel> starRatingFilterResponse = await starRatingIterator.ReadNextAsync();
                foreach (var hotel in starRatingFilterResponse)
                {
                    PrintHotel(hotel);

                }

                Console.WriteLine($"Total of {starRatingFilterResponse.Count} results");
                Console.WriteLine($"This query cost: {starRatingFilterResponse.RequestCharge}");
                Console.WriteLine("=======================================================");
                Console.WriteLine();
            }
        }

        private static void PrintHotel(Hotel hotel)
        {
            Console.WriteLine("Hotel result");
            Console.WriteLine("====================");
            Console.WriteLine($"Id: {hotel.HotelId}");
            Console.WriteLine($"Name: {hotel.HotelName}");
            Console.WriteLine($"City: {hotel.CityName}");
            Console.WriteLine($"Star Rating: {hotel.StarRating}");
            Console.WriteLine("====================");
        }
    }
}
