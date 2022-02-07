using System;

namespace AfterNow.PrezSDK.Internal.Helpers
{
    public static class PrezDebugger
    {
        internal static event Action<object> OnLog;
        internal static event Action<object> OnWarn;
        internal static event Action<object> OnError;
        internal static event Action<Exception> OnException;

        public static void Initialize(Action<object> OnLog, Action<object> OnWarn, Action<object> OnError, Action<Exception> OnException)
        {
            PrezDebugger.OnLog += OnLog;
            PrezDebugger.OnWarn += OnWarn;
            PrezDebugger.OnError += OnError;
            PrezDebugger.OnException += OnException;
        }

        internal static void Log(object message)
        {
            OnLog?.Invoke(message);
        }

        internal static void Warn(object message)
        {
            OnWarn?.Invoke(message);
        }

        internal static void Error(object message)
        {
            OnError?.Invoke(message);
        }

        internal static void Exception(Exception exception)
        {
            OnException?.Invoke(exception);
        }
    }
}