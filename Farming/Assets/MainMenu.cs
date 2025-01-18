using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Matchmaking;
using System.Threading.Tasks;
using TMPro;
using OwlTree.Unity;
using OwlTree;

public class MainMenu : MonoBehaviour
{
    public bool Waiting { get; private set; } = false;

    [SerializeField] string domainName = "http://localhost:5000";

    MatchmakingClient client;

    void Awake()
    {
        client = new(domainName);
    }

    public void OnHost()
    {
        if (Waiting) return;
        var response = client.MakeRequest(new MatchmakingRequest{
            appId = "FarmingWithFriends_OwlTreeExample",
            sessionId = Random.Range(100, 1000).ToString(),
            serverType = ServerType.Relay,
            clientRole = ClientRole.Host,
            maxClients = 6,
            migratable = true,
            owlTreeVersion = 1,
            appVersion = 1
        });
        StartCoroutine(WaitForResponse(response));
    }

    [SerializeField] private TMP_InputField idField;

    public void OnJoin()
    {
        if (Waiting) return;
        var sessionId = idField.text;
        var response = client.MakeRequest(new MatchmakingRequest{
            appId = "FarmingWithFriends_OwlTreeExample",
            sessionId = sessionId,
            serverType = ServerType.Relay,
            clientRole = ClientRole.Client,
            maxClients = 6,
            migratable = true,
            owlTreeVersion = 1,
            appVersion = 1
        });
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
            var connection = Instantiate(connectionPrefab);
            var args = unityArgs.GetArgs();
            args.appId = val.appId;
            args.sessionId = val.sessionId;
            args.serverAddr = val.serverAddr;
            args.tcpPort = val.tcpPort;
            args.udpPort = val.udpPort;
            args.role = NetRole.Client;

            connection.Connect(args);
        }

        Waiting = false;
    }
}
