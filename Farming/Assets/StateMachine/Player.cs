using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.StateMachine;
using OwlTree.Unity;
using OwlTree;
using TMPro;
using UnityEngine.Events;

public class Player : NetworkBehaviour
{
    public struct InputData : IStateData
    {
        public float deltaTime;
        public Vector2 moveDir;
        public bool jumped;
        public Player self;
        public bool farmPressed;
    }

    public NetworkStateMachine netcode;
    private StateMachine _machine;
    public ClientId PlayerId {get; private set; }

    [SerializeField] public GameObject cropPrefab;
    [SerializeField] TextMeshProUGUI _idText;

    public bool IsLocal => netcode == null || PlayerId == Connection.LocalId;

    public void SetNetcode(NetworkStateMachine netcode, ClientId playerId)
    {
        this.netcode = netcode;
        PlayerId = playerId;
        _idText.text = playerId.ToString() + "\n" + netcode.Connection.LocalId;
        this.netcode.Initialize(_machine, new State[]{Idle, Move, Plant, Harvest, Grounded, Airborne, Jump});
    }

    public readonly PlayerIdle Idle = new PlayerIdle();
    public readonly PlayerMove Move = new PlayerMove();
    public readonly PlayerPlant Plant = new PlayerPlant();
    public readonly PlayerHarvest Harvest = new PlayerHarvest();
    public readonly PlayerGrounded Grounded = new PlayerGrounded();
    public readonly PlayerAirborne Airborne = new PlayerAirborne();
    public readonly PlayerJump Jump = new PlayerJump();

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
    public float farmSpeed = 3f;
    public float harvestReach = 1.5f;

    public UnityEvent<Player> OnPlant;
    public UnityEvent<Player, Crop> OnHarvest;

    void Awake()
    {
        _machine = new StateMachine(new State[]{Grounded, Idle}, new InputData{ self = this });
        Debug.Log(_machine.ToString());
    }

    void Update()
    {
        if (!IsLocal || !ClientSwitch.IsSelected(PlayerId))
            return;

        float x = Input.GetKey(KeyCode.A) ? -1f :
            Input.GetKey(KeyCode.D) ? 1f : 0;
        float y = Input.GetKey(KeyCode.W) ? 1f :
            Input.GetKey(KeyCode.S) ? -1f: 0;

        var input = new InputData{
            deltaTime = Time.deltaTime,
            moveDir = new Vector2(x, y),
            jumped = Input.GetKeyDown(KeyCode.Space),
            farmPressed = Input.GetMouseButton(0),
            self = this
        };

        _machine.LogicUpdate(input);
    }

    void FixedUpdate()
    {
        if (!IsLocal)
            return;

        var input = new InputData{
            deltaTime = Time.fixedDeltaTime,
            moveDir =Vector2.zero,
            self = this
        };

        _machine.PhysicsUpdate(input);
    }
}