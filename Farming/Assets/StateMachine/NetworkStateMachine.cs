using System;
using System.Collections.Generic;
using System.Linq;

namespace OwlTree.StateMachine
{
    /// <summary>
    /// Synchronizes a state machine across clients on a connection. Type T is the data container type 
    /// of the states the state machine contains.
    /// </summary>
    public class NetworkStateMachine : NetworkObject
    {
        /// <summary>
        /// The machine being synchronized.
        /// </summary>
        public StateMachine Machine { get; private set; } = null;

        /// <summary>
        /// The authority of this state machine. This doesn't necessarily have to be 
        /// the authority of the connection, allowing clients to have authority over 
        /// specific state machines.
        /// </summary>
        public ClientId Authority { get; private set; } = ClientId.None;

        /// <summary>
        /// True once this has been assigned a state machine through Initialize().
        /// </summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        /// Whether or not this will used cached, pre-allocated states when swapping states.
        /// Or, create new states based on the available state types.
        /// </summary>
        public bool CachedStates { get; private set; } = false;

        private Dictionary<Type, int> _typeIds = null;
        private Dictionary<State, int> _stateIds = null;

        private bool ContainsState(State state)
        {
            return (_typeIds != null && _typeIds.ContainsKey(state.GetType())) ||
                (_stateIds != null && _stateIds.ContainsKey(state));
        }

        private bool ContainsId(int id)
        {
            return (_typeIds != null && _typeIds.ContainsValue(id)) ||
                (_stateIds != null && _stateIds.ContainsValue(id));
        }

        private int GetId(State state)
        {
            if (_typeIds != null && _typeIds.ContainsKey(state.GetType()))
                return _typeIds[state.GetType()];
            if (_stateIds != null && _stateIds.ContainsKey(state))
                return _stateIds[state];
            return 0;
        }

        private State FindStateOnMachine(int id)
        {
            return Machine.Get(_typeIds.First(p => p.Value == id).Key);
        }

        private State GetState(int id)
        {
            if (_typeIds != null && _typeIds.ContainsValue(id))
                return (State)Activator.CreateInstance(_typeIds.First(p => p.Value == id).Key);
            if (_stateIds != null && _stateIds.ContainsValue(id))
                return _stateIds.First(p => p.Value == id).Key;
            return null;
        }

        /// <summary>
        /// Initialize this network state machine. Provide a collection of the possible
        /// types of states this machine can have. States excluded from this collection 
        /// will not be possible to swap to.
        /// </summary>
        public void Initialize(StateMachine machine, IEnumerable<Type> states)
        {
            Machine = machine;
            Authority = Connection.Authority;

            int curId = 1;
            var ordered = states.OrderBy(t => t.ToString());
            _typeIds = new();
            foreach (var state in ordered)
            {
                _typeIds.Add(state, curId);
                curId++;
            }
            CachedStates = false;

            Machine.OnStateSwap += OnStateSwap;
            Machine.OnInsertState += OnInsertState;
            Machine.OnRemoveState += OnRemoveState;

            Initialized = true;
        }

        /// <summary>
        /// Initialize this network state machine. Provide a collection of the possible states
        /// this machine can have active. This collection will be cached. States excluded from this collection
        /// will not be possible to swap to.
        /// </summary>
        public void Initialize(StateMachine machine, IEnumerable<State> states)
        {
            Machine = machine;
            Authority = Connection.Authority;

            int curId = 1;
            var ordered = states.OrderBy(t => t.ToString());
            _stateIds = new();
            foreach (var state in ordered)
            {
                _stateIds.Add(state, curId);
                curId++;
            }
            CachedStates = true;

            Machine.OnStateSwap += OnStateSwap;
            Machine.OnInsertState += OnInsertState;
            Machine.OnRemoveState += OnRemoveState;

            Initialized = true;
        }

        /// <summary>
        /// Rpc to set a new authority of this state machine.
        /// </summary>
        [Rpc(RpcCaller.Server)]
        public virtual void RPC_SetAuthority(ClientId authority)
        {
            Authority = authority;
        }

        private void OnRemoveState(int i, State state)
        {
            if (!ContainsState(state))
                throw new ArgumentException($"The state of type {state.GetType()} was not assigned as a valid state on initialization.");
            
            if (Connection.LocalId == Authority)
                RPC_RemoveState(i);
        }

        [Rpc(RpcCaller.Server)]
        public virtual void RPC_RemoveState(int i, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
            {
                if (Connection.IsAuthority)
                    RPC_SetAuthority(Authority);
                return;
            }
            
            Machine.RemoveAt(i);
        }

        private void OnInsertState(int i, State state)
        {
            if (!ContainsState(state))
                throw new ArgumentException($"The state of type {state.GetType()} was not assigned as a valid state on initialization.");
            
            if (Connection.LocalId == Authority)
                RPC_InsertState(i, GetId(state));
        }

        [Rpc(RpcCaller.Any)]
        public virtual void RPC_InsertState(int i, int state, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
            {
                if (Connection.IsAuthority)
                    RPC_SetAuthority(Authority);
                return;
            }

            if (!ContainsId(state))
                throw new ArgumentException($"The state id {state} was not assigned on initialization.");
            
            Machine.AddSubStateAt(i, GetState(state));
        }

        private void OnStateSwap(State from, State to)
        {
            if (!ContainsState(from))
                throw new ArgumentException($"The state of type {from.GetType()} was not assigned as a valid state on initialization.");
            if (!ContainsState(to))
                throw new ArgumentException($"The state of type {to.GetType()} was not assigned as a valid state on initialization.");
            
            if (Connection.LocalId == Authority)
                RPC_SwapStates(GetId(from), GetId(to));
        }

        [Rpc(RpcCaller.Any)]
        public virtual void RPC_SwapStates(int from, int to, [RpcCaller] ClientId caller = default)
        {
            if (caller != Authority)
            {
                if (Connection.IsAuthority)
                    RPC_SetAuthority(Authority);
                return;
            }
            
            if (!ContainsId(from))
                throw new ArgumentException($"The state id {from} was not assigned on initialization.");
            if (!ContainsId(to))
                throw new ArgumentException($"The state id {from} was not assigned on initialization.");
            
            if (CachedStates)
                Machine.SwapStates(GetState(from), GetState(to));
            else
                Machine.SwapStates(FindStateOnMachine(from), GetState(to));
        }
    }
}