// -----------------------------------------------------------------------
// <copyright file="SingletonMonoBehaviour.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils
{
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;

#endif

    /// <summary>
    /// Base singleton <see cref = "MonoBehaviour" /> class.
    /// </summary>
    /// <typeparam name="T">Type of singleton class.</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour
        where T : SingletonMonoBehaviour<T>
    {
        private static T instance;

        /// <summary>
        /// Gets a value indicating whether the instance is created.
        /// </summary>
        public static bool HasInstance => instance != null;

        /// <summary>
        /// Gets the instance of the singleton.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    CreateInstance();
                }

                return instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the application is quiting.
        /// </summary>
        protected static bool quitting { get; private set; }

        /// <summary>
        /// Create the singleton instance.
        /// </summary>
        protected static void CreateInstance()
        {
            if (quitting)
            {
                Debug.LogWarning($"Singleton of type {typeof(T).Name} will not be created while application is quiting");
                return;
            }

            if (instance == null)
            {
                var go = new GameObject($"[{typeof(T).Name}]");
                instance = go.AddComponent<T>();
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

        /// <summary>
        /// Awake method for <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                instance = this as T;
            }
        }

        /// <summary>
        /// OnDestroy method for <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// OnApplicationQuit method for <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            quitting = true;

            if (instance != null)
            {
#if UNITY_EDITOR
                GameObject.DestroyImmediate(instance.gameObject);
#else
                GameObject.Destroy(instance.gameObject);
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
                    quitting = false;
                    break;
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    quitting = true;
                    if (instance != null)
                    {
                        GameObject.DestroyImmediate(instance.gameObject);
                        instance = null;
                    }

                    break;
            }
        }
#endif
    }
}
