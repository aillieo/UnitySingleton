using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AillieoUtils
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T m_instance;
        protected static bool m_quitting;

        public static T Instance
        {
            get
            {
                if(m_instance == null)
                {
                    CreateInstance();
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

        protected static void CreateInstance()
        {
            if (m_quitting)
            {
                Debug.LogWarning($"Singleton of type {typeof(T).Name} will not be created while application is quiting");
                return;
            }

            if (m_instance == null)
            {
                var go = new GameObject($"[{typeof(T).Name}]");
                m_instance = go.AddComponent<T>();
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    GameObject.DontDestroyOnLoad(go);
                }
                else
                {
                    go.hideFlags = HideFlags.HideAndDontSave;
                }
                EditorApplication.playModeStateChanged -= OnApplicationPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnApplicationPlayModeStateChanged;
#else
                GameObject.DontDestroyOnLoad(go);
#endif
            }
        }

        private void Awake()
        {
            if (m_instance != null && m_instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                m_instance = this as T;
            }
        }

        protected void OnDestroy()
        {
            if (m_instance == this)
            {
                m_instance = null;
            }
        }

        protected void OnApplicationQuit()
        {
            m_quitting = true;

            if (m_instance != null)
            {
#if UNITY_EDITOR
                GameObject.DestroyImmediate(m_instance.gameObject);
#else
                GameObject.Destroy(m_instance.gameObject);
#endif

            }
        }

#if UNITY_EDITOR
        private static void OnApplicationPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    m_quitting = false;
                    break;
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    m_quitting = true;
                    if (m_instance != null)
                    {
                        GameObject.DestroyImmediate(m_instance.gameObject);
                        m_instance = null;
                    }
                    break;
            }
        }
#endif
    }
}
