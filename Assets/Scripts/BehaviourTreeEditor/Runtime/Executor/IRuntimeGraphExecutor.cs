using System.Collections.Generic;
using System.Reflection;


namespace BehaviourTreeEditor.Runtime
{
    /// <summary>
    /// Interface for executing behavior trees at runtime, providing methods to compile graphs and change states.
    /// </summary>
    public interface IRuntimeGraphExecutor
    {
        public static MethodInfo ChangeStateMethod =
            typeof(IRuntimeGraphExecutor).GetMethod(nameof(ChangeState));

        /// <summary>
        /// Common Graph Executor Compiler Entry
        /// </summary>
        /// <param name="graph">Target Runtime Graph to compile</param>
        /// <param name="variables">Variables default value binding</param>
        /// <param name="constants">Constants defulat value binding</param>
        void CompileGraph(RuntimeGraph graph, IEnumerable<IVariableBinding> variables, IEnumerable<IVariableBinding> constants);
        unsafe void ChangeStatePath(int* path, int* newPath, int pathLength);
        void ChangeState(int index);
    }

    /// <summary>
    /// Interface for executing behavior trees at runtime, providing methods to compile graphs and change states.
    /// </summary>
    public interface IRuntimeGraphExecutor<TContext> : IRuntimeGraphExecutor
    {
        public delegate void ResetDelegate(ref TContext context);
        ResetDelegate ResetContextDelegate { get; }
        void Execute(ref TContext context);
    }
}
