using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree;
using UnityEngine.Events;

/// <summary>
/// Thin wrapper around an OwlTree Connection instance that exposes the API,
/// and hooks into Unity's runtime.
/// </summary>
public class UnityConnection : MonoBehaviour
{
    private static List<UnityConnection> _instances = new();

    /// <summary>
    /// Iterable of all unity connections current available.
    /// </summary>
    public static IEnumerable<UnityConnection> Instances => _instances;

    public Connection Connection { get; private set; } = null;

    public UnityEvent<ClientId> OnReady;
    public UnityEvent<ClientId> OnClientConnected;
    public UnityEvent<ClientId> OnClientDisconnected;
    public UnityEvent<ClientId> OnLocalDisconnect;
    public UnityEvent<ClientId> OnHostMigration;
    
    public UnityEvent<NetworkObject> OnObjectSpawn;
    public UnityEvent<NetworkObject> OnObjectDespawn;

    void Awake()
    {
        Connection = new Connection( new Connection.Args{
            role = Connection.Role.Client,
            appId = "FarmingWithFriends_OwlTreeExample",
            printer = (str) => Debug.Log(str),
            verbosity = OwlTree.Logger.Includes().ClientEvents().Exceptions()
        });

        Connection.OnReady += (id) => OnReady.Invoke(id);
        Connection.OnClientConnected += (id) => OnClientConnected.Invoke(id);
        Connection.OnClientDisconnected += (id) => OnClientDisconnected.Invoke(id);
        Connection.OnLocalDisconnect += (id) => OnLocalDisconnect.Invoke(id);
        Connection.OnHostMigration += (id) => OnHostMigration.Invoke(id);
        Connection.OnObjectSpawn += (id) => OnObjectSpawn.Invoke(id);
        Connection.OnObjectDespawn += (id) => OnObjectDespawn.Invoke(id);

        _instances.Add(this);
    }

    void OnDestroy()
    {
        Connection.Disconnect();
        _instances.Remove(this);
    }
}
