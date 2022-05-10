using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    internal class CoroutineRunner : MonoBehaviour
    {
        internal static bool isQuitting = false;
        internal static CoroutineRunner Instance
        {
            get
            {
                if(isQuitting) throw new InvalidProgramException("Application is quitting");

                if (_instance == null)
                {
                    _instance = new GameObject("PrezCoroutineRunner").AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        private static CoroutineRunner _instance;
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
        private static readonly ConcurrentQueue<IEnumerator> _executionQueue1 = new ConcurrentQueue<IEnumerator>();

        internal static T AddComponent<T>() where T : Component
        {
            var instance = Instance;
            if (instance.TryGetComponent<T>(out T comp)) return comp;
            else
            {
                return instance.gameObject.AddComponent<T>();
            }
        }

        internal static void DispatchToMainThread(Action action)
        {
            _executionQueue.Enqueue(action);
        }

        internal static void DispatchToMainThread(IEnumerator enumerator)
        {
            _executionQueue1.Enqueue(enumerator);
        }

        internal void SkipXFrames(int frames, Action action)
        {
            if (action != null)
                StartCoroutine(SkipXFramesAndExecute(frames, action));
        }

        private IEnumerator SkipXFramesAndExecute(int frames, Action action)
        {
            while (frames-- > 0)
            {
                yield return null;
            }
            action();
        }

        private void Update()
        {
            int count = _executionQueue.Count;
            for (int i = 0; i < count; i++)
            {
                _executionQueue.TryDequeue(out Action action);
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
            }

            count = _executionQueue1.Count;
            for (int i = 0; i < count; i++)
            {
                _executionQueue1.TryDequeue(out IEnumerator enumerator);
                if (enumerator != null)
                {
                    StartCoroutine(enumerator);
                }
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }
    }
}
