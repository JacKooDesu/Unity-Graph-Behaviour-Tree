using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BehaviourTreeEditor.Runtime;
using NUnit.Framework;
using UExtensions;

namespace BehaviourTreeEditor.Test
{
    public class MethodCallTest
    {
        private delegate void RefStructDelegate(ref DataStruct s);

        private class MethodClass
        {
            public static int TestReturnMethod(int value) => value + 1;
            public static void TestRefMethod(ref int value) => value += 1;

            public static MethodCallRuntimeNode ReturnMethodNode(IValueSource source) =>
                new MethodCallRuntimeNode(
                    new(typeof(MethodClass), nameof(MethodClass.TestReturnMethod)),
                    new[] { source });

            public static MethodCallRuntimeNode RefMethodNode(IValueSource source) =>
                new MethodCallRuntimeNode(
                    new(typeof(MethodClass), nameof(MethodClass.TestRefMethod)),
                    new[] { source });
        }

        private class DataClass
        {
            public int Value;
        }

        private struct DataStruct
        {
            public int Value;
        }

        Dictionary<Type, object> _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new Dictionary<Type, object>();
        }

        [TearDown]
        public void TearDown()
        {
            _resolver = null;
        }

        [Test]
        public void TestMethodCall()
        {
            var source = new ClassFieldValueSource<DataClass>(
                new(typeof(DataClass), nameof(DataClass.Value)));

            var methodNode = MethodClass.ReturnMethodNode(source);

            var writeNode = new WriteValueRuntimeNode(
                source,
                methodNode);

            var sourceObj = new DataClass();

            _resolver[typeof(DataClass)] = sourceObj;

            var func = Expression.Lambda<Action>(writeNode.ToExpression(_resolver)).Compile();
            func();

            Assert.AreEqual(sourceObj.Value, 1);
        }

        [Test]
        public void TestRefMethodCall()
        {
            var source = new ClassFieldValueSource<DataClass>(
                new(typeof(DataClass), nameof(DataClass.Value)));

            var methodNode = MethodClass.RefMethodNode(source);

            var sourceObj = new DataClass();

            _resolver[typeof(DataClass)] = sourceObj;

            var func = Expression.Lambda<Action>(methodNode.ToExpression(_resolver)).Compile();
            func();

            Assert.AreEqual(sourceObj.Value, 1);
        }

        [Test]
        public void TestRefMethodCallStruct()
        {
            var source = new ClassFieldValueSource<DataStruct>(
                new(typeof(DataStruct), nameof(DataStruct.Value)));

            var methodNode = MethodClass.RefMethodNode(source);

            var param = Expression.Parameter(typeof(DataStruct).MakeByRefType());
            _resolver[typeof(DataStruct)] = param;

            var sourceObj = new DataStruct();
            var func = Expression.Lambda<RefStructDelegate>(
                methodNode.ToExpression(_resolver), param).Compile();
            func(ref sourceObj);

            Assert.AreEqual(sourceObj.Value, 1);
        }
    }
}
