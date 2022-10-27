using System;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InsertRecords
{
    public class ProcessUser
    {
        [Disable("DisableProcessUser")]
        [FunctionName("ProcessUser")]
        public async Task RunAsync([QueueTrigger("users", Connection = "AzureTableStorage")] string myQueueItem, ILogger log)
        {
            var user = System.Text.Json.JsonSerializer.Deserialize<User>(myQueueItem);
            var dbClient = new MongoClient(Environment.GetEnvironmentVariable("MongoDbConnectionString"));
            var database = dbClient.GetDatabase("apimatic-testing");
            var usersCollection = database.GetCollection<BsonDocument>("users");
            var teamsCollection = database.GetCollection<BsonDocument>("teams2");
            var collections = database.ListCollectionNames().ToList();

            if (!collections.Contains("tenants"))
                database.CreateCollection("tenants");

            var tenantsCollection = database.GetCollection<BsonDocument>("tenants");

            using (var session = await dbClient.StartSessionAsync())
            {
                session.StartTransaction();

                try
                {
                    var tenant = new BsonDocument();
                    tenant.Add(new BsonElement("Name", user.Company));
                    tenant.Add(new BsonElement("CreatedBy", user.Id));
                    tenant.Add(new BsonElement("CreatedAt", DateTime.UtcNow));

                    await tenantsCollection.InsertOneAsync(session, tenant);

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(user.Id));
                    var update = Builders<BsonDocument>.Update.Set("TenantId", tenant["_id"].AsObjectId.ToString());

                    var result = await usersCollection.UpdateOneAsync(session, filter, update);

                    if (!result.IsModifiedCountAvailable || result.ModifiedCount <= 0)
                        throw new Exception($"User with Id: {user.Id} does not exist in the database");

                    filter = Builders<BsonDocument>.Filter.Eq("OwnerID", user.Id);
                    update = Builders<BsonDocument>.Update.Set("TenantId", tenant["_id"].AsObjectId.ToString());

                    result = await teamsCollection.UpdateOneAsync(session, filter, update);

                    await session.CommitTransactionAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error writing to MongoDB: " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }       
        }
    }
}
