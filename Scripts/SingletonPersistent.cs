using System;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AillieoUtils
{
    // 用于在ProjectSettings 显示 覆盖默认的path
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PersistentPathAttribute : Attribute
    {
        internal readonly string path;

        public PersistentPathAttribute(string path)
        {
            this.path = path;
        }
    }

    internal static class TypeToPathCache
    {
        private static Dictionary<Type, string> cache;
        private static readonly string prefix = "SingletonObjs";
        public static string GetPath(Type type)
        {
            if (cache == null)
            {
                cache = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Select(t => (t, t.GetCustomAttribute<PersistentPathAttribute>()))
                    .Where(tp => tp.Item2 != null)
                    .ToDictionary(tp => tp.Item1, tp => Path.Combine(Application.persistentDataPath, tp.Item2.path));
            }

            if (!cache.TryGetValue(type, out string path))
            {
                path = Path.Combine(Application.persistentDataPath, prefix, type.FullName);
                cache.Add(type, path);
            }

            return path;
        }

        public static string GetPath<T>()
        {
            return GetPath(typeof(T));
        }
    }

    public abstract class SingletonPersistent<T> where T : SingletonPersistent<T>, new()
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new T();
                    m_instance.OnLoad(TypeToPathCache.GetPath<T>());
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

        public static void Save()
        {
            if (HasInstance)
            {
                string path = TypeToPathCache.GetPath<T>();
                m_instance.OnSave(path);
            }
        }

        protected virtual bool OnLoad(string path)
        {
            if (!File.Exists(path))
            {
                return true;
            }

            try
            {
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new StreamReader(file, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        JsonUtility.FromJsonOverwrite(json, this);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                return false;
            }
        }

        protected virtual bool OnSave(string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var file = File.Exists(path) ? new FileStream(path, FileMode.Truncate) : File.Create(path))
                {
                    using (var writer = new StreamWriter(file, Encoding.UTF8))
                    {
                        string json = JsonUtility.ToJson(this);
                        writer.Write(json);
                        writer.Close();
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                return false;
            }
        }
    }
}
