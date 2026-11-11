namespace BehaviourTreeEditor.Runtime
{
    public class UnityMethodBridge
    {
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void LogWithObject<T>(string message, T target)
        {
            var str = target.ToString();
            UnityEngine.Debug.Log($"{message}: {str}");
        }
    }
}