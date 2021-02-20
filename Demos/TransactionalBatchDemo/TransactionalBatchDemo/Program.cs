using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace TransactionalBatchDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string connectionString = configuration.GetConnectionString("CosmosDBConnectionString");
            string databaseName = configuration["CosmosDBSettings:DatabaseName"];
            string containerName = configuration["CosmosDBSettings:ContainerName"];

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("Please specify a connection string to Cosmos DB in the appsettings.json file");
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException("Please provide a valid database value in the appsettings.json file");
            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentNullException("Please provide a valid container value in the appsettings.json file");

            using (var cosmosClient = new CosmosClient(connectionString))
            {
                var contactContainer = cosmosClient.GetContainer(databaseName, containerName);

                var contactId = Guid.NewGuid().ToString();
                var contactName = "Will";
                
                Address address = new Address { ContactId = contactId, ContactName = contactName, AddressLine1 = "1 Made Up Lane", AddressLine2 = "Pretend Drive", City = "Auckland", State = "Auckland", ZipCode = 7171 };
                Contact contact = new Contact { ContactId = contactId, ContactName = contactName };

                TransactionalBatch batch = contactContainer.CreateTransactionalBatch(new PartitionKey(contactName))
                    .CreateItem<Contact>(contact);
                    //.CreateItem<Address>(address);

                TransactionalBatchResponse batchResponse = await batch.ExecuteAsync();

                using (batchResponse)
                {
                    if (batchResponse.IsSuccessStatusCode)
                    {
                        TransactionalBatchOperationResult<Contact> contactResult = batchResponse.GetOperationResultAtIndex<Contact>(0);
                        Contact contactResultResource = contactResult.Resource;
                        TransactionalBatchOperationResult<Address> addressResult = batchResponse.GetOperationResultAtIndex<Address>(0);
                        Address addressResultResource = addressResult.Resource;
                    }
                    else
                    {
                        TransactionalBatchOperationResult<Contact> contactResult = batchResponse.GetOperationResultAtIndex<Contact>(0);
                        var contactStatusCode = contactResult.StatusCode;
                        Console.WriteLine($"Response code for Contact is: {contactStatusCode}");
                        TransactionalBatchOperationResult<Address> addressResult = batchResponse.GetOperationResultAtIndex<Address>(0);
                        var addressStatusCode = addressResult.StatusCode;
                        Console.WriteLine($"Response code for Contact is: {addressStatusCode}");
                    }
                }
            }
        }
    }
}
