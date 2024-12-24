using OwlTree;

var rand = new Random();
var logId = rand.Next();
var logFile = $"client{logId}.log";
Console.WriteLine("client log id: " + logId.ToString());

var client = new Connection(new Connection.Args{
    role = Connection.Role.Client,
    appId = "FarmingWithFriends_OwlTreeExample",
    printer = (str) => File.AppendAllText(logFile, str),
    verbosity = Logger.Includes().All()
});

client.OnReady += (id) => {
    if (client.IsHost)
        Console.WriteLine("assigned as host");
    Console.WriteLine("assigned client id: " + id.ToString());
};

client.OnLocalDisconnect += (id) => {
    Console.WriteLine("disconnected");
};

while (client.IsActive)
{
    client.ExecuteQueue();
    Thread.Sleep(5);
}

client.Disconnect();
