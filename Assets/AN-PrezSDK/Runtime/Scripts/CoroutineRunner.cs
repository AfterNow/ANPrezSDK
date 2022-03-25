using AfterNow.PrezSDK;
using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    public class CoroutineRunner : Singleton<CoroutineRunner>
    {
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
        private static readonly ConcurrentQueue<IEnumerator> _executionQueue1 = new ConcurrentQueue<IEnumerator>();

        public static void DispatchToMainThread(Action action)
        {
            _executionQueue.Enqueue(action);
        }

        public static void DispatchToMainThread(IEnumerator enumerator)
        {
            _executionQueue1.Enqueue(enumerator);
        }

        public void SkipXFrames(int frames, Action action)
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
    }

    public static class CoroutineExtension
    {
        /// <summary>
        /// Starts a coroutine on the MonoBehaviour of CoroutineRunner gameObject if MonoBehaviour param is null
        /// </summary>
        /// <param name="iterator"></param>
        public static Coroutine Start(this IEnumerator iterator, MonoBehaviour mb = null)
        {
            if (mb != null) return mb.StartCoroutine(iterator);
            return CoroutineRunner.Instance.StartCoroutine(iterator);
        }

        /// <summary>
        /// Allows only one instance of the coroutine to be running
        /// </summary>
        /// <param name="iterator"></param>
        /// <param name="mb"></param>
        /// <returns></returns>
        public static Coroutine StartOne(this IEnumerator iterator, MonoBehaviour mb = null)
        {
            if (mb != null)
            {
                mb.StopCoroutine(iterator);
                return mb.StartCoroutine(iterator);
            }
            CoroutineRunner.Instance.StopCoroutine(iterator);
            return CoroutineRunner.Instance.StartCoroutine(iterator);
        }

        /// <summary>
        /// Stop only works for Coroutines started with IEnumerator.Run
        /// </summary>
        /// <param name="coroutine"></param>
        public static void Stop(this Coroutine coroutine, MonoBehaviour mb = null)
        {
            if (mb != null) mb.StopCoroutine(coroutine);
            else CoroutineRunner.Instance.StopCoroutine(coroutine);
        }
    }
}
