using StackExchange.Redis;

var options = new ConfigurationOptions
{
    EndPoints = new() { "localhost:6379" }
};

var muxer = ConnectionMultiplexer.Connect(options);

var db = muxer.GetDatabase();

Console.WriteLine($"Ping: {db.Ping()}");

// streams

var sensor1 = "sensor:1";
var sensor2 = "sensor:2";

db.KeyDelete(new RedisKey[] { sensor1, sensor2 });

Task.Run(async () =>
{
    var t1 = 1;
    var t2 = 2;

    while (true)
    {
        await db.StreamAddAsync(sensor1, new NameValueEntry[] { new NameValueEntry("temp1", t1) });
        await db.StreamAddAsync(sensor2, new NameValueEntry[] { new NameValueEntry("temp2", t2) });

        await Task.Delay(5000);
    }
});


Task.Run(async () =>
{
    var positions = new Dictionary<string, StreamPosition>()
    {
        { sensor1, new StreamPosition(sensor1, "0-0") },
        { sensor2, new StreamPosition(sensor2, "0-0") }
    };


    while (true)
    {
        var readResults = await db.StreamReadAsync(positions.Values.ToArray(), countPerStream: 1);
        if (!readResults.Any())
        {
            Console.WriteLine("No results");
            await Task.Delay(1000);
        }

        foreach (var res in readResults)
        {
            foreach (var entry in res.Entries)
            {
                Console.WriteLine($"stream: {res.Key}, entry: {entry.Id} values: {string.Join(", ", entry.Values)}");
                positions[res.Key!] = new StreamPosition(res.Key, entry.Id);
            }
        }
    }
});

Console.ReadKey();

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

