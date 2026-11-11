using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [ScriptedImporter(1, Graph.EXTENSION_NAME)]
    internal class GraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<Graph>(ctx.assetPath);
            if (graph is null)
            {
                Debug.LogError($"Failed to load graph from path: {ctx.assetPath}");
                return;
            }
            // 觸發更新 context variables 綁定
            graph.OnEnable();

            var runtimeAsset = ScriptableObject.CreateInstance<RuntimeGraph>();
            runtimeAsset.States = new List<StateRuntimeNode>();

            // 先將外層轉換成 State Tree
            var nodeIndexMap = ProcessStateGraph(ctx, graph, runtimeAsset);

            // State 名稱 - State 索引
            var stateNameIndexDict = runtimeAsset.States.ToDictionary(
                s => s.StateName, s => s.StateIndex);

            // 用於填充 Subgraph 內部節點索引，使用後須 Clear（減少不必要的 new）
            var tempNodeIndexMap = new Dictionary<INode, int>();
            // 對狀態內部進行轉換（Skip(1) 跳過 Root 節點）
            foreach (var (sn, index) in nodeIndexMap.Skip(1))
            {
                var runtimeNode = runtimeAsset.States[index];
                var nodes = (sn as ISubgraphNode).GetSubgraph().GetNodes();

                if (nodes.OfType<OnEnterNode>().FirstOrDefault() is StateEventNode onEnterNode)
                    ProcessSubGraph(ctx, onEnterNode, runtimeNode.OnEnter, stateNameIndexDict, tempNodeIndexMap);
                if (nodes.OfType<OnExitNode>().FirstOrDefault() is StateEventNode onExitNode)
                    ProcessSubGraph(ctx, onExitNode, runtimeNode.OnExit, stateNameIndexDict, tempNodeIndexMap);
                if (nodes.OfType<OnTickNode>().FirstOrDefault() is StateEventNode onTickNode)
                    ProcessSubGraph(ctx, onTickNode, runtimeNode.OnTick, stateNameIndexDict, tempNodeIndexMap);
            }

            // 設定 Variable Bindings
            graph.GetNodes()
                .OfType<VariableBindings>()
                .Cast<VariableBindings>()
                .ToList()
                .ForEach(b =>
                {
                    var list = b.IsConstant ? runtimeAsset.Constants : runtimeAsset.Variables;
                    list.AddRange(b.RuntimeBindings);
                });

            // 取得所有須注入的 Type
            runtimeAsset.RequiredResolveTypes = GetRequiredResolveTypes(runtimeAsset);

            ctx.AddObjectToAsset("RuntimeGraph", runtimeAsset);
            ctx.SetMainObject(runtimeAsset);

            RuntimeGraph.AssetUpdated(ctx.assetPath, runtimeAsset);
        }

        static Dictionary<INode, int> ProcessStateGraph(
             AssetImportContext ctx,
             Graph graph,
             RuntimeGraph runtimeAsset)
        {
            var nodeIndexMap = new Dictionary<INode, int>();

            var startNode = graph.GetNodes().OfType<StartNode>().FirstOrDefault();
            if (startNode is null)
            {
                Debug.LogError($"No start node found in graph: {ctx.assetPath}");
                return nodeIndexMap;
            }

            CreateRuntimeNodes(startNode, runtimeAsset.States, nodeIndexMap,
                (node, index) =>
                {
                    if (node is StartNode s)
                        return new StateRuntimeNode("Root", index);
                    if (node is ISubgraphNode sub)
                        return new StateRuntimeNode(sub.GetSubgraph().Name, index);
                    throw new NotImplementedException($"Node type {node.GetType()} is not supported.");
                });
            ConnectNodes(
                runtimeAsset.States,
                nodeIndexMap);
            // 綁定 State Node 的 Parent
            foreach (var sn in runtimeAsset.States)
                foreach (var child in sn.NextNodeIndices)
                    runtimeAsset.States[child].SetParentIndex(sn.StateIndex);
            // Root 沒有父節點
            runtimeAsset.States[0].SetParentIndex(-1);

            // 計算 State Node 的深度
            runtimeAsset.States[0].SetDepth(0);
            var queue = new Queue<StateRuntimeNode>();
            queue.Enqueue(runtimeAsset.States[0]);
            while (queue.TryDequeue(out var currentNode))
            {
                var newDepth = currentNode.Depth + 1;
                foreach (var child in currentNode.NextNodeIndices)
                {
                    runtimeAsset.States[child].SetDepth(newDepth);
                    queue.Enqueue(runtimeAsset.States[child]);
                }
            }

            return nodeIndexMap;
        }

        static void ProcessSubGraph(
            AssetImportContext ctx,
            INode startNode,
            List<CompilableNode> targetList,
            Dictionary<string, int> stateNameIndexDict,
            Dictionary<INode, int> tempNodeIndexMap)
        {
            if (startNode is null)
                return;

            CreateRuntimeNodes<CompilableNode>(
                startNode,
                targetList,
                tempNodeIndexMap,
                (node, index) => AutoTranslateToRuntimeNodes(node) as CompilableNode);
            ConnectNodes<CompilableNode>(
                targetList,
                tempNodeIndexMap);

            // 設定 TransitionStateRuntimeNode 的 TargetStateIndex
            foreach (var node in targetList)
            {
                if (node is not TransitionStateRuntimeNode transitionNode)
                    continue;

                transitionNode.SetTargetStateIndex(
                    stateNameIndexDict[transitionNode.TargetState]);
            }

            tempNodeIndexMap.Clear();
        }

        static void CreateRuntimeNodes<T>(
            INode start,
            List<T> nodeList,
            Dictionary<INode, int> nodeIndexMap,
            Func<INode, int, T> nodeTranslator)
            where T : RuntimeNode
        {
            var nodeQueue = new Queue<INode>();
            nodeQueue.Enqueue(start);

            List<IPort> tempPortList = new();
            while (nodeQueue.TryDequeue(out var currentNode))
            {
                if (nodeIndexMap.ContainsKey(currentNode)) continue;

                var runtimeNode = nodeTranslator(currentNode, nodeList.Count);
                if (runtimeNode == null) continue;
                nodeIndexMap[currentNode] = nodeList.Count;
                nodeList.Add(runtimeNode);

                var port = currentNode is Node ?
                    currentNode.GetFlowOutputPort() :
                    currentNode.GetOutputPort(0);

                if (port is null)
                    continue;

                port.GetConnectedPorts(tempPortList);
                foreach (var tempPort in tempPortList)
                    nodeQueue.Enqueue(tempPort.GetNode());

                tempPortList.Clear();
            }
        }

        static void ConnectNodes<T>(
            List<T> nodeList,
            Dictionary<INode, int> nodeIndexMap,
            Action<T> afterConnection = default)
            where T : RuntimeNode
        {
            List<IPort> tempPortList = new();

            foreach (var (k, v) in nodeIndexMap)
            {
                var runtimeNode = nodeList[v];
                var port = k is Node ?
                    k.GetFlowOutputPort() :
                    k.GetOutputPort(0);

                if (port is null) continue;
                if (!port.IsConnected) continue;

                port.GetConnectedPorts(tempPortList);
                foreach (var tempPort in tempPortList)
                {
                    if (nodeIndexMap.TryGetValue(tempPort.GetNode(), out var index))
                        runtimeNode.NextNodeIndices.Add(index);
                }
                afterConnection?.Invoke(runtimeNode);
                tempPortList.Clear();
            }
        }

        static RuntimeNode AutoTranslateToRuntimeNodes(INode root)
        {
            var runtimeNode = root is Node node ?
                node.Translate() :
                new UndefinedRuntimeNode();

            if (runtimeNode is UndefinedRuntimeNode)
                Debug.LogWarning($"Failed to translate node of type {root.GetType()}");

            return runtimeNode;
        }

        static List<SType> GetRequiredResolveTypes(RuntimeGraph runtimeAsset)
        {
            return runtimeAsset.GetAllCompilableNodes()
                .SelectMany(n => n.GetRequiredResolveTypes())
                .Distinct()
                .Select(x => new SType(x))
                .ToList();
        }

        static T GetInputPortValue<T>(IPort port)
        {
            T value = default;
            if (port.IsConnected)
            {
                switch (port.FirstConnectedPort.GetNode())
                {
                    case IVariableNode variableNode:
                        variableNode.Variable.TryGetDefaultValue<T>(out value);
                        break;
                    case IConstantNode constantNode:
                        constantNode.TryGetValue<T>(out value);
                        break;
                }
            }
            else
            {
                port.TryGetValue<T>(out value);
            }

            return value;

        }
    }
}
