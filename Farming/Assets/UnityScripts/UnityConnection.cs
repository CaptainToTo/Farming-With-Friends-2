using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree;
using UnityEngine.Events;
using System;

namespace OwlTree.Unity
{

/// <summary>
/// Thin wrapper around an OwlTree Connection instance
/// that hooks into Unity's runtime.
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

    [HideInInspector] public UnityEvent<Bandwidth> OnBandwidthReport;

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
            measureBandwidth = _args.measureBandwidth,
            bandwidthReporter = (b) => OnBandwidthReport.Invoke(b),
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
            _spawner?.DespawnAll();
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

    public IEnumerable<NetworkGameObject> GameObjects => _spawner?.Objects;

    public NetworkGameObject Spawn(GameObject prefab)
    {
        if (!Connection.IsAuthority)
            throw new InvalidOperationException("Non-authority clients cannot spawn new objects.");
        var spawned = _spawner.Spawn(prefab);
        return spawned;
    }

    public void Despawn(NetworkGameObject target)
    {
        if (!Connection.IsAuthority)
            throw new InvalidOperationException("Non-authority clients cannot despawn objects.");
        _spawner.Despawn(target);
    }

    public NetworkGameObject GetGameObject(GameObjectId id) => _spawner.GetGameObject(id);

    public bool TryGetObject(GameObjectId id, out NetworkGameObject obj) => _spawner.TryGetObject(id, out obj);

    public RpcProtocols Protocols => Connection.Protocols;

    public void Log(string message) => Connection.Log(message);

    public Bandwidth Bandwidth => Connection.Bandwidth;

    public bool Threaded => Connection.Threaded;

    public Connection.Role NetRole => Connection.NetRole;

    public bool IsServer => Connection.IsServer;

    public bool IsClient => Connection.IsClient;

    public bool IsHost => Connection.IsHost;

    public bool IsRelay => Connection.IsRelay;

    public int ClientCount => Connection.ClientCount;

    public IEnumerable<ClientId> Clients => Connection.Clients;

    public bool ContainsClient(ClientId id) => Connection.ContainsClient(id);

    public ClientId LocalId => Connection.LocalId;

    public ClientId Authority => Connection.Authority;

    public bool IsAuthority => Connection.IsAuthority;

    public bool Migratable => Connection.Migratable;

    public void Read() => Connection.Read();

    public void AwaitConnection() => Connection.AwaitConnection();

    public void ExecuteQueue() => Connection.ExecuteQueue();

    public void Send() => Connection.Send();

    public PingRequest Ping(ClientId target) => Connection.Ping(target);

    public void Disconnect() => Connection.Disconnect();

    public void Disconnect(ClientId id) => Connection.Disconnect(id);

    public void MigrateHost(ClientId id) => Connection.MigrateHost(id);

    public IEnumerable<NetworkObject> NetworkObjects => Connection.NetworkObjects;

    public bool TryGetObject(NetworkId id, out NetworkObject obj) => Connection.TryGetObject(id, out obj);

    public NetworkObject GetNetworkObject(NetworkId id) => Connection.GetNetworkObject(id);

    public T Spawn<T>() where T : NetworkObject, new() => Connection.Spawn<T>();

    public NetworkObject Spawn(Type t) => Connection.Spawn(t);

    public void Despawn(NetworkObject target) => Connection.Despawn(target);

    public void AddObjectMap<K, V>() => Connection.AddObjectMap<K, V>();

    public void AddObjectToMap<K, V>(K key, V val) => Connection.AddObjectToMap(key, val);

    public bool TryGetObject<K, V>(K key, out V val) => Connection.TryGetObject(key, out val);

    public bool TryGetObject(Type k, object key, out object val) => Connection.TryGetObject(k, key, out val);

    public V GetObject<K, V>(K key) => Connection.GetObject<K, V>(key);

    public bool HasKey<K>(K key) => Connection.HasKey(key);

    public IEnumerable<V> GetObjects<K, V>() => Connection.GetObjects<K, V>();

    public void RemoveObject<K>(K key) => Connection.RemoveObject(key);

    public void ClearMap<K, V>() => Connection.ClearMap<K, V>();

    public void RemoveMap<K, V>() => Connection.RemoveMap<K, V>();

    public void WaitForObject(NetworkId id, Action<NetworkObject> callback) => Connection.WaitForObject(id, callback);

    public void WaitForObject<K, V>(K id, Action<V> callback) => Connection.WaitForObject(id, callback);
}

}

