using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree;
using System.Linq;

namespace OwlTree.Unity
{
    
public class NetworkTransform : MonoBehaviour
{
    public UnityConnection Connection { get; private set; } = null;
    
    public bool IsActive => _netcode?.IsActive ?? false;

    private TransformNetcode _netcode = null;

    public void Initialize(UnityConnection connection)
    {
        
    }
}

public class TransformNetcode : NetworkObject
{

}

}
