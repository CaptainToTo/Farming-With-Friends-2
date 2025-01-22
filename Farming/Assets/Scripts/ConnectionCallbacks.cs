using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Unity;
using OwlTree;
using TMPro;

// subscribe to events from a connection's unity events
public class ConnectionCallbacks : MonoBehaviour
{
    private UnityConnection connection;
    [SerializeField] private GameObject managersPrefab;

    [SerializeField] private TextMeshProUGUI sessionText;

    public void OnStart(UnityConnection connection)
    {
        this.connection = connection;
    }

    public void OnReady(ClientId localId)
    {
        // if the authority init managers that will spawn all necessary game objects
        if (connection.IsAuthority)
            connection.Spawn(managersPrefab);
        
        // display session info
        sessionText.text = $"Session Id: {connection.SessionId.Id}\nApp Id: {connection.AppId.Id}\n{localId} [{(connection.IsAuthority ? "host" : "client")}]";
    }
}
