namespace AillieoUtils
{
    public abstract class Singleton<T> where T : class, new()
    {
        private static T m_instance = null;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new T();
                }

                return m_instance;
            }
        }

        public static bool HasInstance
        {
            get
            {
                return m_instance != null;
            }
        }

        public static void Destroy()
        {
            m_instance = null;
        }
    }
}
