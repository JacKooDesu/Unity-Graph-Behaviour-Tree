using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UNode = Unity.GraphToolkit.Editor.Node;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal abstract class Node : UNode
    {
        internal virtual RuntimeNode Translate() => new UndefinedRuntimeNode();
    }

    internal interface IValueSourceConvertable
    {
        IValueSource ToValueSource();
        IEnumerable<IPort> OutputPorts();
    }
}
