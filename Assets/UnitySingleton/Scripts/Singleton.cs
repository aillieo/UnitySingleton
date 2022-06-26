// -----------------------------------------------------------------------
// <copyright file="Singleton.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils
{
    /// <summary>
    /// Base singleton class.
    /// </summary>
    /// <typeparam name="T">Type of singleton class.</typeparam>
    public abstract class Singleton<T>
        where T : Singleton<T>, new()
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
                }

                return instance;
            }
        }

        /// <summary>
        /// Destroy the instance.
        /// </summary>
        public static void Destroy()
        {
            instance = null;
        }
    }
}
