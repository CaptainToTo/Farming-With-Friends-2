

using System.Text.Json;

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
        private int _responseCode;

        public ResponseCodes ResponseCode {
            get {
                switch(_responseCode)
                {
                    case (int)ResponseCodes.RequestAccepted: return ResponseCodes.RequestAccepted;

                    case (int)ResponseCodes.NotFound: return ResponseCodes.NotFound;
                    case (int)ResponseCodes.ExceptionThrow: return ResponseCodes.ExceptionThrow;
                    case (int)ResponseCodes.RequestRejected: return ResponseCodes.RequestRejected;

                    default: return ResponseCodes.Invalid;
                }
            }
        }

        public bool RequestSuccessful => 200 <= _responseCode && _responseCode <= 299;

        public bool RequestFailed => 400 <= _responseCode && _responseCode <= 499;

        public string serverAddr;

        public int udpPort;

        public int tcpPort;

        public string sessionId;

        public string appId;

        public ServerType serverType;

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static MatchmakingResponse Deserialize(string data)
        {
            return JsonSerializer.Deserialize<MatchmakingResponse>(data);
        }

        public static MatchmakingResponse NotFound = new MatchmakingResponse{
            _responseCode = (int)ResponseCodes.NotFound
        };

        public static MatchmakingResponse ExceptionThrown = new MatchmakingResponse{
            _responseCode = (int)ResponseCodes.ExceptionThrow,
        };

        public static MatchmakingResponse RequestRejected = new MatchmakingResponse{
            _responseCode = (int)ResponseCodes.RequestRejected
        };
    }
}