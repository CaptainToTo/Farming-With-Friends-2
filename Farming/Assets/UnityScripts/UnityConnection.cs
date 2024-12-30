using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree;
using UnityEngine.Events;
using System;

namespace OwlTree.Unity
{

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

    [SerializeField] private ConnectionArgs _args;

    public UnityEvent<UnityConnection> OnAwake;

    public UnityEvent<ClientId> OnReady;
    public UnityEvent<ClientId> OnClientConnected;
    public UnityEvent<ClientId> OnClientDisconnected;
    public UnityEvent<ClientId> OnLocalDisconnect;
    public UnityEvent<ClientId> OnHostMigration;
    
    public UnityEvent<NetworkObject> OnObjectSpawn;
    public UnityEvent<NetworkObject> OnObjectDespawn;

    public UnityEvent<NetworkGameObject> OnGameObjectSpawn;
    public UnityEvent<NetworkGameObject> OnGameObjectDespawn;

    public bool IsActive => Connection.IsActive;

    public bool IsReady => Connection.IsReady && _spawner != null;

    void Awake()
    {
        var args = new Connection.Args{
            role = _args.isClient ? Connection.Role.Client : Connection.Role.Server,
            appId = _args.appId,
            serverAddr = _args.serverAddr,
            tcpPort = _args.tcpPort,
            serverUdpPort = _args.udpPort,
            maxClients = _args.maxClients,
            hostAddr = _args.hostAddr,
            connectionRequestRate = _args.connectionRequestRate,
            connectionRequestLimit = _args.connectionRequestLimit,
            connectionRequestTimeout = _args.connectionRequestTimeout,
            bufferSize = _args.bufferSize,
            useCompression = _args.useCompression,
            threaded = _args.threaded,
            threadUpdateDelta = _args.threadUpdateDelta,
            printer = (str) => Debug.Log(str),
            verbosity = Logger.Includes().ClientEvents().Exceptions().AllRpcProtocols().AllTypeIds()
        };

        Connection = new Connection(args);

        Connection.OnClientConnected += (id) => OnClientConnected.Invoke(id);
        Connection.OnClientDisconnected += (id) => OnClientDisconnected.Invoke(id);
        Connection.OnLocalDisconnect += (id) => {
            OnLocalDisconnect.Invoke(id);
            _spawner.DespawnAll();
            Destroy(gameObject);
        };
        Connection.OnHostMigration += (id) => OnHostMigration.Invoke(id);
        Connection.OnObjectSpawn += (id) => OnObjectSpawn.Invoke(id);
        Connection.OnObjectDespawn += (id) => OnObjectDespawn.Invoke(id);

        StartCoroutine(WaitForReady());

        _instances.Add(this);

        OnAwake?.Invoke(this);
    }

    private IEnumerator WaitForReady()
    {
        while (!Connection.IsReady)
            yield return null;

        if (Connection.IsAuthority)
        {
            _spawner = Connection.Spawn<PrefabSpawner>();
            _spawner.Initialize(this, _args.prefabs);
        }

        while (_spawner == null)
        {
            foreach (var s in PrefabSpawner.Instances)
            {
                Debug.Log("searching for spawner");
                if (s.Connection == Connection)
                {
                    _spawner = s;
                    _spawner.Initialize(this, _args.prefabs);
                    break;
                }
            }
            yield return null;
        }
        OnReady?.Invoke(Connection.LocalId);
    }

    void OnDestroy()
    {
        if (Connection.IsActive)
            Connection.Disconnect();
        _instances.Remove(this);
    }

    void FixedUpdate()
    {
        Connection.ExecuteQueue();
    }

    private PrefabSpawner _spawner = null;

    public NetworkGameObject Spawn(GameObject prefab)
    {
        if (!Connection.IsAuthority)
            throw new InvalidOperationException("Non-authority clients cannot spawn new objects.");
        return _spawner.Spawn(prefab);
    }

    public void Despawn(NetworkGameObject target)
    {
        if (!Connection.IsAuthority)
            throw new InvalidOperationException("Non-authority clients cannot despawn objects.");
        _spawner.Despawn(target);
    }
}

}

