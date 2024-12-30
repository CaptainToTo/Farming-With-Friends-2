using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree;

namespace OwlTree.Unity
{
public abstract class NetworkBehaviour : MonoBehaviour
{
    public NetworkGameObject NetObject { get {
        if (_netObj == null)
            _netObj = GetComponent<NetworkGameObject>();
        return _netObj;
    }}
    private NetworkGameObject _netObj = null;

    public UnityConnection Connection => NetObject?.Connection;

    public virtual void OnSpawn() { }

    public virtual void OnDespawn() { }
}

}
