using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OwlTree;
using OwlTree.StateMachine;
using OwlTree.Unity;
using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : NetworkBehaviour
{
    public PlayerManagerNetcode netcode = null;

    public override void OnSpawn()
    {
        transform.position = Connection.transform.position;
        if (Connection.IsAuthority)
        {
            netcode = Connection.Spawn<PlayerManagerNetcode>();
            netcode.manager = this;
            Connection.OnClientConnected.AddListener(SpawnPlayer);
            foreach (var player in Connection.Clients)
                SpawnPlayer(player);
        }
        Connection.OnClientDisconnected.AddListener(DespawnPlayer);

        DontDestroyOnLoad(gameObject);
    }

    public override void OnDespawn()
    {
        Connection.Despawn(netcode);
        Connection.OnClientConnected.RemoveListener(SpawnPlayer);
        Connection.OnClientDisconnected.RemoveListener(DespawnPlayer);
    }

    [SerializeField] private GameObject _playerPrefab;

    private Dictionary<ClientId, Player> _players = new();

    public IEnumerable<KeyValuePair<ClientId, Player>> Players => _players;

    public UnityEvent<Player> OnNewPlayer;

    private void SpawnPlayer(ClientId player)
    {
        if (Connection.IsAuthority)
        {
            var playerObj = Connection.Spawn(_playerPrefab);
            var stateMachine = Connection.Spawn<NetworkStateMachine>();
            stateMachine.SetAuthority(player);
            playerObj.GetComponent<Player>().SetNetcode(stateMachine, player);
            playerObj.GetComponent<NetworkTransform>().SetAuthority(player);
            CachePlayer(player, playerObj.GetComponent<Player>());
            netcode.AttachPlayerNetcode(player, playerObj.Id, stateMachine.Id);
        }
    }

    public void CachePlayer(ClientId id, Player player)
    {
        _players.Add(id, player);
        OnNewPlayer.Invoke(player);
    }

    public bool HasPlayer(ClientId id) => _players.ContainsKey(id);

    private void DespawnPlayer(ClientId player)
    {
        if (Connection.IsAuthority)
        {
            Connection.Despawn(_players[player].netcode);
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

    [Rpc(RpcPerms.ClientsToAuthority)]
    public virtual void RequestManagerId([CallerId] ClientId caller = default)
    {
        AttachToManager(caller, manager.NetObject.Id);
    }

    [Rpc(RpcPerms.ClientsToAuthority)]
    public virtual void RequestPlayers([CallerId] ClientId caller = default)
    {
        foreach (var pair in manager.Players)
            AttachPlayerNetcode(pair.Key, pair.Value.NetObject.Id, pair.Value.netcode.Id);
    }

    [Rpc(RpcPerms.AuthorityToClients)]
    public virtual void AttachToManager([CalleeId] ClientId callee, GameObjectId id)
    {
        Connection.WaitForObject<GameObjectId, NetworkGameObject>(id, (obj) => {
            manager = obj.GetComponent<PlayerManager>();
            manager.netcode = this;
            RequestPlayers();
        });
    }

    [Rpc(RpcPerms.AuthorityToClients)]
    public virtual void AttachPlayerNetcode(ClientId player, GameObjectId id, NetworkId netcodeId)
    {
        Connection.WaitForObject(netcodeId, (netcodeObj) => {
            if (manager?.HasPlayer(player) ?? true)
                return;
            var netcode = (NetworkStateMachine)netcodeObj;
            Connection.Maps.TryGet(id, out NetworkGameObject obj);
            var playerObj = obj.GetComponent<Player>();
            playerObj.SetNetcode(netcode, player);
            manager.CachePlayer(player, playerObj);
        });
    }
}
