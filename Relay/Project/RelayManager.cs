
using System.Collections.Concurrent;
using OwlTree;

public class ConnectionManager
{
    private ConcurrentDictionary<string, Connection> _connections = new();
    private ConcurrentDictionary<string, long> _startTimes = new();

    public int Capacity { get; private set; }

    public int Count => _connections.Count;

    public IEnumerable<Connection> Connections => _connections.Values;

    public ConnectionManager(int capacity = -1, int threadUpdateDelta = 40, long timeout = 60000)
    {
        Capacity = capacity;
        _threadUpdateDelta = threadUpdateDelta;
        _timeout = timeout;
        IsActive = true;
        _thread = new Thread(ThreadLoop);
        _thread.Start();
    }

    private long _timeout;

    private Thread _thread;
    private int _threadUpdateDelta;

    public bool IsActive { get; private set; }

    private void ThreadLoop()
    {
        List<string> toBeRemoved = new();
        while (IsActive)
        {
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var pair in _connections)
            {
                pair.Value.ExecuteQueue();
                if (
                    !pair.Value.IsActive || 
                    (
                        pair.Value.ClientCount == 0 && 
                        _startTimes.TryGetValue(pair.Key, out var startTime) && 
                        start - startTime > _timeout
                    )
                )
                {
                    toBeRemoved.Add(pair.Key);
                }
            }
            foreach (var sessionId in toBeRemoved)
            {
                if (_connections[sessionId].IsActive)
                    _connections[sessionId].Disconnect();
                _connections.Remove(sessionId, out var connection);
                _startTimes.Remove(sessionId, out var time);
            }
            toBeRemoved.Clear();
            long diff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            Thread.Sleep(Math.Max(0, _threadUpdateDelta - (int)diff));
        }

        foreach (var connection in Connections)
            connection.Disconnect();
        
        _connections.Clear();
        _startTimes.Clear();
    }

    public Connection Add(string sessionId, Connection.Args args)
    {
        if (Capacity != -1 && _connections.Count >= Capacity)
            throw new InvalidOperationException("Cannot create more connections, manager is at capacity.");
        
        if (_connections.ContainsKey(sessionId))
            throw new ArgumentException($"'{sessionId}' already exists.");
        
        var connection = new Connection(args);

        if (!_connections.TryAdd(sessionId, connection))
        {
            connection.Disconnect();
            throw new InvalidOperationException("Failed to cache new connection.");
        }
        _startTimes.TryAdd(sessionId, DateTimeOffset.Now.ToUnixTimeMilliseconds());
        
        return connection;
    }

    public Connection? Get(string sessionId)
    {
        return _connections.GetValueOrDefault(sessionId);
    }

    public string Get(Connection connection)
    {
        return _connections.Where(p => p.Value == connection).FirstOrDefault().Key;
    }

    public bool Contains(string sessionId) => _connections.ContainsKey(sessionId);

    public void DisconnectAll()
    {
        IsActive = false;
    }
}