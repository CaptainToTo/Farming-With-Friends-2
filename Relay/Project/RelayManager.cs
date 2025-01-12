
using System.Collections.Concurrent;
using OwlTree;

public class ConnectionManager
{
    private ConcurrentDictionary<string, Connection> _connections = new();

    public int Capacity { get; private set; }

    public int Count => _connections.Count;

    public IEnumerable<Connection> Connections => _connections.Select(p => p.Value);

    public ConnectionManager(int capacity = -1, int threadUpdateDelta = 40)
    {
        Capacity = capacity;
        _threadUpdateDelta = threadUpdateDelta;
        IsActive = true;
        _thread = new Thread(ThreadLoop);
        _thread.Start();
    }

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
                if (!pair.Value.IsActive)
                    toBeRemoved.Add(pair.Key);
            }
            foreach (var sessionId in toBeRemoved)
                _connections.Remove(sessionId, out var connection);
            long diff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            Thread.Sleep(Math.Max(0, _threadUpdateDelta - (int)diff));
        }

        foreach (var connection in Connections)
            connection.Disconnect();
        
        _connections.Clear();
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