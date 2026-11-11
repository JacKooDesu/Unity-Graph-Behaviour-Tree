using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    [CreateAssetMenu(menuName = "Behaviour Tree Editor/CompilableRuntimeGraph")]
    public class CompilableRuntimeGraph : ScriptableObject
    {
        [SerializeField]
        public RuntimeGraph Graph;

        [SerializeReference]
        [SerializeField]
        public List<IVariableBinding> Variables = new();
        [SerializeReference]
        [SerializeField]
        public List<IVariableBinding> Constants = new();

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Graph is null)
            {
                Variables.Clear();
                Constants.Clear();
                return;
            }

            Check(Graph.Variables, Variables);
            Check(Graph.Constants, Constants);

            void Check(List<IVariableBinding> source, List<IVariableBinding> target)
            {
                var sourceDict = source.ToDictionary(x => x.Key);
                List<IVariableBinding> pendingRemove = new();
                foreach (var item in target)
                {
                    var key = item?.Key;
                    if (string.IsNullOrEmpty(key))
                    {
                        pendingRemove.Add(item);
                        continue;
                    }

                    if (!sourceDict.Remove(key))
                        pendingRemove.Add(item);
                }

                foreach (var item in pendingRemove)
                    target.Remove(item);

                foreach (var itemMissing in sourceDict.Values)
                    target.Add(itemMissing);
            }
        }
#endif
    }
}
