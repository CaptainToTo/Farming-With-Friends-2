using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OwlTree.StateMachine;
using OwlTree.Unity;
using OwlTree;
using TMPro;
using UnityEngine.Events;

// The player controller uses a state machine
public class Player : NetworkBehaviour
{
    // data struct passed to states
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
    // The client id of the player this controller is assigned to
    public ClientId PlayerId {get; private set; }

    [Tooltip("Displays who this player is above their character.")]
    [SerializeField] TextMeshProUGUI _idText;
    [Tooltip("The camera attached to the player. Disabled if not the local player.")]
    [SerializeField] Camera _cmra;

    // true if this player controller is assigned to the local player
    public bool IsLocal => netcode == null || PlayerId == Connection.LocalId;
    
    // make this player controller networked
    public void SetNetcode(NetworkStateMachine netcode, ClientId playerId)
    {
        this.netcode = netcode;
        PlayerId = playerId;
        _idText.text = playerId.ToString();
        this.netcode.Initialize(_machine, new State[]{Idle, Move, Plant, Harvest, Grounded, Airborne, Jump});
        if (playerId != Connection.LocalId)
            _cmra.gameObject.SetActive(false);
    }

    // player states
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

    // listened to by crop manager to spawn and despawn crops
    public UnityEvent<Player> OnPlant;
    public UnityEvent<Player, Crop> OnHarvest;

    void Awake()
    {
        _machine = new StateMachine(new State[]{Grounded, Idle}, new InputData{ self = this });
        Debug.Log(_machine.ToString());
    }

    void Update()
    {
        if (!IsLocal)
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