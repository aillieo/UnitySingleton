// -----------------------------------------------------------------------
// <copyright file="SingletonScriptableObject.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
#if UNITY_EDITOR
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditorInternal;
#endif

    /// <summary>
    /// Base singleton <see cref = "ScriptableObject" /> class.
    /// </summary>
    /// <typeparam name="T">Type of singleton class.</typeparam>
    public abstract class SingletonScriptableObject<T> : BaseSingletonScriptableObject
        where T : SingletonScriptableObject<T>
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
#if DEVELOPMENT_BUILD
                if (instance == null)
                {
                    Debug.LogError(
                        $"Failed to get instance for {typeof(T)}, make sure it is included in building process");
                }
#endif
                return instance;
            }
        }

        /// <summary>
        /// Awake method for <see cref="ScriptableObject"/>.
        /// </summary>
        protected virtual void Awake()
        {
            Debug.Log($"SingletonScriptableObject Awake: {typeof(T)}");
            instance = this as T;
        }
    }

    /// <summary>
    /// Provides custom path and keywords settings for a <see cref="SingletonScriptableObject{T}"/> instance in ProjectSettings window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SettingsMenuPathAttribute : Attribute
    {
        internal readonly string path;
        internal readonly string[] keywords;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsMenuPathAttribute"/> class.
        /// </summary>
        /// <param name="path">Display path in ProjectSettings window.</param>
        /// <param name="keywords">Keywords in ProjectSettings window.</param>
        public SettingsMenuPathAttribute(string path, string[] keywords = null)
        {
            this.path = path;
            this.keywords = keywords;
        }
    }

    /// <summary>
    /// Internal base class for <see cref="SingletonScriptableObject{T}"/>.
    /// </summary>
    public abstract class BaseSingletonScriptableObject : ScriptableObject
    {
#if UNITY_EDITOR

        private static readonly Dictionary<Type, ScriptableObject> cachedInstanceForType = new Dictionary<Type, ScriptableObject>();
        private static readonly string projectSettingAssetsFolder = "ProjectSettings/SingletonScriptableObjects/";

        private static Type[] cachedSingletonScriptableObjectTypes;

        private static Type[] singletonScriptableObjectTypes
        {
            get
            {
                if (cachedSingletonScriptableObjectTypes == null)
                {
                    cachedSingletonScriptableObjectTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Where(t => t.BaseType != null
                                    && t.BaseType.IsGenericType
                                    && t.BaseType.GetGenericTypeDefinition() == typeof(SingletonScriptableObject<>))
                        .ToArray();
                }

                return cachedSingletonScriptableObjectTypes;
            }
        }

        /// <summary>
        /// Should include this asset in build process.
        /// </summary>
        /// <param name="report">Current building report.</param>
        /// <returns>Should include or not.</returns>
        protected virtual bool ShouldIncludeInBuild(BuildReport report)
        {
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LoadAllInstances()
        {
            foreach (Type type in singletonScriptableObjectTypes)
            {
                LoadOrCreateInstanceForType(type);
            }
        }

        private static void SaveToProjectSettingsFolder(ScriptableObject asset)
        {
            Directory.CreateDirectory(projectSettingAssetsFolder);
            InternalEditorUtility.SaveToSerializedFileAndForget(
                new UnityEngine.Object[] { asset },
                Path.Combine(projectSettingAssetsFolder, $"{asset.GetType().Name}.asset"),
                true);
        }

        private static ScriptableObject LoadFromProjectSettingsFolder(Type type)
        {
            var path = Path.Combine(projectSettingAssetsFolder, $"{type.Name}.asset");
            UnityEngine.Object[] objs = InternalEditorUtility.LoadSerializedFileAndForget(path);
            ScriptableObject asset = null;
            if (objs != null && objs.Length > 0)
            {
                asset = objs[0] as ScriptableObject;
            }

            if (asset != null)
            {
                asset.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
            }

            return asset;
        }

        private static ScriptableObject CreateInstanceForType(Type type)
        {
            ScriptableObject asset = CreateInstance(type);

            asset.name = type.Name;
            asset.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;

            SaveToProjectSettingsFolder(asset);

            return asset;
        }

        private static ScriptableObject LoadOrCreateInstanceForType(Type type)
        {
            if (!cachedInstanceForType.TryGetValue(type, out ScriptableObject instance) || instance == null)
            {
                ScriptableObject newInstance = LoadFromProjectSettingsFolder(type);
                if (newInstance == null)
                {
                    newInstance = CreateInstanceForType(type);
                }

                cachedInstanceForType[type] = newInstance;
                instance = newInstance;
            }

            return instance;
        }

        private static void ResetInstance(Type type)
        {
            ScriptableObject asset = LoadFromProjectSettingsFolder(type);

            DestroyImmediate(asset);
            cachedInstanceForType.Remove(type);

            CreateInstanceForType(type);
        }

        internal class Provider : SettingsProvider
        {
            private readonly Type type;

            private ScriptableObject asset;
            private Editor editor;
            private GenericMenu contextMenu;

            private Provider(string path, Type type, string[] keywords)
                : base(path, SettingsScope.Project)
            {
                if (type == null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                this.type = type;

                this.contextMenu = new GenericMenu();
                this.contextMenu.AddItem(new GUIContent("Reset"), false, () => ResetInstance(this.type));

                if (keywords != null)
                {
                    this.keywords = keywords;
                }
                else if (!string.IsNullOrEmpty(type.FullName))
                {
                    this.keywords = type.FullName.Split('.')
                        .Append("Aillieo")
                        .Append("AillieoUtils")
                        .Append("Singleton")
                        .Append("ScriptableObject");
                }
            }

            [SettingsProviderGroup]
            public static SettingsProvider[] RegisterSettingsProviders()
            {
                return singletonScriptableObjectTypes
                    .Where(type => type != null)
                    .Select(type =>
                    {
                        var path = string.Empty;
                        string[] keywords = null;
                        var settingsMenuPath = type.GetCustomAttribute<SettingsMenuPathAttribute>();

                        if (settingsMenuPath != null)
                        {
                            keywords = settingsMenuPath.keywords;
                            path = $"{settingsMenuPath.path}";
                        }

                        if (string.IsNullOrWhiteSpace(path) && !string.IsNullOrEmpty(type.FullName))
                        {
                            path = $"Project/{type.FullName.Replace('.', '/')}";
                        }

                        return new Provider(path, type, keywords);
                    })
                    .Select(provider => provider as SettingsProvider)
                    .ToArray();
            }

            public override void OnTitleBarGUI()
            {
                base.OnTitleBarGUI();

                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), "IN TitleText"))
                {
                    this.contextMenu.ShowAsContext();
                }
            }

            public override void OnGUI(string search)
            {
                base.OnGUI(search);

                this.asset = LoadOrCreateInstanceForType(this.type);

                if (this.editor == null || this.editor.target != this.asset)
                {
                    this.editor = Editor.CreateEditor(this.asset);
                }

                EditorGUI.BeginChangeCheck();

                this.editor.OnInspectorGUI();

                if (EditorGUI.EndChangeCheck())
                {
                    SaveToProjectSettingsFolder(this.asset);
                }
            }
        }

        // build之前 拷贝到Temp 并添加到Preload
        // build之后 删掉Temp目录
        internal class AssetBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            private static readonly string tempFolder = "Assets/SingletonScriptableObject Temp Folder";

            public int callbackOrder => -100;

            public void OnPreprocessBuild(BuildReport report)
            {
                Directory.CreateDirectory(tempFolder);

                var preloadSet = new HashSet<UnityEngine.Object>(PlayerSettings.GetPreloadedAssets());

                foreach (var type in singletonScriptableObjectTypes)
                {
                    var path = Path.Combine(tempFolder, $"{type.Name}.asset");

                    AssetDatabase.DeleteAsset(path);

                    var asset = LoadOrCreateInstanceForType(type);
                    if (!(asset is BaseSingletonScriptableObject filter && filter.ShouldIncludeInBuild(report)))
                    {
                        continue;
                    }

                    asset.hideFlags = HideFlags.None;
                    AssetDatabase.CreateAsset(asset, path);

                    preloadSet.Add(asset);
                }

                PlayerSettings.SetPreloadedAssets(preloadSet.ToArray());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorApplication.update += this.CheckBuildStatus;
            }

            public void OnPostprocessBuild(BuildReport report)
            {
                if (!Directory.Exists(tempFolder))
                {
                    return;
                }

                var preloadSet = new HashSet<UnityEngine.Object>(PlayerSettings.GetPreloadedAssets());

                foreach (var type in singletonScriptableObjectTypes)
                {
                    var path = Path.Combine(tempFolder, $"{type.Name}.asset");
                    ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                    preloadSet.Remove(asset);
                    AssetDatabase.DeleteAsset(path);
                }

                PlayerSettings.SetPreloadedAssets(preloadSet.ToArray());

                Directory.Delete(tempFolder);
                File.Delete($"{tempFolder}.meta");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            private void CheckBuildStatus()
            {
                if (!BuildPipeline.isBuildingPlayer)
                {
                    EditorApplication.update -= this.CheckBuildStatus;

                    BuildReport lastBuildReport = this.TryLoadLastBuildReport();
                    if (lastBuildReport.summary.result != BuildResult.Succeeded)
                    {
                        this.OnPostprocessBuild(lastBuildReport);
                    }
                }
            }

            private BuildReport TryLoadLastBuildReport()
            {
                UnityEngine.Object[] allObjects = InternalEditorUtility.LoadSerializedFileAndForget("Library/LastBuild.buildreport");
                if (allObjects != null && allObjects.Length > 0)
                {
                    return allObjects.FirstOrDefault(o => o is BuildReport) as BuildReport;
                }

                return null;
            }
        }
#endif
    }
}
