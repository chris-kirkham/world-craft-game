using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _inst;

    public static T Inst
    {
        get
        {
            if (_inst)
            {
                return _inst;
            }

            return null;
        }
    }

    private void Awake()
    {
        if (_inst)
        {
            Debug.LogWarning($"Instance of {typeof(T).Name} already exists! Destroying this instance.");
            Destroy(gameObject);
        }
        else
        {
            _inst = this as T;
        }

        OnAwake();
    }

    private void OnDestroy()
    {
        if (_inst == this)
        {
            _inst = null;
        }
    }

    public static bool InstExists()
    {
        return _inst;
    }

    protected virtual void OnAwake()
    {

    }
}
