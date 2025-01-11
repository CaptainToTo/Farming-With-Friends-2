
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OwlTree.Matchmaking
{
    public class MatchmakingEndpoint
    {
        public delegate MatchmakingResponse ProcessRequest(MatchmakingRequest request);

        private HttpListener _listener;
        private ProcessRequest _callback;

        public MatchmakingEndpoint(string domain, ProcessRequest processRequest)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(domain);
            _callback = processRequest;
        }

        public MatchmakingEndpoint(IEnumerable<string> domains, ProcessRequest processRequest)
        {
            _listener = new HttpListener();
            foreach (var domain in domains)
                _listener.Prefixes.Add(domain);
            _callback = processRequest;
        }

        public bool IsActive { get; private set; } = false;

        public async void Start()
        {
            _listener.Start();
            IsActive = true;

            while (IsActive)
            {
                var context = await _listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                if (request.Url?.AbsolutePath == "/matchmaking")
                {
                    try
                    {
                        string requestBody = new StreamReader(request.InputStream, Encoding.UTF8).ReadToEnd();
                        var requestObj = MatchmakingRequest.Deserialize(requestBody);
                        var responseObj = _callback.Invoke(requestObj);
                        string responseBody = responseObj.Serialize();
                        response.StatusCode = (int)responseObj.ResponseCode;
                        byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        response.StatusCode = (int)ResponseCodes.RequestRejected;
                    }
                }
                else
                {
                    response.StatusCode = (int)ResponseCodes.NotFound;
                }
                response.OutputStream.Close();
            }

            _listener.Close();
        }

        public void Close()
        {
            IsActive = false;
        }
    }
}