using System;
using System.Linq;
using System.Text;

namespace OwlTree
{
    /// <summary>
    /// Responsible for generating network object proxies that will handle RPC send & recv logic.
    /// Proxies and proxy factories are generated by OwlTree source generator.
    /// </summary>
    public abstract class ProxyFactory
    {
        // TODO: remove reflection usage
        /// <summary>
        /// Gets the specific project implementation.
        /// </summary>
        internal static ProxyFactory GetProjectImplementation()
        {
            var implementation = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ProxyFactory).IsAssignableFrom(t)).FirstOrDefault();
            if (implementation == null)
                return null;
            return (ProxyFactory)Activator.CreateInstance(implementation);
        }

        /// <summary>
        /// Returns an array of type id values for all of the user created NetworkObject types.
        /// </summary>
        public abstract byte[] GetTypeIds();

        /// <summary>
        /// Creates a new proxy for the given user defined NetworkObject type.
        /// </summary>
        public abstract NetworkObject CreateProxy(Type t);

        /// <summary>
        /// Gets the id of the network object type.
        /// </summary>
        public abstract byte TypeId(Type t);

        /// <summary>
        /// Returns true if the given type has an assigned id.
        /// </summary>
        public abstract bool HasTypeId(Type t);

        /// <summary>
        /// Gets the network object type from the given id.
        /// </summary>
        public abstract Type TypeFromId(byte id);

        internal string GetAllIdAssignments()
        {
            var ids = GetTypeIds();
            var str = new StringBuilder("All Network Type Ids:\n");
            foreach (var id in ids)
            {
                str.Append($"{id}: {TypeFromId(id)}\n");
            }
            return str.ToString();
        }
    }
}