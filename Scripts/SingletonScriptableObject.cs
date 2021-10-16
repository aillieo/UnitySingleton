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

        public static bool HasInstance
        {
            get
            {
                return m_instance != null;
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
                Debug.LogError($"typeof {typeof(T)} already has an instance");
                return;
            }

            m_instance = customInstance;
        }

        protected static void InternalCreateInstance()
        {
            if (m_instance == null)
            {
                m_instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();

#if UNITY_EDITOR
                // 很诡异 编辑器经常出现 第一次运行的时候取不到
                if (m_instance == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                    if (guids.Length > 0)
                    {
                        m_instance = Resources.Load<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }
#endif

                if (m_instance == null)
                {
                    throw new Exception($"Failed to create instance for {typeof(T)}");
                }
            }
        }
    }
}
