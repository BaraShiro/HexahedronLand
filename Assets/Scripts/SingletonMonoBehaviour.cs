using UnityEngine;

/// <summary>
/// Derive from this base class to create a MonoBehaviour script that implements a thread safe singleton pattern <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of <see cref="Instance"/>, should be set to the type of the deriving class.</typeparam>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    private static readonly object SingletonLock = typeof(T);
    
    /// <value>The singleton instance of this class <typeparamref name="T"/>.</value>
    public static T Instance { get; private set; }

    private void Awake()
    {
        lock (SingletonLock)
        {
            if (Instance != null && Instance != this as T)
            {
                OnFailedCreateInstance();
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            OnCreateInstance();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            OnDestroyInstance();
            Instance = null;
        }
    }

    /// <summary>
    /// This function is called when the instance is assigned.
    /// </summary>
    /// <remarks>
    /// <see cref="Instance"/> holds a reference to an object when this function is called.
    /// </remarks>
    /// <seealso cref="OnFailedCreateInstance"/>
    /// <seealso cref="OnDestroyInstance"/>
    protected virtual void OnCreateInstance() { }

    /// <summary>
    /// This function is called when trying to assign the instance when it is already assigned to another object.
    /// </summary>
    /// <remarks>
    /// The function is called right before <see cref="GameObject.Destroy(Object)"/> is called on the object.
    /// </remarks>
    /// <seealso cref="OnCreateInstance"/>
    /// <seealso cref="OnDestroyInstance"/>
    protected virtual void OnFailedCreateInstance() { }

    /// <summary>
    /// This function is called when the <see cref="GameObject"/> holding the instance is destroyed.
    /// </summary>
    /// <remarks>
    /// <see cref="Instance"/> still holds a reference to an object when this function is called.
    /// </remarks>
    /// <seealso cref="OnCreateInstance"/>
    /// <seealso cref="OnFailedCreateInstance"/>
    protected virtual void OnDestroyInstance() { }

}
