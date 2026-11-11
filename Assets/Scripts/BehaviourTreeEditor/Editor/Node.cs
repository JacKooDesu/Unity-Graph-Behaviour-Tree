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

    [Serializable]
    public class TestType
    {
        public int ValueInt1;
        public int ValueInt2;
        public int ValueInt3;
        public float ValueFloat1;
        public enum ETest { A, B, C }
        public ETest ValueEnum;
        public Vector3 ValueVector3;
        public Vector2 ValueVector2;
        public TestTypeB ValueTestTypeB = new();
    }

    [Serializable]
    public class TestTypeB : ISTypeProvider
    {
        public int ValueInt1;
        public float ValueFloat1;
        public static int ValueIntStatic = 5;
        public static int MethodIntStatic() => ValueIntStatic;
    }

    [Serializable]
    public class CharacterStateMachineContext : ISTypeProvider
    {
        public int Step;
        public float MoveSpeed;
        public float JumpForce;

        public int State;
        public bool InputJump;
        public float InputDirection;
        public bool IsGround;
        public float VelocityX;
        public float VelocityY;

        public Rigidbody2D Rigidbody2D;
        public GameObject GameObject;
        public CharacterStateMachineContext Self;
        public TestType TestType = new();

        public void TestMethod()
        {

        }
        public int TestMethodInt(float f) => (int)f;
    }
    [Serializable]
    public class CharacterStateWriter : ISTypeProvider
    {
        public Vector3 Linear;
    }
}
