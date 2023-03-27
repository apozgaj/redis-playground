using StackExchange.Redis;

var options = new ConfigurationOptions
{
    EndPoints = new() { "localhost:6379" }
};

var muxer = ConnectionMultiplexer.Connect(options);

var db = muxer.GetDatabase();

Console.WriteLine($"Ping: {db.Ping()}");

//
// lists
//
var fruits = "fruits";
var vegetables = "vegetables";

await db.KeyDeleteAsync(new RedisKey[] { fruits, vegetables });
var numberOfItems = db.ListLeftPush(fruits, new RedisValue[] { "orange", "banana", "mango" });
var listItem = db.ListGetByIndex(fruits, 0);
var entireList = string.Join(", ", db.ListRange(fruits));
Console.WriteLine($"list {entireList}");