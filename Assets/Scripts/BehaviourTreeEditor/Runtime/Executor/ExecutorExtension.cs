using System.Collections.Generic;


namespace BehaviourTreeEditor.Runtime
{
    public static class ExecutorExtension
    {
        /// <summary>
        /// 基於 Depth 搜索共同祖先
        /// </summary>
        public static StateRuntimeNodeExecutor<TContext> LcaExecutor<TContext>(
            this IList<StateRuntimeNodeExecutor<TContext>> executors,
            int fromIndex,
            int toIndex)
        {
            var a = executors[fromIndex];
            var b = executors[toIndex];

            // 對齊深度
            while (a.Depth > b.Depth)
                a = executors[a.ParentIndex];
            while (b.Depth > a.Depth)
                b = executors[b.ParentIndex];

            while (a != b)
            {
                a = executors[a.ParentIndex];
                b = executors[b.ParentIndex];
            }

            return a;
        }
    }
}
