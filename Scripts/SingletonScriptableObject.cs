using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditorInternal;
using UnityEditor.Build.Reporting;
#endif

namespace AillieoUtils
{
    public abstract class SingletonScriptableObject<T> : BaseSingletonScriptableObject
        where T : ScriptableObject
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
#if DEBUG
                if (m_instance == null)
                {
                    UnityEngine.Debug.LogError(
                        $"Failed to get instance for {typeof(T)}, make sure it is included in building process");
                }
#endif
                return m_instance;
            }
        }

        protected virtual void Awake()
        {
            m_instance = this as T;
        }
    }

    // 用于在ProjectSettings 显示 覆盖默认的path
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SettingsMenuPathAttribute : Attribute
    {
        internal readonly string path;

        public SettingsMenuPathAttribute(string path)
        {
            this.path = path;
        }
    }

    public abstract class BaseSingletonScriptableObject : ScriptableObject
    {

#if UNITY_EDITOR
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

        private static Type[] cachedSingletonScriptableObjectTypes;
        private static readonly Dictionary<Type, ScriptableObject> cachedInstanceForType = new Dictionary<Type, ScriptableObject>();

        private static readonly string projectSettingAssetsFolder = "ProjectSettings/SingletonScriptableObjects/";

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
            string path = Path.Combine(projectSettingAssetsFolder, $"{type.Name}.asset");
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

        private static ScriptableObject ResetInstance(Type type)
        {
            ScriptableObject asset = LoadFromProjectSettingsFolder(type);

            DestroyImmediate(asset);
            cachedInstanceForType.Remove(type);

            return CreateInstanceForType(type);
        }

        internal class Provider : SettingsProvider
        {
            private static readonly string prefix = "Project/";
            private readonly Type type;

            private ScriptableObject asset;
            private UnityEditor.Editor editor;
            private GenericMenu contextMenu;

            public override void OnTitleBarGUI()
            {
                base.OnTitleBarGUI();

                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), "IN TitleText"))
                {
                    contextMenu.ShowAsContext();
                }
            }

            public override void OnGUI(string search)
            {
                base.OnGUI(search);

                asset = LoadOrCreateInstanceForType(type);

                if (editor == null || editor.target != asset)
                {
                    editor = UnityEditor.Editor.CreateEditor(asset);
                }

                EditorGUI.BeginChangeCheck();

                editor.OnInspectorGUI();

                if (EditorGUI.EndChangeCheck())
                {
                    SaveToProjectSettingsFolder(asset);
                }
            }

            private Provider(string path, Type type)
                : base(path, SettingsScope.Project)
            {
                this.type = type;

                this.contextMenu = new GenericMenu();
                this.contextMenu.AddItem(new GUIContent("Reset"), false, () => ResetInstance(this.type));

                this.keywords = type.FullName.Split('.')
                    .Append("Aillieo")
                    .Append("AillieoUtils")
                    .Append("Singleton")
                    .Append("ScriptableObject");
            }

            [SettingsProviderGroup]
            public static SettingsProvider[] RegisterSettingsProviders()
            {
                return singletonScriptableObjectTypes
                    .Where(type => type != null)
                    .Select(type =>
                    {
                        string path = string.Empty;
                        var settingsMenuPath = type.GetCustomAttribute<SettingsMenuPathAttribute>();
                        if (settingsMenuPath != null && !string.IsNullOrWhiteSpace(settingsMenuPath.path))
                        {
                            path = $"{settingsMenuPath.path}";
                            if (!path.StartsWith(prefix, StringComparison.InvariantCulture))
                            {
                                path = prefix + path;
                            }
                        }
                        else
                        {
                            path = type.FullName.Replace('.', '/');
                        }

                        return new Provider(path, type);
                    }).ToArray();
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

                HashSet<UnityEngine.Object> preloadSet = new HashSet<UnityEngine.Object>(PlayerSettings.GetPreloadedAssets());

                foreach (var type in singletonScriptableObjectTypes)
                {
                    string path = Path.Combine(tempFolder, $"{type.Name}.asset");

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
            }

            public void OnPostprocessBuild(BuildReport report)
            {
                HashSet<UnityEngine.Object> preloadSet = new HashSet<UnityEngine.Object>(PlayerSettings.GetPreloadedAssets());

                foreach (var type in singletonScriptableObjectTypes)
                {
                    string path = Path.Combine(tempFolder, $"{type.Name}.asset");
                    ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                    preloadSet.Remove(asset);
                    AssetDatabase.DeleteAsset(path);
                }

                PlayerSettings.SetPreloadedAssets(preloadSet.ToArray());

                Directory.Delete(tempFolder);
                File.Delete($"{tempFolder}.meta");

                AssetDatabase.Refresh();
            }
        }
#endif
    }
}
