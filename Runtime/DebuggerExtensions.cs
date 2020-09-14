using Svelto.ECS;

namespace Svelto.ECS.Debugger
{
    public static class DebuggerExtensions
    {
        public static void AttachDebugger(this EnginesRoot root, string name = null)
        {
            Debugger.Instance.AddEnginesRoot(root, name);
        }
        public static void DetachDebugger(this EnginesRoot root)
        {
            Debugger.Instance.RemoveEnginesRoot(root);
        }
    }
}