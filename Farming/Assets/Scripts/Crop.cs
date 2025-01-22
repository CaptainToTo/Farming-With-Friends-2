using UnityEngine;
using OwlTree.Unity;
using OwlTree;
using UnityEngine.Events;

// planted by players and grows over time
public class Crop : NetworkBehaviour
{
    [Tooltip("The rate at which the crop will grow.")]
    [SerializeField] float growthSpeed = 0.5f;
    [Tooltip("When the crop will stop growing.")]
    [SerializeField] float maxGrowth = 20f;
    [Tooltip("The starting scale the crop visual will start at. Lerps to maxScale as it grows.")]
    [SerializeField] float startScale = 0.1f;
    [Tooltip("The size the crop visual will be once the crop has completely grown.")]
    [SerializeField] float maxScale = 0.5f;

    public CropNetcode netcode = null;
    // easy getter to convert position to NetVec3
    public NetworkVec3 NetPos => new NetworkVec3(transform.position.x, transform.position.y, transform.position.z);

    public Renderer Rend {
        get {
            if (_rend == null)
                _rend = GetComponent<Renderer>();
            return _rend;
        }
    }
    private Renderer _rend = null;

    // the age of the crop, synchronized
    public float Growth {
        get => _growth;
        set => UpdateGrowth(value);
    }
    private float _growth = 0f;

    public UnityEvent<Crop> OnGrowth;

    // clamp growth, and update visuals based on value
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

            // only send state update if the growth changes
            OnGrowth.AddListener((crop) => netcode.SendGrowth(_growth));
        }
    }

    // network tick rate is on fixed update, so there's no reason to update this more frequently with update
    void FixedUpdate()
    {
        UpdateGrowth(_growth + (Time.fixedDeltaTime * growthSpeed));
    }
}

public class CropNetcode : NetworkObject
{
    // the crop this netcode is synching
    public Crop crop = null;

    public override void OnSpawn()
    {
        // if this is a client, request the game object id of the crop this netcode is assigned to
        if (!Connection.IsAuthority)
            RequestCrop();
    }

    [Rpc(RpcPerms.ClientsToAuthority)]
    public virtual void RequestCrop([CallerId] ClientId caller = default)
    {
        SendCropId(caller, crop.NetObject.Id, crop.Growth);
    }

    [Rpc(RpcPerms.AuthorityToClients)]
    public virtual void SendCropId([CalleeId] ClientId callee, GameObjectId id, float growth)
    {
        // wait for crop game object to exist, then cache it, and update its state
        Connection.WaitForObject<GameObjectId, NetworkGameObject>(id, (obj) => {
            crop = obj.GetComponent<Crop>();
            crop.netcode = this;
            crop.Growth = growth;
        });
    }

    [Rpc(RpcPerms.AuthorityToClients, RpcProtocol = Protocol.Udp)]
    public virtual void SendGrowth(float growth)
    {
        if (crop == null)
            return;
        crop.Growth = growth;
    }
}
