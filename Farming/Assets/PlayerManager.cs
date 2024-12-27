using System;
using System.Collections;
using System.Collections.Generic;
using OwlTree;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public class Netcode : NetworkObject
    {
        public override void OnSpawn()
        {
            Rpcs = this;
        }

        [Rpc]
        public virtual void CachePlayerNetcode(ClientId player, NetworkId id)
        {

        }
    }

    private static Netcode Rpcs = null;

    private UnityConnection _connection;
    public void CacheConnection(UnityConnection connection)
    {
        _connection = connection;
        _connection.OnReady.AddListener(InitManager);
    }

    private void InitManager(ClientId localId)
    {
        if (_connection.Connection.IsAuthority)
        {
            Rpcs = _connection.Connection.Spawn<Netcode>();
        }
    }

    private void CachePlayer(NetworkObject player)
    {
        throw new NotImplementedException();
    }

    [SerializeField] private Player _playerPrefab;

    private Dictionary<ClientId, Player> _players = new();

    private void SpawnPlayer(ClientId player)
    {

    }

    private void RemovePlayer(ClientId player)
    {

    }
}
