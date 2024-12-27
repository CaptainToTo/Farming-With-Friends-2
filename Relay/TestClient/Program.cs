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

client.OnClientConnected += (id) => {
    Console.WriteLine($"client {id} connected");
};

client.OnHostMigration += (id) => {
    if (client.IsHost)
        Console.WriteLine("you are now the host");
    else
        Console.WriteLine($"client {id} assigned as new host");
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
