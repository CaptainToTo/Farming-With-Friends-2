using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Unity;
using OwlTree;
using TMPro;

public class ConnectionCallbacks : MonoBehaviour
{
    private UnityConnection connection;
    [SerializeField] private GameObject managersPrefab;

    [SerializeField] private TextMeshProUGUI sessionText;

    public void OnAwake(UnityConnection connection)
    {
        this.connection = connection;
    }

    public void OnReady(ClientId localId)
    {
        if (connection.IsAuthority)
        {
            connection.Spawn(managersPrefab);
        }
        sessionText.text = $"Session Id: {connection.SessionId.Id}\nApp Id: {connection.AppId.Id}\n{localId} [{(connection.IsAuthority ? "host" : "client")}]";
    }
}
