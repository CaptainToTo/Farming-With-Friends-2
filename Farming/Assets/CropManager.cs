using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.Unity;
using OwlTree;
using System;

public class CropManager : NetworkBehaviour
{
    public CropManagerNetcode netcode = null;

    public override void OnSpawn()
    {
        if (Connection.IsAuthority)
        {
            netcode = Connection.Spawn<CropManagerNetcode>();
            netcode.manager = this;
        }
        DontDestroyOnLoad(gameObject);
    }

    [SerializeField] GameObject cropPrefab;

    private List<Crop> _crops = new();

    public void AddCrop(Crop crop)
    {
        if (!_crops.Contains(crop))
            _crops.Add(crop);
    }

    public void RemoveCrop(Crop crop)
    {
        _crops.Remove(crop);
    }

    public IEnumerable<Crop> Crops => _crops;

    public void AttachToPlayer(Player player)
    {
        player.OnPlant.AddListener(Connection ? NetworkPlantCrop : PlantCrop);
        player.OnHarvest.AddListener(Connection ? NetworkHarvestCrop : HarvestCrop);
    }

    private void HarvestCrop(Player player, Crop crop)
    {
        _crops.Remove(crop);
        Destroy(crop.gameObject);
    }

    private void PlantCrop(Player player)
    {
        var cropObj = Instantiate(cropPrefab);
        cropObj.transform.position = player.transform.position + new Vector3(0, -0.5f, -1f);
        _crops.Add(cropObj.GetComponent<Crop>());
    }

    private void NetworkHarvestCrop(Player player, Crop crop)
    {
        if (Connection.IsAuthority)
            ReceiveHarvestRequest(player, crop);
        else
            netcode.RequestHarvest(crop.NetObject.Id, player.NetObject.Id);
    }

    public void ReceiveHarvestRequest(Player player, Crop crop)
    {
        _crops.Remove(crop);
        netcode.SendHarvest(crop.NetObject.Id);
        Connection.Despawn(crop.NetObject);
    }

    private void NetworkPlantCrop(Player player)
    {
        if (Connection.IsAuthority)
            ReceivePlantRequest(player);
        else
            netcode.RequestPlant(player.NetObject.Id);
    }

    public void ReceivePlantRequest(Player player)
    {
        var crop = Connection.Spawn(cropPrefab);
        crop.transform.position = player.transform.position + new Vector3(0, -0.5f, -1f);
        _crops.Add(crop.GetComponent<Crop>());
        netcode.SendPlant(crop.Id, player.NetObject.Id);
    }
}

public class CropManagerNetcode : NetworkObject
{
    public CropManager manager = null;

    public override void OnSpawn()
    {
        if (!Connection.IsAuthority)
            RequestManagerId();
    }

    [Rpc(RpcPerms.ClientsToAuthority)]
    public virtual void RequestManagerId([CallerId] ClientId caller = default)
    {
        SendManagerId(caller, manager.NetObject.Id);
    }
    
    [Rpc(RpcPerms.AuthorityToClients)]
    public virtual void SendManagerId([CalleeId] ClientId callee, GameObjectId id)
    {
        Connection.WaitForObject<GameObjectId, NetworkGameObject>(id, (obj) => {
            manager = obj.GetComponent<CropManager>();
            manager.netcode = this;
            RequestCrops();
        });
    }

    [Rpc(RpcPerms.ClientsToAuthority)]
    public virtual void RequestCrops([CallerId] ClientId caller = default)
    {
        foreach (var crop in manager.Crops)
            SendCrop(caller, crop.NetObject.Id, new NetworkVec3(crop.transform.position.x, crop.transform.position.y, crop.transform.position.z));
    }

    [Rpc(RpcPerms.AuthorityToClients)]
    public virtual void SendCrop([CalleeId] ClientId callee, GameObjectId cropId, NetworkVec3 pos)
    {
        Connection.WaitForObject<GameObjectId, NetworkGameObject>(cropId, (obj) => {
            manager.AddCrop(obj.GetComponent<Crop>());
            obj.transform.position = new Vector3(pos.x, pos.y, pos.z);
        });
    }

    [Rpc(RpcPerms.AuthorityToClients)]
    public virtual void SendPlant(GameObjectId cropId, GameObjectId playerId)
    {
        Connection.WaitForObject<GameObjectId, NetworkGameObject>(cropId, (obj) => {
            manager.AddCrop(obj.GetComponent<Crop>());
            obj.transform.position = Connection.Maps.Get<GameObjectId, NetworkGameObject>(playerId).transform.position +
                new Vector3(0, -0.5f, -1f);
        });
    }

    [Rpc(RpcPerms.ClientsToAuthority)]
    public virtual void RequestPlant(GameObjectId objId, [CallerId] ClientId caller = default)
    {
        if (
            Connection.Maps.TryGet<GameObjectId, NetworkGameObject>(objId, out var obj) &&
            obj.TryGetComponent<Player>(out var player) && player.PlayerId == caller
        )
        {
            manager.ReceivePlantRequest(player);
        }
    }

    [Rpc(RpcPerms.AuthorityToClients)]
    public virtual void SendHarvest(GameObjectId cropId)
    {
        NetworkGameObject obj = Connection.Maps.Get<GameObjectId, NetworkGameObject>(cropId);
        manager.RemoveCrop(obj.GetComponent<Crop>());
    }
    
    [Rpc(RpcPerms.ClientsToAuthority)]
    public virtual void RequestHarvest(GameObjectId cropId, GameObjectId playerId, [CallerId] ClientId caller = default)
    {
        if (
            Connection.Maps.TryGet<GameObjectId, NetworkGameObject>(cropId, out var cropObj) &&
            cropObj.TryGetComponent<Crop>(out var crop) && 

            Connection.Maps.TryGet<GameObjectId, NetworkGameObject>(playerId, out var playerObj) &&
            playerObj.TryGetComponent<Player>(out var player) && player.PlayerId == caller &&

            (crop.transform.position - player.transform.position).magnitude <= player.harvestReach
        )
        {
            manager.ReceiveHarvestRequest(player, crop);
        }
    }
}