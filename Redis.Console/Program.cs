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