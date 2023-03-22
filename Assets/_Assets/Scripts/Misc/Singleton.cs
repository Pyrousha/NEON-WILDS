using UnityEngine;
public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance = null;

    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<T>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Duplicate instance of singleton found: " + gameObject.name + ", destroying.");
            Destroy(gameObject);
        }
    }
}