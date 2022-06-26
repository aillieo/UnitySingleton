// -----------------------------------------------------------------------
// <copyright file="SingletonThreadLocal.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils
{
    using System.Threading;

    /// <summary>
    /// Thread local base singleton class.
    /// </summary>
    /// <typeparam name="T">Type of singleton class.</typeparam>
    public abstract class SingletonThreadLocal<T>
        where T : SingletonThreadLocal<T>, new()
    {
        private static ThreadLocal<T> instance = new ThreadLocal<T>(() => new T());

        /// <summary>
        /// Gets a value indicating whether the instance is created.
        /// </summary>
        public static bool HasInstance
        {
            get
            {
                return instance.IsValueCreated;
            }
        }

        /// <summary>
        /// Gets the instance of the singleton.
        /// </summary>
        public static T Instance
        {
            get
            {
                return instance.Value;
            }
        }
    }
}
