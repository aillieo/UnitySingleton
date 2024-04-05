## UnitySingleton

[中文版本](README.zh-cn.md)

UnitySingleton provides six types of singleton base classes, which can be used to create the corresponding derived types as needed.

### Singleton

The most commonly used singleton and the basic implementation of the singleton pattern.

Class definition:

```c#
public class MySingleton : Singleton<MySingleton>
{
    public void MyMethod()
    {
    }
}
```

Use:

```c#
MySingleton.Instance.MyMethod();
```

### SingletonMonoBehaviour

Singleton based on `MonoBehaviour`.

Class definition:

```c#
public class MySingletonMonoBehaviour : SingletonMonoBehaviour<MySingletonMonoBehaviour>
{
    public void MyMethod()
    {
    }
}
```

Use:

```c#
MySingletonMonoBehaviour.Instance.MyMethod();
```

The instance will be displayed in Hierarchy after created:

![image_01](Screenshots/image_01.png)

### SingletonScriptableObject

Singleton based on `ScriptableObject`.

Class definition:

```c#
public class MySingletonScriptableObject : SingletonScriptableObject<MySingletonScriptableObject>
{
    public int intValue;
    public string stringValue;
}
```

Use:

```c#
UnityEngine.Debug.Log($"intValue is {MySingletonScriptableObject.Instance.intValue}");
```

After the singleton class has been created, it can be edited on the Project Settings page. The path to the page can be specified by adding the `SettingsMenuPath` attribute.

![image_02](Screenshots/image_02.png)

Whether to include the instance (asset) of the ScriptableObject in the build process to make it available in runtime player can be defined by overriding the `ShouldIncludeInBuild` method, e.g.:

```c#
protected override bool ShouldIncludeInBuild(BuildReport report)
{
    bool devBuild = (report.summary.options & UnityEditor.BuildOptions.Development) == 0;
    bool android = report.summary.platformGroup == UnityEditor.BuildTargetGroup.Android;
    return android && devBuild;
}
```

### SingletonPersistent

Class definition:

```c#
public class MySingletonPersistent : SingletonPersistent<MySingletonPersistent>
{
    public bool enableSound;
    public bool enableMusic;
    public int volume;
}
```

A SingletonPersistent instance supports runtime persistence, and by default JSON format is used. Overriding 'OnSave' and 'OnLoad' to customize the saving and loading process.

### SingletonThreadSafe

Class definition:

```c#
public class MySingletonThreadSafe : SingletonThreadSafe<MySingletonThreadSafe>
{
    private int value;

    public int Increase()
    {
        return Interlocked.Increment(ref value);
    }
}
```

### SingletonThreadLocal

Class definition:

```c#
public class MySingletonThreadLocal : SingletonThreadLocal<MySingletonThreadLocal>
{
    public void SomeMethod()
    {
        UnityEngine.Debug.Log(Thread.CurrentThread.ManagedThreadId);
    }
}
```

## Installation

Clone this repository and copy it to your project folder, or add https://github.com/aillieo/UnitySingleton.git#upm as a dependency in the Package Manager window.
