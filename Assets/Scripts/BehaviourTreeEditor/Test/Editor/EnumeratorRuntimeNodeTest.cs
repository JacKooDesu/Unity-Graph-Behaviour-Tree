using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BehaviourTreeEditor.Runtime;
using NUnit.Framework;

namespace BehaviourTreeEditor.Test
{
    public class EnumeratorRuntimeNodeTest
    {
        [Test]
        public void TestArray()
        {
            var zero = new ConstantValueSource<int>(0);
            int[] array = new[] { 1, 2, 3 };
            var arrayRef = new ConstantValueSource<int[]>(array);

            var enumeratorNode = new EnumeratorRuntimeNode(arrayRef);
            var iter = new EnumeratorRuntimeNode.EnumerableScopeValueSource();
            var writeNode = new WriteValueRuntimeNode(iter, zero);

            Test(enumeratorNode, writeNode);

            foreach (var item in array)
                Assert.IsTrue(item == 0);
        }

        [Test]
        public void TestList()
        {
            var zero = new ConstantValueSource<int>(0);
            List<int> list = new() { 1, 2, 3 };
            var listRef = new ConstantValueSource<List<int>>(list);

            var enumeratorNode = new EnumeratorRuntimeNode(listRef);
            var iter = new EnumeratorRuntimeNode.EnumerableScopeValueSource();
            var writeNode = new WriteValueRuntimeNode(iter, zero);

            Test(enumeratorNode, writeNode);

            foreach (var item in list)
                Assert.IsTrue(item == 0);
        }

        [Test]
        public void TestEnumerable()
        {
            var zero = new ConstantValueSource<int>(0);
            var enumerable = EnumerableTester();
            var enumerableSource = new ConstantValueSource<IEnumerable<int>>(enumerable);
            var enumeratorNode = new EnumeratorRuntimeNode(enumerableSource);
            var iter = new EnumeratorRuntimeNode.EnumerableScopeValueSource();

            var testInt = 0;
            var actionNode = new TestActionNode<int>(
                i => Assert.AreEqual(i, testInt++),
                iter);
            Test(enumeratorNode, actionNode);
        }

        IEnumerable<int> EnumerableTester()
        {
            for (int i = 0; i < 3; ++i)
                yield return i;
        }

        void Test(
            EnumeratorRuntimeNode enumeratorNode,
            CompilableNode actionNode)
        {
            Dictionary<Type, object> resolver = new();
            var nodes = new List<CompilableNode>();
            resolver[typeof(List<CompilableNode>)] = nodes;

            nodes.Add(enumeratorNode);

            nodes.Add(actionNode);

            enumeratorNode.NextNodeIndices.Add(nodes.IndexOf(actionNode));
            var func = Expression.Lambda<Action>(enumeratorNode.ToExpression(resolver)).Compile();
            func();
        }
    }
}
