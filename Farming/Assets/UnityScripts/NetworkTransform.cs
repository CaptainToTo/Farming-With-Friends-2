using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OwlTree.Unity
{
    public class NetworkTransform : NetworkBehaviour
    {
        internal TransformNetcode netcode = null;

        public ClientId Authority => netcode.Authority;

        public void SetAuthority(ClientId authority)
        {
            if (Connection.Connection.IsAuthority)
                netcode.SetAuthority(authority);
        }

        public Vector3 offset;

        public override void OnSpawn()
        {
            if (Connection.Connection.IsAuthority)
            {
                netcode = Connection.Connection.Spawn<TransformNetcode>();
                netcode.transform = this;
            }
        }

        void FixedUpdate()
        {
            if (netcode == null || Connection.Connection.LocalId != netcode.Authority)
                return;

            var pos = new NetworkVec3(transform.localPosition.x + offset.x, transform.localPosition.y + offset.y, transform.localPosition.z + offset.z);
            var rot = new NetworkVec4(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w);
            var scale = new NetworkVec3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            netcode.SendTransform(pos, rot, scale);
        }
    }

    public class TransformNetcode : NetworkObject
    {
        internal NetworkTransform transform = null;

        public ClientId Authority { get; private set; }

        public override void OnSpawn()
        {
            Authority = Connection.Authority;
            if (!Connection.IsAuthority)
                RequestTransform();
        }

        [Rpc(RpcCaller.Server, InvokeOnCaller = true)]
        public virtual void SetAuthority(ClientId authority)
        {
            Authority = authority;
        }

        [Rpc(RpcCaller.Client)]
        public virtual void RequestTransform([RpcCaller] ClientId caller = default)
        {
            CacheTransform(caller, transform.NetObject.Id);
        }

        [Rpc(RpcCaller.Server)]
        public virtual void CacheTransform([RpcCallee] ClientId callee, GameObjectId id)
        {
            Connection.WaitForObject<GameObjectId, NetworkGameObject>(id, (obj) => {
                transform = obj.GetComponent<NetworkTransform>();
                transform.netcode = this;
            });
        }

        [Rpc(RpcCaller.Any, RpcProtocol = Protocol.Udp)]
        public virtual void SendTransform(NetworkVec3 pos, NetworkVec4 rot, NetworkVec3 scale, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
                return;

            if (transform == null)
                return;

            transform.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
            transform.transform.localRotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);
            transform.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
        }
    }
}
