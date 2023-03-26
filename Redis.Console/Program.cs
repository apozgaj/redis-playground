using StackExchange.Redis;

var options = new ConfigurationOptions
{
    EndPoints = new() { "localhost:6379" }
};

var muxer = ConnectionMultiplexer.Connect(options);

var db = muxer.GetDatabase();

Console.WriteLine($"Ping: {db.Ping()}");

// transactions
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