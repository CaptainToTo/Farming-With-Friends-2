using System;

namespace OwlTree
{
    /// <summary>
    /// Unique integer Id for each network object. 
    /// </summary>
    public struct NetworkId : IEncodable
    {
        public const UInt32 FIRST_NETWORK_ID = 1;

        // the actual id
        private UInt32 _id;

        /// <summary>
        /// Get a NetworkId instance using an existing id.
        /// </summary>
        public NetworkId(uint id)
        {
            _id = id;
        }

        /// <summary>
        /// Get a NetworkId instance by decoding it from a span.
        /// </summary>
        public NetworkId(ReadOnlySpan<byte> bytes)
        {
            _id = 0;
            FromBytes(bytes);
        }

        /// <summary>
        /// The id value.
        /// </summary>
        public uint Id { get { return _id; } }

        /// <summary>
        /// Gets the network id from the given bytes.
        /// </summary>
        public void FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4)
                throw new ArgumentException("Span must have 4 bytes from ind to decode a ClientId from.");

            _id = BitConverter.ToUInt32(bytes);
        }

        /// <summary>
        /// Inserts id as bytes into the given span.
        /// </summary>
        public void InsertBytes(Span<byte> bytes)
        {
            if (bytes.Length < 4)
                return;
            BitConverter.TryWriteBytes(bytes, _id);
        }

        public int ByteLength() { return 4; }

        /// <summary>
        /// The network object id used to signal that there is no object. Id value is 0.
        /// </summary>
        public static NetworkId None = new NetworkId(0);

        // Operators

        /// <summary>
        /// Returns the id number as a string.
        /// </summary>
        public override string ToString()
        {
            return "<NetworkId: " + (_id == 0 ? "None" : _id.ToString()) + ">";
        }

        public static bool operator ==(NetworkId a, NetworkId b)
        {
            return a._id == b._id;
        }

        public static bool operator !=(NetworkId a, NetworkId b)
        {
            return a._id != b._id;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj.GetType() == typeof(NetworkId) && ((NetworkId)obj)._id == _id;
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public static int MaxLength()
        {
            return 4;
        }
    }
}