using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Unity;
using OwlTree;
using UnityEngine.Events;

public class Crop : NetworkBehaviour
{
    [SerializeField] float growthSpeed = 0.5f;
    [SerializeField] float maxGrowth = 20f;
    [SerializeField] float startScale = 0.1f;
    [SerializeField] float maxScale = 0.5f;

    public CropNetcode netcode = null;
    public NetworkVec3 NetPos => new NetworkVec3(transform.position.x, transform.position.y, transform.position.z);

    public Renderer Rend {
        get {
            if (_rend == null)
                _rend = GetComponent<Renderer>();
            return _rend;
        }
    }
    private Renderer _rend = null;

    public float Growth {
        get => _growth;
        set => UpdateGrowth(value);
    }
    private float _growth = 0f;

    public UnityEvent<Crop> OnGrowth;

    private void UpdateGrowth(float growth)
    {
        var prevGrowth = _growth;
        _growth = Mathf.Clamp(growth, 0, maxGrowth);

        if (_growth - prevGrowth < 0.001f)
            return;

        float progress = _growth / maxGrowth;
        transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one * maxScale, progress);
        Rend.material.color = Color.Lerp(Color.yellow, Color.green, progress);
        OnGrowth.Invoke(this);
    }

    public override void OnSpawn()
    {
        if (Connection.IsAuthority)
        {
            netcode = Connection.Spawn<CropNetcode>();
            netcode.crop = this;

            OnGrowth.AddListener((crop) => netcode.SendGrowth(_growth));
        }
    }

    void FixedUpdate()
    {
        UpdateGrowth(_growth + (Time.fixedDeltaTime * growthSpeed));
    }
}

public class CropNetcode : NetworkObject
{
    public Crop crop = null;

    public override void OnSpawn()
    {
        if (!Connection.IsAuthority)
            RequestCrop();
    }

    [Rpc(RpcCaller.Client)]
    public virtual void RequestCrop([RpcCaller] ClientId caller = default)
    {
        SendCropId(caller, crop.NetObject.Id, crop.Growth);
    }

    [Rpc(RpcCaller.Server)]
    public virtual void SendCropId([RpcCallee] ClientId callee, GameObjectId id, float growth)
    {
        Connection.WaitForObject<GameObjectId, NetworkGameObject>(id, (obj) => {
            crop = obj.GetComponent<Crop>();
            crop.netcode = this;
            crop.Growth = growth;
        });
    }

    [Rpc(RpcCaller.Server, RpcProtocol = Protocol.Udp)]
    public virtual void SendGrowth(float growth)
    {
        if (crop == null)
            return;
        crop.Growth = growth;
    }
}
