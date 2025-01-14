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
            if (Connection.IsAuthority)
                netcode.SetAuthority(authority);
        }

        public Vector3 offset;

        public override void OnSpawn()
        {
            if (Connection.IsAuthority)
            {
                netcode = Connection.Spawn<TransformNetcode>();
                netcode.transform = this;
            }
        }

        [SerializeField] private bool _interpolate = false;
        public bool Interpolate => _interpolate;

        [SerializeField] private bool _continuousSync = false;

        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _syncScale = false;

        [HideInInspector] public Vector3 nextPos;
        [HideInInspector] public Quaternion nextRot;
        [HideInInspector] public Vector3 nextScale;

        void FixedUpdate()
        {
            if (netcode == null)
                return;

            if (Connection.LocalId == netcode.Authority)
            {
                var pos = new NetworkVec3(transform.localPosition.x + offset.x, transform.localPosition.y + offset.y, transform.localPosition.z + offset.z);
                var rot = new NetworkVec4(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w);
                var scale = new NetworkVec3(transform.localScale.x, transform.localScale.y, transform.localScale.z);

                if (_continuousSync || (nextPos - transform.localPosition).magnitude > 0.001f)
                    netcode.SendPosition(pos);
                if (_syncRotation && (_continuousSync || (nextRot.eulerAngles - transform.eulerAngles).magnitude > 0.001f))
                    netcode.SendRotation(rot);
                if (_syncScale && (_continuousSync || (nextScale - transform.localScale).magnitude > 0.001f))
                    netcode.SendScale(scale);
                
                nextPos = transform.localPosition;
                nextRot = transform.localRotation;
                nextScale = transform.localScale;
            }
            else if (_interpolate)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, nextPos, 0.5f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, nextRot, 0.7f);
            }
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
            CacheTransform(caller, transform.NetObject.Id, Authority);
        }

        [Rpc(RpcCaller.Server)]
        public virtual void CacheTransform([RpcCallee] ClientId callee, GameObjectId id, ClientId authority)
        {
            Authority = authority;
            Connection.WaitForObject<GameObjectId, NetworkGameObject>(id, (obj) => {
                transform = obj.GetComponent<NetworkTransform>();
                transform.netcode = this;
                RequestState(Authority);
            });
        }

        [Rpc(RpcCaller.Any)]
        public virtual void RequestState([RpcCallee] ClientId callee, [RpcCaller] ClientId caller = default)
        {
            if (Connection.LocalId != Authority)
                return;

            var pos = new NetworkVec3(
                transform.transform.localPosition.x + transform.offset.x, 
                transform.transform.localPosition.y + transform.offset.y, 
                transform.transform.localPosition.z + transform.offset.z);
            var rot = new NetworkVec4(
                transform.transform.localRotation.x, 
                transform.transform.localRotation.y, 
                transform.transform.localRotation.z, 
                transform.transform.localRotation.w);
            var scale = new NetworkVec3(
                transform.transform.localScale.x, 
                transform.transform.localScale.y, 
                transform.transform.localScale.z);
            
            SendState(caller, pos, rot, scale);
        }

        [Rpc(RpcCaller.Any)]
        public virtual void SendState([RpcCallee] ClientId callee, NetworkVec3 pos, NetworkVec4 rot, NetworkVec3 scale, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
                return;

            transform.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
            transform.nextPos = transform.transform.localPosition;

            transform.transform.localRotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);
            transform.nextRot = transform.transform.localRotation;

            transform.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
        }

        [Rpc(RpcCaller.Any, RpcProtocol = Protocol.Udp)]
        public virtual void SendPosition(NetworkVec3 pos, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
                return;

            if (transform == null)
                return;
            
            if (transform.Interpolate)
                transform.nextPos = new Vector3(pos.x, pos.y, pos.z);
            else
                transform.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
        }

        [Rpc(RpcCaller.Any, RpcProtocol = Protocol.Udp)]
        public virtual void SendRotation(NetworkVec4 rot, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
                return;

            if (transform == null)
                return;

            if (transform.Interpolate)
                transform.nextRot = new Quaternion(rot.x, rot.y, rot.z, rot.w);
            else
                transform.transform.localRotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);
        }

        [Rpc(RpcCaller.Any, RpcProtocol = Protocol.Udp)]
        public virtual void SendScale(NetworkVec3 scale, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
                return;

            if (transform == null)
                return;
            
            transform.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
        }
    }
}
