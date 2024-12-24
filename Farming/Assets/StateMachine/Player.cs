using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.StateMachine;

public class Player : MonoBehaviour
{
    public struct InputData : IStateData
    {
        public float deltaTime;
        public Vector2 moveDir;
        public bool jumped;
        public Player self;
    }

    private StateMachine _machine;

    public readonly PlayerIdle Idle = new PlayerIdle();
    public readonly PlayerMove Move = new PlayerMove();
    public readonly PlayerGrounded Grounded = new PlayerGrounded();
    public readonly PlayerAirborne Airborne = new PlayerAirborne();

    public Renderer Rend {
        get {
            if (_rend == null)
                _rend = GetComponent<Renderer>();
            return _rend;
        }
    }
    private Renderer _rend = null;

    public Color Color {
        get => _color;
        set {
            _color = value;
            Rend.material.color = _color * _shade;
        }
    }
    private Color _color = Color.white;

    public Color Shade {
        get => _shade;
        set {
            _shade = value;
            Rend.material.color = _color * _shade;
        }
    }
    private Color _shade = Color.white;

    public Rigidbody Rb {
        get {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();
            return _rb;
        }
    }
    private Rigidbody _rb = null;

    public float speed = 10f;
    public float jumpSpeed = 2f;

    void Awake()
    {
        _machine = new StateMachine(new State[]{Grounded, Idle}, new InputData{ self = this });
        Debug.Log(_machine.ToString());
    }

    void Update()
    {
        float x = Input.GetKey(KeyCode.A) ? -1f :
            Input.GetKey(KeyCode.D) ? 1f : 0;
        float y = Input.GetKey(KeyCode.W) ? 1f :
            Input.GetKey(KeyCode.S) ? -1f: 0;

        var input = new InputData{
            deltaTime = Time.deltaTime,
            moveDir = new Vector2(x, y),
            jumped = Input.GetKeyDown(KeyCode.Space),
            self = this
        };

        _machine.LogicUpdate(input);
    }

    void FixedUpdate()
    {
        float x = Input.GetKey(KeyCode.A) ? -1f :
            Input.GetKey(KeyCode.D) ? 1f : 0;
        float y = Input.GetKey(KeyCode.W) ? 1f :
            Input.GetKey(KeyCode.S) ? -1f: 0;

        var input = new InputData{
            deltaTime = Time.fixedDeltaTime,
            moveDir = new Vector2(x, y),
            self = this
        };

        _machine.PhysicsUpdate(input);
    }

    void LateUpdate()
    {
        float x = Input.GetKey(KeyCode.A) ? -1f :
            Input.GetKey(KeyCode.D) ? 1f : 0;
        float y = Input.GetKey(KeyCode.W) ? 1f :
            Input.GetKey(KeyCode.S) ? -1f: 0;

        var input = new InputData{
            deltaTime = Time.deltaTime,
            moveDir = new Vector2(x, y),
            self = this
        };

        _machine.RenderUpdate(input);
    }
}