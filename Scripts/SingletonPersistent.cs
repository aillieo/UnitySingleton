// -----------------------------------------------------------------------
// <copyright file="SingletonPersistent.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Provides custom path to save the <see cref="SingletonPersistent{T}"/> instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PersistentPathAttribute : Attribute
    {
        internal readonly string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentPathAttribute"/> class.
        /// </summary>
        /// <param name="path">Path to save the singleton instance.</param>
        public PersistentPathAttribute(string path)
        {
            this.path = path;
        }
    }

    /// <summary>
    /// Base singleton class that supports persistent.
    /// </summary>
    /// <typeparam name="T">Type of singleton class.</typeparam>
    public abstract class SingletonPersistent<T>
        where T : SingletonPersistent<T>, new()
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
                    instance = new T();
                    instance.OnLoad(TypeToPathCache.GetPath<T>());
                }

                return instance;
            }
        }

        /// <summary>
        /// Destroy the instance. Instance not saved will lose.
        /// </summary>
        public static void Destroy()
        {
            instance = null;
        }

        /// <summary>
        /// Save the singleton to disk.
        /// </summary>
        public static void Save()
        {
            if (HasInstance)
            {
                var path = TypeToPathCache.GetPath<T>();
                instance.OnSave(path);
            }
        }

        /// <summary>
        /// Load data for an instance from disk and deserialize.
        /// </summary>
        /// <param name="path">Path of persistent data.</param>
        /// <returns>Success deserialize and load.</returns>
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
                        var json = reader.ReadToEnd();
                        JsonUtility.FromJsonOverwrite(json, this);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        /// <summary>
        /// Serialize an instance and save to disk.
        /// </summary>
        /// <param name="path">Path of persistent data.</param>
        /// <returns>Success serialize and save.</returns>
        protected virtual bool OnSave(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var file = File.Exists(path) ? new FileStream(path, FileMode.Truncate) : File.Create(path))
                {
                    using (var writer = new StreamWriter(file, Encoding.UTF8))
                    {
                        var json = JsonUtility.ToJson(this);
                        writer.Write(json);
                        writer.Close();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
    }

    internal static class TypeToPathCache
    {
        private static readonly string prefix = "SingletonObjs";
        private static Dictionary<Type, string> cache;

        public static string GetPath<T>()
        {
            return GetPath(typeof(T));
        }

        private static string GetPath(Type type)
        {
            if (cache == null)
            {
                cache = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Select(t => (t, t.GetCustomAttribute<PersistentPathAttribute>()))
                    .Where(tp => tp.Item2 != null)
                    .ToDictionary(tp => tp.Item1, tp => Path.Combine(Application.persistentDataPath, tp.Item2.path));
            }

            if (!cache.TryGetValue(type, out var path))
            {
                path = Path.Combine(Application.persistentDataPath, prefix, type.FullName);
                cache.Add(type, path);
            }

            return path;
        }
    }
}
