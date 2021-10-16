using System.Threading;

namespace AillieoUtils
{
    public abstract class SingletonThreadLocal<T> where T : class, new()
    {
        private static ThreadLocal<T> m_instance;

        static SingletonThreadLocal()
        {
            m_instance = new ThreadLocal<T>(() => new T());
        }

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
