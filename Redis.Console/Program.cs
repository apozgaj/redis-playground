using StackExchange.Redis;

var options = new ConfigurationOptions
{
    EndPoints = new() { "localhost:6379" }
};

var muxer = ConnectionMultiplexer.Connect(options);

var db = muxer.GetDatabase();
Console.WriteLine($"Ping: {db.Ping()}");

// Lua Scripting

var scriptText = @"
    local id = redis.call('incr', @id_key)
    local key = 'key:' .. id
    redis.call('set', key, @value)
    return key";

var script = LuaScript.Prepare(scriptText);

var key1 = db.ScriptEvaluate(script, new { id_key = (RedisKey)"autoincrement", value = "a string value" });

Console.WriteLine($"key 1: {key1}");

// Transactions

var trans = db.CreateTransaction();
trans.HashSetAsync("person:1", new HashEntry[]
{
   new("name", "steve"),
   new("age", 32),
   new("postal_code", "32999")
});

trans.SortedSetAddAsync("person:name:steve", "person:1", 0);
trans.SortedSetAddAsync("person:postal_code:32999", "person:1", 0);
trans.SortedSetAddAsync("person:age", "person:1", 32);

// trans.AddCondition(Condition.HashEqual("person:1", "age", 32));
trans.HashIncrementAsync("person:1", "age");
trans.SortedSetIncrementAsync("person:age", "person:1", 1);

var success = trans.Execute();
Console.WriteLine($"success: {success}");


// Streams

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


// Lists

var fruits = "fruits";
var vegetables = "vegetables";

await db.KeyDeleteAsync(new RedisKey[] { fruits, vegetables });
var numberOfItems = db.ListLeftPush(fruits, new RedisValue[] { "orange", "banana", "mango" });
var listItem = db.ListGetByIndex(fruits, 0);
var entireList = string.Join(", ", db.ListRange(fruits));
Console.WriteLine($"list {entireList}");



// Sets

var allUsers = "users";
var activeUsers = "users:active";
var offlineUsers = "users:offline";

db.KeyDelete(new RedisKey[] { allUsers, activeUsers, offlineUsers });

db.SetAdd(activeUsers, new RedisValue[] { "User1", "User2" });
db.SetAdd(offlineUsers, new RedisValue[] { "User3", "User4" });
db.SetCombineAndStore(SetOperation.Union, allUsers, new RedisKey[] { activeUsers, offlineUsers });

// Hash

var user = "user";
db.KeyDelete(new RedisKey[] { user });

db.HashSet(user, new HashEntry[]
{
    new HashEntry("name", "Andrija"),
    new HashEntry("age", 30),
});

var hasgetAll = db.HashGetAll(user);

Console.WriteLine($"hash {string.Join(", ", hasgetAll)}");

var subscriber = muxer.GetSubscriber();
var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;

var channel = await subscriber.SubscribeAsync("test-channel");

channel.OnMessage(msg =>
{
    Console.WriteLine($"Seq msg received: {msg}, on channel: {msg.Channel}");
});

await subscriber.SubscribeAsync("test-channel", (channel, msg) =>
{
    Console.WriteLine($"Concurrent msg received: {msg}, on channel: {channel}");
});

Task.Run(async () =>
{
    var i = 0;
    while (!cancellationToken.IsCancellationRequested)
    {
        await db.PublishAsync("test-channel", i);
        await Task.Delay(2000);
    }
});

Console.ReadKey();

