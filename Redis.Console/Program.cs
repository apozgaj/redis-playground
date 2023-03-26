using StackExchange.Redis;

var options = new ConfigurationOptions
{
    EndPoints = new() { "localhost:6379" }
};

var muxer = ConnectionMultiplexer.Connect(options);

var db = muxer.GetDatabase();

// Lua Scripting

var scriptText = @"
    local id = redis.call('incr', @id_key)
    local key = 'key:' .. id
    redis.call('set', key, @value)
    return key";

var script = LuaScript.Prepare(scriptText);

var key1 = db.ScriptEvaluate(script, new { id_key = (RedisKey)"autoincrement", value = "a string value" });

Console.WriteLine($"key 1: {key1}");