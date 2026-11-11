using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BehaviourTreeEditor.Editor;
using BehaviourTreeEditor.Runtime;
using NUnit.Framework;
using UExtensions;
using UnityEngine;
using UnityEngine.TestTools;

namespace BehaviourTreeEditor.Test
{
    public class ConditionRuntimeNodeTest
    {
        [Test]
        public void TestExpression()
        {
            var obj = new TestClass();
            Dictionary<Type, object> resolver = new()
            {
                { typeof(TestClass), obj }
            };
            var fieldData = new SField(typeof(TestClass), nameof(TestClass.IntValue));

            var conditionNode = new ConditionRuntimeNode<int>(
                EConditionType.Equal,
                new ConstantValueSource<int>(10),
                new ClassFieldValueSource<int>(fieldData)
            );

            var func = Expression.Lambda<Func<bool>>(conditionNode.ToExpression(resolver)).Compile();

            Assert.That(func(), Is.True);
            obj.IntValue = 20;
            Assert.That(func(), Is.False);
        }

        public class TestClass
        {
            public int IntValue = 10;
        }
    }
}
