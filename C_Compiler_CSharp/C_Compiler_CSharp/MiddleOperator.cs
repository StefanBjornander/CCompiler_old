namespace CCompiler {
  public enum MiddleOperator
   {AssignRegister, InspectRegister, StackTop,
    JumpRegister, SysCall, Interrupt,

    PushZero, PushOne, PushFloat, PopFloat, TopFloat, PopEmpty,

    Initializer, InitializerZero, AssignInitSize, Assign,

    LogicalOr, LogicalAnd,
    BitwiseNot, BitwiseOr, BitwiseXOr, BitwiseAnd,
    ShiftLeft, ShiftRight,

    Compare, Equal, NotEqual, LessThan,
    LessThanEqual, GreaterThan,
    GreaterThanEqual, //UnsignedLessThan,
    //UnsignedLessThanEqual, UnsignedGreaterThan,
    //UnsignedGreaterThanEqual,

    Carry, NotCarry, Case, CaseEnd, Jump,

    Index, Dot, Increment, Decrement,
    UnaryAdd, UnarySubtract, BinaryAdd, BinarySubtract,

    Multiply, Divide, Modulo,
    //UnsignedMultiply, UnsignedDivide, UnsignedModulo,

    Address, Dereference,

    PreCall, Call, PostCall, DecreaseStack,

    ParameterInitSize, Parameter,
    GetReturnValue, SetReturnValue, Return, Exit,

    IntegralToIntegral, IntegralToFloating, FloatingToIntegral,

    FunctionEnd, Empty};
}