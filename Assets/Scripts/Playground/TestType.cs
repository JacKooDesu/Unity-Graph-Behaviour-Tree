using System;
using UExtensions;
using UnityEngine;

[Serializable]
public class TestType
{
    public int ValueInt1;
    public int ValueInt2;
    public int ValueInt3;
    public float PFloat { get; set; }
    public float ValueFloat1;
    public enum ETest { A, B, C }
    public ETest ValueEnum;
    public Vector3 ValueVector3;
    public Vector2 ValueVector2;
    public TestTypeB ValueTestTypeB { get; set; } = new();
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

    public void TestMethod(CharacterStateMachineContext param_CharacterStateMachineContext_0)
    {
        ref var local_0 = ref param_CharacterStateMachineContext_0.GameObject;
        ref var local_3 = ref param_CharacterStateMachineContext_0.GameObject;
        var local_6 = new Vector3(1f, 0f, 0f);
        var local_7 = local_3.transform.position + local_6;
        local_0.transform.position = local_7;
    }
    public int TestMethodInt(float f) => (int)f;
}

[Serializable]
public class CharacterStateWriter : ISTypeProvider
{
    public Vector3 Linear;
}
