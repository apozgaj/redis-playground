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



//
// sets
//

var allUsers = "users";
var activeUsers = "users:active";
var offlineUsers = "users:offline";

db.KeyDelete(new RedisKey[] { allUsers, activeUsers, offlineUsers });

db.SetAdd(activeUsers, new RedisValue[] { "User1", "User2" });
db.SetAdd(offlineUsers, new RedisValue[] { "User3", "User4" });
db.SetCombineAndStore(SetOperation.Union, allUsers, new RedisKey[] { activeUsers, offlineUsers });

//
// hash
//

var user = "user";
db.KeyDelete(new RedisKey[] { user });

db.HashSet(user, new HashEntry[]
{
    new HashEntry("name", "Andrija"),
    new HashEntry("age", 30),
});

var hasgetAll = db.HashGetAll(user);

Console.WriteLine($"hash {string.Join(", ", hasgetAll)}");