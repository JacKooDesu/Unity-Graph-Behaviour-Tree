using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BehaviourTreeEditor.Runtime;
using NUnit.Framework;
using UExtensions;

namespace BehaviourTreeEditor.Test
{
    public class WriteValueTest
    {
        private delegate void TestDelegate(ref TestStruct s);
        private delegate void TestInnerDelegate(ref TestStruct3 s);

        private struct TestStruct
        {
            public int Value;
        }

        private struct TestStruct2
        {
            public TestStruct Inner;
        }

        private struct TestStruct3
        {
            public TestStruct2 Inner;
        }

        [Test]
        public void TestWriteStruct()
        {
            var source = new ClassFieldValueSource<TestStruct>(
                new SField(typeof(TestStruct), nameof(TestStruct.Value)));
            var valueSource = new ConstantValueSource<int>(20);
            var writeNode = new WriteValueRuntimeNode(source, valueSource);

            // inject parameter
            var paramExp = Expression.Parameter(typeof(TestStruct).MakeByRefType());
            var resolver = new Dictionary<Type, object>();
            resolver[typeof(TestStruct)] = paramExp;

            // execute
            var target = new TestStruct()
            {
                Value = 0
            };
            var func = Expression.Lambda<TestDelegate>(
                writeNode.ToExpression(resolver),
                paramExp).Compile();
            func(ref target);

            Assert.AreEqual(20, target.Value);
        }

        [Test]
        public void TestWriteInnerStruct()
        {
            var target = new TestStruct3
            {
                Inner = new()
                {
                    Inner = new()
                    {
                        Value = 0
                    }
                }
            };

            var source = new ClassFieldValueSource(
                new SField(typeof(TestStruct3), nameof(TestStruct3.Inner)));
            var inner = new InstanceFieldValueSource(
                source, new SField(typeof(TestStruct2), nameof(TestStruct2.Inner)));
            var intSource = new InstanceFieldValueSource(
                inner, new SField(typeof(TestStruct), nameof(TestStruct.Value)));
            var valueSource = new ConstantValueSource<int>(20);
            var writeNode = new WriteValueRuntimeNode(intSource, valueSource);

            // inject parameter
            var paramExp = Expression.Parameter(typeof(TestStruct3).MakeByRefType());
            var resolver = new Dictionary<Type, object>();
            resolver[typeof(TestStruct3)] = paramExp;

            // execute
            var func = Expression.Lambda<TestInnerDelegate>(
                writeNode.ToExpression(resolver),
                paramExp).Compile();
            func(ref target);

            Assert.AreEqual(20, target.Inner.Inner.Value);
        }
    }
}
