using System;
using System.Threading;

namespace AillieoUtils
{
    public abstract class SingletonThreadSafe<T> where T : class, new()
    {
        private static readonly Lazy<T> m_instance = new Lazy<T>(() => new T(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static bool HasInstance
        {
            get
            {
                return m_instance.IsValueCreated;
            }
        }

        public static T Instance
        {
            get
            {
                return m_instance.Value;
            }
        }
    }
}
