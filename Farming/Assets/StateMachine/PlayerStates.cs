using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.StateMachine;

public class PlayerIdle : State
{
    public override void OnEnter(State from, IStateData data)
    {
        ((Player.InputData)data).self.Color = Color.green;
    }

    public override void LogicUpdate(IStateData data)
    {
        if (((Player.InputData)data).moveDir.magnitude > 0.1f)
            SwapTo(((Player.InputData)data).self.Move);
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
        if (((Player.InputData)data).moveDir.magnitude < 0.1f)
            SwapTo(((Player.InputData)data).self.Idle);
        ((Player.InputData)data).self.transform.position += ((Player.InputData)data).self.speed * Time.deltaTime * new Vector3(((Player.InputData)data).moveDir.x, 0, ((Player.InputData)data).moveDir.y);
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
            ((Player.InputData)data).self.Rb.velocity = new Vector3(((Player.InputData)data).self.Rb.velocity.x, ((Player.InputData)data).self.jumpSpeed, ((Player.InputData)data).self.Rb.velocity.z);
    }

    public override void PhysicsUpdate(IStateData data)
    {
        var start = ((Player.InputData)data).self.transform.position + (Vector3.down * 0.45f);
        var dist = 0.2f;
        if (!Physics.Raycast(start, Vector3.down, dist, LayerMask.GetMask("Ground")))
            SwapTo(((Player.InputData)data).self.Airborne);
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
        Debug.DrawLine(start, start + (Vector3.down * dist), Color.red, 0.5f);
        if (Physics.Raycast(start, Vector3.down, dist, LayerMask.GetMask("Ground")))
            SwapTo(((Player.InputData)data).self.Grounded);
    }
}
