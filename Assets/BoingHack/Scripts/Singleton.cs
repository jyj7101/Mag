using UnityEngine;
using System.Collections;

namespace Utils
{
    using System;
    using UnityEngine;
    /// <summary>
    /// implement by System.Lazy
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static readonly Lazy<T> instance = new Lazy<T>(() => new T());

        public static T Instance
        {
            get
            {
                return instance.Value;
            }
        }

        protected Singleton() { }
    }

    public class LocalSingleton<T> : MonoBehaviour where T : LocalSingleton<T>
    {
        protected static T _instance;

        public static T Instance { get => _instance; }


        private static event Action<T> onInitialized;
        public static event Action<T> OnInitialized
        {
            add
            {
                if (_instance != null)
                {
                    value?.Invoke(_instance);
                    return;
                }

                onInitialized += value;
            }
            remove
            {
                onInitialized -= value;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;

                Initialize();

                //invoke added handler
                onInitialized?.Invoke(_instance);
                onInitialized = null;
            }
            else if (_instance != this)
            {
                DestroyImmediate(gameObject);
            }
        }

        protected virtual void Initialize()
        {

        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static bool TryGetInstance(out T instance)
        {
            instance = _instance;
            return instance != null;
        }
    }


    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        protected static T _instance;

        private static object _lock = new object();

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                        "' already destroyed on application quit." +
                        " Won't create again - returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.LogError("[Singleton] Something went really wrong " +
                                " - there should never be more than 1 singleton!" +
                                " Reopening the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton) " + typeof(T).ToString();

                            DontDestroyOnLoad(singleton);
                            Debug.Log("[Singleton] An instance of " + typeof(T) +
                                " is needed in the scene, so '" + singleton +
                                "' was created with DontDestroyOnLoad.");
                        }
                        else
                        {
                            Debug.Log("[Singleton] Using instance already created: " +
                                _instance.gameObject.name);
                        }
                    }

                    return _instance;
                }
            }
        }

        private static event Action<T> onInitialized;
        public static event Action<T> OnInitialized
        {
            add
            {
                if (_instance != null)
                {
                    value?.Invoke(_instance);
                    return;
                }

                onInitialized += value;
            }
            remove
            {
                onInitialized -= value;
            }
        }

        private static bool applicationIsQuitting = false;
        public static bool IsQuitting { get => applicationIsQuitting; }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);

                Initialize();

                //invoke added handler
                onInitialized?.Invoke(_instance);
                onInitialized = null;
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        protected virtual void Initialize()
        {

        }

        protected virtual void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        private static bool IsDontDestroyOnLoad()
        {
            if (_instance == null)
            {
                return false;
            }
            // Object exists independent of Scene lifecycle, assume that means it has DontDestroyOnLoad set
            if ((_instance.gameObject.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (IsDontDestroyOnLoad())
            {
                applicationIsQuitting = true;
            }
        }
    }
}