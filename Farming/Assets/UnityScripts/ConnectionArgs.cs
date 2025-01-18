using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OwlTree.Unity
{

    [CreateAssetMenu(fileName = "ConnectionArgs", menuName = "OwlTree/ConnectionArgs")]
    public class ConnectionArgs : ScriptableObject
    {
        /// <summary>
        /// A unique, max 64 ASCII character id used for simple client verification. <b>Default = "MyOwlTreeApp"</b>
        /// </summary>
        public string appId = "MyOwlTreeApp";

        public string sessionId = "MyAppSession";

        public bool isClient = true;

        public string serverAddr = "127.0.0.1";

        public int tcpPort = 8000;

        public int udpPort = 9000;

        public int maxClients = 4;

        public string hostAddr = null;

        public bool migratable = false;

        public bool shutdownWhenEmpty = true;

        public int connectionRequestRate = 5000;

        public int connectionRequestLimit = 10;

        public int connectionRequestTimeout = 20000;

        public int bufferSize = 2048;

        /// <summary>
        /// The version of Owl Tree this connection is running on. 
        /// This value can be lowered from the default to use older formats of Owl Tree. 
        /// <b>Default = Current Version</b>
        /// </summary>
        public ushort owlTreeVersion = 1;

        /// <summary>
        /// The minimum Owl Tree version that will be supported. If clients using an older version attempt to connect,
        /// they will be rejected. <b>Default = 0 (always accept)</b>
        /// </summary>
        public ushort minOwlTreeVersion = 0;

        /// <summary>
        /// The version of your app this connection is running on. <b>Default = 1</b>
        /// </summary>
        public ushort appVersion = 1;

        /// <summary>
        /// The minimum app version that will be supported. If clients using an older version attempt to connect,
        /// they will be rejected. <b>Default = 0 (always accept)</b>
        /// </summary>
        public ushort minAppVersion = 0;

        /// <summary>
        /// Adds Huffman encoding and decoding to the connection's read and send steps, with a priority of 100. <b>Default = true</b>
        /// </summary>
        public bool useCompression = true;

        public bool measureBandwidth = false;

        /// <summary>
        /// If false, Reading and writing to sockets will need to called by your program with <c>Read()</c>
        /// and <c>Send()</c>. These operations will be done synchronously.
        /// <br /><br />
        /// If true <b>(Default)</b>, reading and writing will be handled autonomously in a separate, dedicated thread. 
        /// Reading will fill a queue of RPCs to be executed in the main program thread by calling <c>ExecuteQueue()</c>.
        /// Reading and writing will be done at a regular frequency, as defined by the <c>threadUpdateDelta</c> arg.
        /// </summary>
        public bool threaded = true;

        /// <summary>
        /// If the connection is threaded, specify the number of milliseconds the read/write thread will spend sleeping
        /// between updates. <b>Default = 40 (25 ticks/sec)</b>
        /// </summary>
        public int threadUpdateDelta = 40;

        public GameObject[] prefabs;

        public Connection.Args GetArgs()
        {
            return new Connection.Args{
                appId = appId,
                sessionId = sessionId,
                role = isClient ? NetRole.Client : NetRole.Server,
                serverAddr = serverAddr,
                tcpPort = tcpPort,
                udpPort = udpPort,
                maxClients = maxClients,
                hostAddr = hostAddr,
                migratable = migratable,
                shutdownWhenEmpty = shutdownWhenEmpty,
                connectionRequestLimit = connectionRequestLimit,
                connectionRequestTimeout = connectionRequestTimeout,
                bufferSize = bufferSize,
                owlTreeVersion = owlTreeVersion,
                minOwlTreeVersion = minOwlTreeVersion,
                appVersion = appVersion,
                minAppVersion = minAppVersion,
                measureBandwidth = measureBandwidth,
                useCompression = useCompression,
                threaded = threaded,
                threadUpdateDelta = threadUpdateDelta,
                logger = Debug.Log,
                verbosity = Logger.Includes().AllRpcProtocols().AllTypeIds().ClientEvents()
            }.Add("prefabs", prefabs);
        }
    }
}
