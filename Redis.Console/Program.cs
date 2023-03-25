using StackExchange.Redis;

var options = new ConfigurationOptions
{
    EndPoints = new() { "localhost:6379" }
};

var muxer = ConnectionMultiplexer.Connect(options);

var db = muxer.GetDatabase();

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