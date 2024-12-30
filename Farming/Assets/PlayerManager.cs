using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OwlTree;
using OwlTree.StateMachine;
using OwlTree.Unity;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    private PlayerManagerNetcode _netcode = null;
    public void SetNetcode(PlayerManagerNetcode netcode)
    {
        _netcode = netcode;
    }

    public override void OnSpawn()
    {
        if (Connection.Connection.IsAuthority)
        {
            _netcode = Connection.Connection.Spawn<PlayerManagerNetcode>();
            _netcode.manager = this;
            Connection.OnClientConnected.AddListener(SpawnPlayer);
            Connection.OnClientDisconnected.AddListener(DespawnPlayer);
            SpawnPlayer(Connection.Connection.LocalId);
        }
        transform.position = Connection.transform.position;
    }

    public override void OnDespawn()
    {
        Connection.Connection.Despawn(_netcode);
        Connection.OnClientConnected.RemoveListener(SpawnPlayer);
        Connection.OnClientDisconnected.RemoveListener(DespawnPlayer);
    }

    [SerializeField] private GameObject _playerPrefab;

    private Dictionary<ClientId, Player> _players = new();

    public IEnumerable<KeyValuePair<ClientId, Player>> Players => _players;

    private void SpawnPlayer(ClientId player)
    {
        if (Connection.Connection.IsAuthority)
        {
            var playerObj = Connection.Spawn(_playerPrefab);
            var stateMachine = Connection.Connection.Spawn<NetworkStateMachine>();
            _netcode.AttachPlayerNetcode(player, playerObj.Id, stateMachine.Id);
        }
    }

    public void CachePlayer(ClientId id, Player player)
    {
        _players.Add(id, player);
        player.transform.position = transform.position;
    }

    public bool HasPlayer(ClientId id) => _players.ContainsKey(id);

    private void DespawnPlayer(ClientId player)
    {
        if (Connection.Connection.IsAuthority)
        {
            Connection.Connection.Despawn(_players[player].netcode);
            Connection.Despawn(_players[player].GetComponent<NetworkGameObject>());
        }
    }
}

public class PlayerManagerNetcode : NetworkObject
{
    public PlayerManager manager;

    public override void OnSpawn()
    {
        if (!Connection.IsAuthority)
            RequestManagerId(Connection.LocalId);
    }

    [Rpc(RpcCaller.Client)]
    public virtual void RequestManagerId([RpcCaller] ClientId caller = default)
    {
        AttachToManager(caller, manager.NetObject.Id);
        foreach (var pair in manager.Players)
        {
            AttachPlayerNetcode(pair.Key, pair.Value.NetObject.Id, pair.Value.netcode.Id);
        }
    }

    [Rpc(RpcCaller.Server)]
    public virtual void AttachToManager([RpcCallee] ClientId callee, GameObjectId id)
    {
        if (Connection.TryGetObject(id, out NetworkGameObject obj))
        {
            manager = obj.GetComponent<PlayerManager>();
            manager.SetNetcode(this);
        }
    }

    [Rpc(RpcCaller.Server, InvokeOnCaller = true)]
    public virtual void AttachPlayerNetcode(ClientId player, GameObjectId id, NetworkId netcodeId)
    {
        if (manager.HasPlayer(player))
            return;
        var netcode = (NetworkStateMachine)Connection.GetNetworkObject(netcodeId);
        Connection.TryGetObject(id, out NetworkGameObject obj);
        var playerObj = obj.GetComponent<Player>();
        playerObj.SetNetcode(netcode);
        manager.CachePlayer(player, playerObj);
    }
}
