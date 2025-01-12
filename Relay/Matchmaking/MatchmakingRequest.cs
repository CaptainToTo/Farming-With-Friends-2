using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OwlTree.Matchmaking
{
    public enum ServerType
    {
        ServerAuthoritative,
        Relay
    }

    public enum ClientRole
    {
        Host,
        Client
    }

    public struct MatchmakingRequest
    {
        public string appId { get; set; }
        public string sessionId { get; set; }
        public ServerType serverType { get; set; }
        public ClientRole clientRole { get; set; }
        public int maxClients { get; set; }
        public bool migratable { get; set; }
        public ushort owlTreeVersion { get; set; }
        public ushort minOwlTreeVersion { get; set; }
        public ushort appVersion { get; set; }
        public ushort minAppVersion { get; set; }
        public Dictionary<string, string> args { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static MatchmakingRequest Deserialize(string data)
        {
            return JsonSerializer.Deserialize<MatchmakingRequest>(data);
        }
    }

    public class MatchmakingClient
    {

        public class Args
        {
            public string serverDomain = "";

            public string appId = "";

            public string sessionId = "";

            public ServerType serverType = ServerType.Relay;

            public ClientRole clientRole = ClientRole.Client;

            public int maxClients = 4;

            public bool migratable = false;

            public ushort owlTreeVersion = 1;

            public ushort minOwlTreeVersion = 0;

            public ushort appVersion = 1;

            public ushort minAppVersion = 0;

            public Dictionary<string, string> args = new();
        }

        private Args _args;

        public MatchmakingClient(Args args)
        {
            _args = args;
        }

        public async Task<MatchmakingResponse> MakeRequest()
        {
            using var client = new HttpClient();

            try
            {
                var request = new MatchmakingRequest{
                    appId = _args.appId,
                    sessionId = _args.sessionId,
                    serverType = _args.serverType,
                    clientRole = _args.clientRole,
                    maxClients = _args.maxClients,
                    migratable = _args.migratable,
                    owlTreeVersion = _args.owlTreeVersion,
                    minOwlTreeVersion = _args.minOwlTreeVersion,
                    appVersion = _args.appVersion,
                    minAppVersion = _args.minAppVersion,
                    args = _args.args
                }.Serialize();
                Console.WriteLine(request);
                var content = new StringContent(request, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(_args.serverDomain + "/matchmaking", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                    return MatchmakingResponse.Deserialize(responseContent);
                }
                else
                {
                    switch ((int)response.StatusCode)
                    {
                        case (int)ResponseCodes.RequestRejected: return MatchmakingResponse.RequestRejected;
                        case (int)ResponseCodes.NotFound: 
                        default:
                        return MatchmakingResponse.NotFound;
                    }
                }
            }
            catch
            {
                return MatchmakingResponse.ExceptionThrown;
            }
        }
    }
}