using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Matchmaking.Unity;
using System.Threading.Tasks;
using TMPro;
using OwlTree.Unity;
using OwlTree;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public bool Waiting { get; private set; } = false;

    [SerializeField] string domainName = "http://127.0.0.1:5000";

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

    [SerializeField] UnityConnection connectionPrefab;
    [SerializeField] ConnectionArgs unityArgs;

    private IEnumerator WaitForResponse(Task<MatchmakingResponse> response)
    {
        Waiting = true;
        while (!response.IsCompleted)
            yield return null;
        
        var val = response.Result;

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
