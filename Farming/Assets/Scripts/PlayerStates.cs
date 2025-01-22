using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.StateMachine;
using System.Linq;

// Contains all states a player can be in

public class PlayerIdle : State
{
    public override void OnEnter(State from, IStateData data)
    {
        ((Player.InputData)data).self.Color = Color.green;
    }

    public override void LogicUpdate(IStateData data)
    {
        var playerData = (Player.InputData)data;

        if (playerData.moveDir.magnitude > 0.1f)
            SwapTo(playerData.self.Move);
        
        else if (playerData.farmPressed && playerData.self.Grounded.IsActive)
        {
            var crops = Physics.OverlapSphere(playerData.self.transform.position, playerData.self.harvestReach, LayerMask.GetMask("Crops"));
            if (crops.Length > 0)
                SwapTo(playerData.self.Harvest);
            else
                SwapTo(playerData.self.Plant);
        }
    }
}

public class PlayerMove : State
{
    public override void OnEnter(State from, IStateData data)
    {
        ((Player.InputData)data).self.Color = Color.blue;
    }

    public override void LogicUpdate(IStateData data)
    {
        var playerData = (Player.InputData)data;
        if (playerData.moveDir.magnitude < 0.1f)
            SwapTo(playerData.self.Idle);
        playerData.self.transform.position += playerData.self.speed * Time.deltaTime * 
            new Vector3(playerData.moveDir.x, 0, playerData.moveDir.y);
    }
}

// hold down left click to harvest or plant

public class PlayerPlant : State
{

    float timer = 0f;
    public override void OnEnter(State from, IStateData data)
    {
        ((Player.InputData)data).self.Color = Color.yellow;
        timer = ((Player.InputData)data).self.farmSpeed;
    }

    public override void LogicUpdate(IStateData data)
    {
        var playerData = (Player.InputData)data;
        if (timer <= 0)
        {
            playerData.self.OnPlant.Invoke(playerData.self);
            SwapTo(playerData.self.Idle);
            return;
        }

        if (playerData.farmPressed)
            timer -= playerData.deltaTime;
        else
            SwapTo(playerData.self.Idle);
    }
}

public class PlayerHarvest : State
{
    float timer = 0f;
    public override void OnEnter(State from, IStateData data)
    {
        ((Player.InputData)data).self.Color = Color.cyan;
        timer = ((Player.InputData)data).self.farmSpeed;
    }

    public override void LogicUpdate(IStateData data)
    {
        var playerData = (Player.InputData) data;

        if (timer <= 0)
        {
            var crops = Physics.OverlapSphere(
                    playerData.self.transform.position, 
                    playerData.self.harvestReach, 
                    LayerMask.GetMask("Crops")
                ).Select(c => c.GetComponent<Crop>());
            if (crops.Count() > 0)
                playerData.self.OnHarvest.Invoke(playerData.self, crops.First());
            SwapTo(playerData.self.Idle);
            return;
        }

        if (playerData.farmPressed)
            timer -= playerData.deltaTime;
        else
            SwapTo(playerData.self.Idle);
    }
}

public class PlayerGrounded : State
{
    public override void OnEnter(State from, IStateData data)
    {
        ((Player.InputData)data).self.Shade = Color.white;
    }

    public override void LogicUpdate(IStateData data)
    {
        if (((Player.InputData)data).jumped)
            SwapTo(((Player.InputData)data).self.Jump);
    }

    public override void PhysicsUpdate(IStateData data)
    {
        var start = ((Player.InputData)data).self.transform.position + (Vector3.down * 0.45f);
        var dist = 0.2f;
        if (!Physics.Raycast(start, Vector3.down, dist, LayerMask.GetMask("Ground")))
            SwapTo(((Player.InputData)data).self.Airborne);
    }
}

public class PlayerJump : State
{
    public override void OnEnter(State from, IStateData data)
    {
        var playerData = (Player.InputData)data;
        playerData.self.Rb.velocity = new Vector3(playerData.self.Rb.velocity.x, playerData.self.jumpSpeed, playerData.self.Rb.velocity.z);
        SwapTo(playerData.self.Airborne);
    }
}

public class PlayerAirborne : State
{
    public override void OnEnter(State from, IStateData data)
    {
        ((Player.InputData)data).self.Shade = Color.gray;
    }

    public override void PhysicsUpdate(IStateData data)
    {
        var start = ((Player.InputData)data).self.transform.position + (Vector3.down * 0.45f);
        var dist = 0.2f;
        // ((Player.InputData)data).self.transform.eulerAngles += Vector3.up * 0.5f;
        if (Physics.Raycast(start, Vector3.down, dist, LayerMask.GetMask("Ground")))
            SwapTo(((Player.InputData)data).self.Grounded);
    }
}
