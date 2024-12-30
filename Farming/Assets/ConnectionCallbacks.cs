using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Unity;
using OwlTree;
using TMPro;

public class ConnectionCallbacks : MonoBehaviour
{
    private UnityConnection connection;
    [SerializeField] private GameObject playerManagerPrefab;

    [SerializeField] private TextMeshProUGUI roleText;

    public void OnAwake(UnityConnection connection)
    {
        this.connection = connection;
    }

    public void OnReady(ClientId localId)
    {
        if (connection.Connection.IsAuthority)
        {
            connection.Spawn(playerManagerPrefab);
            Debug.Log("set role text to host");
            roleText.text = "host";
        }
        else
        {
            Debug.Log("set role text to client");
            roleText.text = "client";
        }
    }
}
