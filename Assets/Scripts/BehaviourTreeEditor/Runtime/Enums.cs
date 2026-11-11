namespace BehaviourTreeEditor.Runtime
{
    public enum EConditionType
    {
        Equal,
        NotEqual,
    }

    public enum EEquality
    {
        Less,
        Greater,
        LessOrEqual,
        GreaterOrEqual,
        Equal,
    }

    public enum EValue
    {
        Bool,
        Int,
        Float,
        String,
        Vector3,
        Vector2
    }

    public enum ELogic
    {
        And,
        Or,
        Xor,
        Not,
    }

    public enum EOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
    }

    public enum NodeState
    {
        Running,
        Success,
        Failure,
    }
}