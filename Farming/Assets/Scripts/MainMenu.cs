using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Matchmaking.Unity;
using System.Threading.Tasks;
using TMPro;
using OwlTree.Unity;
using OwlTree;
using UnityEngine.SceneManagement;

// use OwlTree's matchmaking service API to handle hosting and joining sessions
public class MainMenu : MonoBehaviour
{
    public bool Waiting { get; private set; } = false;

    [Tooltip("The URL matchmaking requests will be made to.")]
    [SerializeField] string domainName = "http://127.0.0.1:5000";

    // the API client request will be made with
    MatchmakingClient client;

    void Awake()
    {
        client = new(domainName);
    }

    public void OnHost()
    {
        if (Waiting) return;
        var request = new MatchmakingRequest{
            appId = "FarmingWithFriends_OwlTreeExample",
            sessionId = Random.Range(100, 1000).ToString(),
            serverType = ServerType.Relay,
            clientRole = ClientRole.Host,
            maxClients = 6,
            migratable = true,
            owlTreeVersion = 1,
            appVersion = 1
        };
        var response = client.MakeRequest(request);
        Debug.Log("sent request: " + request.Serialize());
        StartCoroutine(WaitForResponse(response));
    }

    [SerializeField] private TMP_InputField idField;

    public void OnJoin()
    {
        if (Waiting) return;
        var sessionId = idField.text;
        var request = new MatchmakingRequest{
            appId = "FarmingWithFriends_OwlTreeExample",
            sessionId = sessionId,
            serverType = ServerType.Relay,
            clientRole = ClientRole.Client,
            maxClients = 6,
            migratable = true,
            owlTreeVersion = 1,
            appVersion = 1
        };
        var response = client.MakeRequest(request);
        Debug.Log("sent request: " + request.Serialize());
        StartCoroutine(WaitForResponse(response));
    }

    [Tooltip("The prefab of the UnityConnection that will be used to create a new connection.")]
    [SerializeField] UnityConnection connectionPrefab;
    [Tooltip("The connection args that will be provided to the connection.")]
    [SerializeField] ConnectionArgs unityArgs;

    private IEnumerator WaitForResponse(Task<MatchmakingResponse> response)
    {
        Waiting = true;
        while (!response.IsCompleted)
            yield return null;
        
        var val = response.Result;

        // if matchmaking request was successful, create a new connection
        if (val.RequestSuccessful)
        {
            Debug.Log("got response: " + val.Serialize());
            var connection = Instantiate(connectionPrefab);
            var args = unityArgs.GetArgs();
            args.appId = val.appId;
            args.sessionId = val.sessionId;
            args.serverAddr = val.serverAddr;
            args.tcpPort = val.tcpPort;
            args.udpPort = val.udpPort;
            args.role = NetRole.Client;

            connection.Connect(args);

            SceneManager.LoadScene("Farm");
        }
        else
        {
            Debug.Log("Request Failed: " + val.Serialize());
        }

        Waiting = false;
    }
}
