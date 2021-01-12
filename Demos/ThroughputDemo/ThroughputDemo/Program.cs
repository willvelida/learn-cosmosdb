using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;

namespace ThroughputDemo
{
    class Program
    {

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = configuration["CosmosConnectionString"];

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException("Please specify a connection string in the appsettings.json file");
                }

                using (CosmosClient cosmosClient = new CosmosClient(connectionString))
                {
                    // Create a database with manual throughput
                    string databaseOne = "HotelDBv1";
                    string databaseTwo = "HotelDBv2";
                    string containerOne = "ContainerOne";
                    string containerTwo = "ContainerTwo";

                    Database databaseManual = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseOne, throughput: 400);
                    Console.WriteLine($"{databaseManual.Id} has been created!");

                    // Create a collection with manual throughput
                    Container containerManual = await cosmosClient.GetDatabase(databaseOne).CreateContainerAsync(
                        id: containerOne,
                        partitionKeyPath: "/CityName",
                        throughput: 400
                        );
                    Console.WriteLine($"{containerManual.Id} has been created!");

                    // Create a database with autoscale throughput
                    ThroughputProperties autoscaleThroughputProperties = ThroughputProperties.CreateAutoscaleThroughput(4000);

                    Database databaseAutoscale = await cosmosClient.CreateDatabaseAsync(databaseTwo, throughputProperties: autoscaleThroughputProperties);
                    Console.WriteLine($"{databaseAutoscale.Id} has been created!");

                    // Create a collection with autoscale throughput
                    ContainerProperties autoscaleContainerProperties = new ContainerProperties(containerTwo, "/CityName");

                    Container containerAutoscale = await databaseAutoscale.CreateContainerAsync(autoscaleContainerProperties, autoscaleThroughputProperties);
                    Console.WriteLine($"{containerAutoscale.Id} has been created!");
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown: {ex.Message}");
                throw;
            }
        }
    }
}
