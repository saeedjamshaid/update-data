// See https://aka.ms/new-console-template for more information
using Azure.Storage.Queues;
using Common;
using MongoDB.Bson;
using MongoDB.Driver;

var dbClient = ResourceGetter.GetMongoDbClient();
var database = dbClient.GetDatabase("apimatic-testing");
var usersCollection = database.GetCollection<BsonDocument>("users");

var filter = Builders<BsonDocument>.Filter.Eq("TenantId", BsonNull.Value);

var options = new FindOptions<BsonDocument>
{
    BatchSize = 100
};

QueueClient queueClient = ResourceGetter.GetQueueClient("users");
queueClient.DeleteIfExists();
queueClient.CreateIfNotExists();

using (var cursor = await usersCollection.FindAsync(filter, options))
{
    int recordsProcessed = 0;

    while (await cursor.MoveNextAsync())
    {
        var batch = cursor.Current;
        Console.WriteLine($"Processing records from {recordsProcessed} to {recordsProcessed + batch.Count()}");
        foreach (var doc in batch)
        {
            queueClient.SendMessage(System.Text.Json.JsonSerializer.Serialize(User.Create(doc)));
        }
        recordsProcessed += batch.Count();
    }
}

public static class ResourceGetter
{
    public static readonly Secrets _secrets = new AppSettingReader().ReadSection<Secrets>("Secrets");
    public static MongoClient GetMongoDbClient()
    {
        return new MongoClient(_secrets.MongoDbConnectionString);
    }

    public static QueueClient GetQueueClient(string queueName)
    {
        return new QueueClient(_secrets.AzureTableStorage, queueName, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
    }
}

public class Secrets
{
    public string AzureTableStorage { get; set; }

    public string MongoDbConnectionString { get; set; }
}