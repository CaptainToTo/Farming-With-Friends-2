using OwlTree;
using OwlTree.Matchmaking;

public static class Program
{
    public static ConnectionManager? relays;

    public static void Main(string[] args)
    {
        var endpoint = new MatchmakingEndpoint("http://localhost:5000/", HandleRequest);
        relays = new ConnectionManager();
        endpoint.Start();
        HandleCommands();
        endpoint.Close();
        relays.DisconnectAll();
    }

    public static MatchmakingResponse HandleRequest(MatchmakingRequest request)
    {
        if (!relays!.Contains(request.sessionId))
        {
            var logFile = $"relay{request.sessionId}.log";
            File.WriteAllText(logFile, "");

            var connection = relays.Add(request.sessionId, new Connection.Args{
                appId = request.appId,
                role = Connection.Role.Relay,
                tcpPort = 0,
                udpPort = 0,
                maxClients = request.maxClients,
                migratable = request.migratable,
                owlTreeVersion = request.owlTreeVersion,
                minOwlTreeVersion = request.minOwlTreeVersion,
                appVersion = request.appVersion,
                minAppVersion = request.minAppVersion,
                printer = (str) => File.AppendAllText(logFile, str),
                verbosity = Logger.Includes().All()
            });

            return new MatchmakingResponse{
                responseCode = ResponseCodes.RequestAccepted,
                serverAddr = "127.0.0.1",
                udpPort = connection.ServerUdpPort,
                tcpPort = connection.ServerTcpPort,
                sessionId = request.sessionId,
                appId = request.appId,
                serverType = ServerType.Relay
            };
        }
        return MatchmakingResponse.RequestRejected;
    }

    public static void HandleCommands()
    {
        while (true)
        {
            Console.Write("input command (h): ");
            var com = Console.ReadLine();
            if (com == null)
                continue;

            var tokens = com.Split(' ');

            var quit = false;

            switch (tokens[0])
            {
                case "r":
                case "relays":
                    Commands.RelayList(relays);
                    break;
                case "p":
                case "players":
                    Commands.PlayerList(relays!.Get(tokens[1])!);
                    break;
                case "q":
                case "quit":
                    quit = true;
                    break;
                case "ping":
                    Commands.Ping(tokens[2], relays!.Get(tokens[1])!);
                    break;
                case "d":
                case "disconnect":
                    Commands.Disconnect(tokens[2], relays!.Get(tokens[1])!);
                    break;
                case "h":
                case "help":
                default:
                    Commands.Help();
                    break;
            }

            if (quit)
            {
                break;
            }
        }
    }
}

// var rand = new Random();
// var logId = rand.Next();

// var logFile = $"relay{logId}.log";
// Console.WriteLine("relay log id: " + logId.ToString());

// var relay = new Connection(new Connection.Args{
//     role = Connection.Role.Relay,
//     appId = "FarmingWithFriends_OwlTreeExample",
//     migratable = true,
//     shutdownWhenEmpty = false,
//     maxClients = 10,
//     useCompression = true,
//     printer = (str) => File.AppendAllText(logFile, str),
//     verbosity = Logger.Includes().All()
// });

// while (relay.IsActive)
// {
//     relay.ExecuteQueue();
//     Console.Write("relay command (h): ");
//     var com = Console.ReadLine();
//     if (com == null)
//         continue;

//     var tokens = com.Split(' ');

//     var quit = false;

//     relay.ExecuteQueue();
//     switch (tokens[0])
//     {
//         case "p":
//         case "players":
//             Commands.PlayerList(relay);
//             break;
//         case "q":
//         case "quit":
//             quit = true;
//             break;
//         case "ping":
//             Commands.Ping(tokens[1], relay);
//             break;
//         case "d":
//         case "disconnect":
//             Commands.Disconnect(tokens[1], relay);
//             break;
//         case "h":
//         case "help":
//         default:
//             Commands.Help();
//             break;
//     }

//     if (quit)
//     {
//         relay.Disconnect();
//         break;
//     }
// }


