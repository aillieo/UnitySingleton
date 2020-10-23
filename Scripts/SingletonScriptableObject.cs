using System;
using System.Linq;
using UnityEngine;

namespace AillieoUtils
{
    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    InternalCreateInstance();
                }
                return m_instance;
            }
        }

        public static void TryConfigInstanceManually(T customInstance)
        {
            if(customInstance == null)
            {
                throw new Exception($"Invalid argument {nameof(customInstance)}");
            }

            if(m_instance != null)
            {
                Debug.LogError($"typeof {typeof(T)} already has a instance");
                return;
            }

            m_instance = customInstance;
        }

        public static void InternalCreateInstance()
        {
            if (m_instance == null)
            {
                m_instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();

                if (m_instance == null)
                {
                    throw new Exception($"Failed to create instance for {typeof(T)}");
                }
            }
        }
    }
}
