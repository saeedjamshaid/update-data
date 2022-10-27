using MongoDB.Bson;

namespace Common
{
    public record User(string Id, string? Company, string? TeamId)
    {
        public static User Create(BsonDocument doc)
        {
            return new(doc["_id"].AsObjectId.ToString(), doc["Company"].BsonType == BsonType.Null ? null : doc["Company"].ToString(),
                doc["TeamID"].BsonType == BsonType.Null ? null : doc["TeamID"].ToString());
        }
    }
}