// -----------------------------------------------------------------------
// <copyright file="SingletonThreadSafe.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils
{
    using System;
    using System.Threading;

    /// <summary>
    /// Thread-safe base singleton class.
    /// </summary>
    /// <typeparam name="T">Type of singleton class.</typeparam>
    public abstract class SingletonThreadSafe<T>
        where T : SingletonThreadSafe<T>, new()
    {
        private static readonly Lazy<T> instance = new Lazy<T>(() => new T(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets a value indicating whether the instance is created.
        /// </summary>
        public static bool HasInstance => instance.IsValueCreated;

        /// <summary>
        /// Gets the instance of the singleton.
        /// </summary>
        public static T Instance => instance.Value;
    }
}
