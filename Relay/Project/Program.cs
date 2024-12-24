using OwlTree;

var rand = new Random();
var logId = rand.Next();

var logFile = $"relay{logId}.log";
Console.WriteLine("relay log id: " + logId.ToString());

var relay = new Connection(new Connection.Args{
    role = Connection.Role.Relay,
    appId = "FarmingWithFriends_OwlTreeExample",
    maxClients = 10,
    printer = (str) => File.AppendAllText(logFile, str),
    verbosity = Logger.Includes().All()
});

relay.OnClientDisconnected += (id) => {
    if (id == relay.Authority)
    {
        Console.WriteLine("Host disconnected, shutting down relay");
        relay.Disconnect();
    }
};

while (relay.IsActive)
{
    relay.ExecuteQueue();
    Console.Write("relay command (h): ");
    var com = Console.ReadLine();
    if (com == null)
        continue;

    var tokens = com.Split(' ');

    var quit = false;

    relay.ExecuteQueue();
    switch (tokens[0])
    {
        case "p":
        case "players":
            Commands.PlayerList(relay);
            break;
        case "q":
        case "quit":
            quit = true;
            break;
        case "ping":
            Commands.Ping(tokens[1], relay);
            break;
        case "d":
        case "disconnect":
            Commands.Disconnect(tokens[1], relay);
            break;
        case "h":
        case "help":
        default:
            Commands.Help();
            break;
    }

    if (quit)
    {
        relay.Disconnect();
        break;
    }
}


