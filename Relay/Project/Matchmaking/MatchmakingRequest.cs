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
        public string appId;
        public string sessionId;
        public ServerType serverType;
        public ClientRole clientRole;
        public int maxClients;
        public bool migratable;
        public ushort owlTreeVersion;
        public ushort minOwlTreeVersion;
        public ushort appVersion;
        public ushort minAppVersion;
        public Dictionary<string, string> args;

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
                var content = new StringContent(request, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(_args.serverDomain + "/matchmaking", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return MatchmakingResponse.Deserialize(responseContent);
                }
                else
                {
                    switch ((int)response.StatusCode)
                    {
                        case (int)ResponseCodes.NotFound: return MatchmakingResponse.NotFound;
                        case (int)ResponseCodes.RequestRejected: return MatchmakingResponse.RequestRejected;
                    }
                    return MatchmakingResponse.NotFound;
                }
            }
            catch
            {
                return MatchmakingResponse.ExceptionThrown;
            }
        }
    }
}