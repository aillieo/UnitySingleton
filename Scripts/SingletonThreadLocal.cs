// -----------------------------------------------------------------------
// <copyright file="SingletonThreadLocal.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils
{
    using System;
    using System.Threading;

    /// <summary>
    /// Thread local base singleton class.
    /// </summary>
    /// <typeparam name="T">Type of singleton class.</typeparam>
    public abstract class SingletonThreadLocal<T>
        where T : SingletonThreadLocal<T>, new()
    {
        private static ThreadLocal<T> instance = new ThreadLocal<T>();

        /// <summary>
        /// Gets a value indicating whether the instance is created for current thread.
        /// </summary>
        public static bool HasInstance
        {
            get
            {
                return instance != null && instance.IsValueCreated && instance.Value != null;
            }
        }

        /// <summary>
        /// Gets the instance of the singleton for current thread.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new InvalidOperationException($"All instances of {typeof(T)} were destroyed.");
                }

                if (!instance.IsValueCreated)
                {
                    instance.Value = new T();
                }

                return instance.Value;
            }
        }

        /// <summary>
        /// Destroy the instance for current thread.
        /// </summary>
        public static void Destroy()
        {
            if (instance.IsValueCreated)
            {
                instance.Value = null;
            }
        }

        /// <summary>
        /// Destroy the instances for all threads.
        /// </summary>
        public static void DestroyAll()
        {
            if (instance == null)
            {
                return;
            }

            var instanceToDispose = Interlocked.Exchange(ref instance, null);
            if (instanceToDispose != null)
            {
                instanceToDispose.Dispose();
            }
        }
    }
}
