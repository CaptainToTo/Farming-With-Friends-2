using System;
using System.Collections.Generic;
using OwlTree;
using UnityEngine;

namespace OwlTree.Unity
{
    public class PrefabSpawner : NetworkObject
    {
        private static List<PrefabSpawner> _instances = new();

        public static IEnumerable<PrefabSpawner> Instances => _instances;

        public override void OnSpawn()
        {
            _instances.Add(this);
            Connection.Maps.AddMap<GameObjectId, NetworkGameObject>();
        }

        public override void OnDespawn()
        {
            _instances.Remove(this);
        }

        public void Initialize(UnityConnection connection, IEnumerable<GameObject> prefabs)
        {
            _connection = connection;

            var curId = PrefabId.FirstPrefabId;
            foreach (var prefab in prefabs)
            {
                _prefabs.Add(new PrefabId(curId), prefab);
                curId++;
            }
            Initialized = true;

            if (!Connection.IsAuthority)
                RequestObjects();
        }

        public bool Initialized { get; private set; } = false;
        private Dictionary<PrefabId, GameObject> _prefabs = new();

        public IEnumerable<GameObject> Prefabs => _prefabs.Values;

        public IEnumerable<NetworkGameObject> Objects => Connection.Maps.GetValues<GameObjectId, NetworkGameObject>();

        private bool TryGetPrefabId(GameObject prefab, out PrefabId id)
        {
            foreach (var pair in _prefabs)
            {
                if (pair.Value == prefab)
                {
                    id = pair.Key;
                    return true;
                }
            }
            id = PrefabId.None;
            return false;
        }

        public bool TryGetObject(GameObjectId id, out NetworkGameObject obj)
        {
            return Connection.Maps.TryGet(id, out obj);
        }

        public NetworkGameObject GetGameObject(GameObjectId id)
        {
            if (!Connection.Maps.TryGet(id, out NetworkGameObject obj))
                return null;
            return obj;
        }

        private UnityConnection _connection;

        public Action<NetworkGameObject> OnObjectSpawn;

        public Action<NetworkGameObject> OnObjectDespawn;

        private uint _curId = GameObjectId.FirstGameObjectId;
        private GameObjectId NextGameObjectId()
        {
            var id = new GameObjectId(_curId);
            _curId++;
            return id;
        }

        public NetworkGameObject Spawn(GameObject prefab)
        {
            if (!TryGetPrefabId(prefab, out var id))
                throw new ArgumentException($"Prefab '{prefab.name}' is not assigned a prefab id. Make sure this prefab is in the prefab list.");
            
            var obj = GameObject.Instantiate(prefab);

            if (!obj.TryGetComponent<NetworkGameObject>(out var netObj))
                netObj = obj.AddComponent<NetworkGameObject>();

            netObj.Id = NextGameObjectId();
            netObj.Prefab = id;
            netObj.Connection = _connection;
            Connection.Maps.Add(netObj.Id, netObj);

            SendSpawn(id, netObj.Id);

            netObj.InvokeOnSpawn();

            return netObj;
        }

        [Rpc(RpcPerms.AuthorityToClients)]
        public virtual void SendSpawn(PrefabId id, GameObjectId assignedId)
        {
            if (!Initialized)
                return;

            if (!_prefabs.TryGetValue(id, out var prefab))
                throw new ArgumentException($"prefab id {id} is not assigned to a prefab.");
            
            var obj = GameObject.Instantiate(prefab);

            if (!obj.TryGetComponent<NetworkGameObject>(out var netObj))
                netObj = obj.AddComponent<NetworkGameObject>();
            
            netObj.Id = assignedId;
            netObj.Prefab = id;
            netObj.Connection = _connection;
            Connection.Maps.Add(netObj.Id, netObj);
            netObj.InvokeOnSpawn();
        }

        public void SendNetworkObjects(ClientId callee)
        {
            foreach (var obj in Objects)
                SendSpawnTo(callee, obj.Prefab, obj.Id);
        }

        [Rpc(RpcPerms.ClientsToAuthority)]
        public virtual void RequestObjects([CallerId] ClientId caller = default)
        {
            SendNetworkObjects(caller);
        }

        [Rpc(RpcPerms.AuthorityToClients)]
        public virtual void SendSpawnTo([CalleeId] ClientId callee, PrefabId id, GameObjectId assignedId)
        {
            if (!Initialized)
                return;
            
            if (!_prefabs.TryGetValue(id, out var prefab))
                throw new ArgumentException($"prefab id {id} is not assigned to a prefab.");
            if (Connection.Maps.HasKey(assignedId))
                return;
            
            var obj = GameObject.Instantiate(prefab);

            if (!obj.TryGetComponent<NetworkGameObject>(out var netObj))
                netObj = obj.AddComponent<NetworkGameObject>();
            
            netObj.Id = assignedId;
            netObj.Connection = _connection;
            Connection.Maps.Add(netObj.Id, netObj);
            netObj.InvokeOnSpawn();
        }

        public void Despawn(NetworkGameObject target)
        {
            Connection.Maps.Remove(target.Id);
            target.InvokeOnDespawn();
            OnObjectDespawn?.Invoke(target);
            target.Connection = null;
            SendDespawn(target.Id);
            GameObject.Destroy(target.gameObject);
        }

        [Rpc(RpcPerms.AuthorityToClients)]
        public virtual void SendDespawn(GameObjectId id)
        {
            if (!Connection.Maps.TryGet(id, out NetworkGameObject obj))
                throw new ArgumentException($"no network game object has the id {id}.");
            Connection.Maps.Remove(id);
            obj.InvokeOnDespawn();
            OnObjectDespawn?.Invoke(obj);
            obj.Connection = null;
            GameObject.Destroy(obj.gameObject);
        }

        public void DespawnAll()
        {
            foreach (var obj in Objects)
            {
                obj.OnDespawn.Invoke(obj);
                OnObjectDespawn?.Invoke(obj);
                obj.Connection = null;
                if (obj != null)
                    GameObject.Destroy(obj.gameObject);
            }
            Connection.Maps.Clear<GameObjectId, NetworkGameObject>();
        }
    }
}
