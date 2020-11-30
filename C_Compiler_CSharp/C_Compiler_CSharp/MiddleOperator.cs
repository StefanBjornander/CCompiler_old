namespace CCompiler {
  public enum MiddleOperator {Goto, AssignRegister, Compare,
                              InspectFlagbyte, InspectRegister, SysCall,
                              JumpRegister, Interrupt, Expression, Declaration,
                              PushZero, PushOne, PushFloat, PopFloat, TopFloat,
                              PopEmpty, Comma, AssignInitSize, Assign, LogicalOr, LogicalAnd,
                              BitwiseOr, BitwiseXOr, BitwiseAnd,
                              Equal, NotEqual, SignedLessThan,
                              SignedLessThanEqual, SignedGreaterThan,
                              SignedGreaterThanEqual, UnsignedLessThan,
                              UnsignedLessThanEqual, UnsignedGreaterThan,
                              UnsignedGreaterThanEqual, ShiftLeft,
                              ShiftRight, BinaryAdd, BinarySubtract,
                              SignedMultiply, SignedDivide, SignedModulo,
                              UnsignedMultiply, UnsignedDivide, UnsignedModulo,
                              UnaryAdd, UnarySubtract, Carry, NotCarry,
                              LogicalNot, BitwiseNot, Address, Dereference,
                              Call, PostCall, DecreaseStack,
                              ParameterInitSize, Parameter, Empty, SetReturnValue,
                              Return, Exit, Increment, Decrement, PreCall,
                              FunctionEnd, Conditional, GetReturnValue,
                              IntegralToIntegral, IntegralToFloating, FloatingToIntegral,
                              ArrayToPointer, FunctionToPointer, StringToPointer,
                              ValueOffset, AddressOffset, Case, CaseEnd, StackTop,
                              Index, Dot, Arrow, Variable, Value, Initializer, InitializerZero};
}