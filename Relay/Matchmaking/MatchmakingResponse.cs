

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OwlTree.Matchmaking
{
    public enum ResponseCodes
    {
        Invalid = 0,

        RequestAccepted = 200,

        NotFound = 404,
        ExceptionThrow = 410,
        RequestRejected = 411
    }

    public struct MatchmakingResponse
    {
        public ResponseCodes responseCode { get; set; }

        [JsonIgnore]
        public bool RequestSuccessful => 200 <= (int)responseCode && (int)responseCode <= 299;
        [JsonIgnore]
        public bool RequestFailed => 400 <= (int)responseCode && (int)responseCode <= 499;

        public string serverAddr { get; set; }

        public int udpPort { get; set; }

        public int tcpPort { get; set; }

        public string sessionId { get; set; }

        public string appId { get; set; }

        public ServerType serverType { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static MatchmakingResponse Deserialize(string data)
        {
            return JsonSerializer.Deserialize<MatchmakingResponse>(data);
        }

        public static MatchmakingResponse NotFound = new MatchmakingResponse{
            responseCode = ResponseCodes.NotFound
        };

        public static MatchmakingResponse ExceptionThrown = new MatchmakingResponse{
            responseCode = ResponseCodes.ExceptionThrow,
        };

        public static MatchmakingResponse RequestRejected = new MatchmakingResponse{
            responseCode = ResponseCodes.RequestRejected
        };
    }
}