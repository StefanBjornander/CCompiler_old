using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class StaticExpression {
    public static Expression Binary(MiddleOperator middleOp,
                                    Expression leftExpression,
                                    Expression rightExpression) {
      Type leftType = leftExpression.Symbol.Type,
           rightType = rightExpression.Symbol.Type;
      object leftValue = leftExpression.Symbol.Value,
             rightValue = rightExpression.Symbol.Value;
    
      switch (middleOp) {
        case MiddleOperator.BinaryAdd: // &i + 2
          if ((leftValue is StaticAddress) && (rightValue is BigInteger)) {
            StaticAddress staticAddress = (StaticAddress) leftValue;
            int resultOffset =
              ((int) rightValue) * leftType.PointerType.Size();
            object resultValue =
              new StaticAddress(staticAddress.UniqueName,
                                staticAddress.Offset + resultOffset);
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }

          // 2 + &i
          if ((leftValue is BigInteger) && (rightValue is StaticAddress)) {
            StaticAddress staticAddress = (StaticAddress) rightValue;
            int resultOffset =
              ((int) leftValue) * rightType.PointerType.Size();
            object resultValue =
               new StaticAddress(staticAddress.UniqueName,
                                 staticAddress.Offset + resultOffset);
            Symbol resultSymbol =new Symbol(rightType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }

          if (leftExpression.Symbol.IsExternOrStatic() && leftType.IsArray()
              && (rightValue is BigInteger)) { // a + 2
            int resultOffset = ((int) rightValue) * leftType.ArrayType.Size();
            object resultValue =
              new StaticAddress(leftExpression.Symbol.UniqueName,
                                resultOffset);
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }

          if (leftExpression.Symbol.IsExternOrStatic() &&
              (leftValue is BigInteger) && rightType.IsArray()) { // 2 + a
            int resultOffset = ((int) leftValue) * rightType.ArrayType.Size();
            object resultValue =
              new StaticAddress(rightExpression.Symbol.UniqueName,
                                resultOffset);
            Symbol resultSymbol = new Symbol(rightType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }
          break;

        case MiddleOperator.BinarySubtract: // &i - 2
          if ((leftValue is StaticAddress) && (rightValue is BigInteger)) {
            StaticAddress staticAddress = (StaticAddress) leftValue;
            int resultOffset = ((int) leftValue) * leftType.PointerType.Size();
            object resultValue =
              new StaticAddress(staticAddress.UniqueName,
                                staticAddress.Offset - resultOffset);
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }

          if (leftExpression.Symbol.IsExternOrStatic() && leftType.IsArray()
              && (rightValue is BigInteger)) { // a - 2
            int resultOffset = ((int) rightValue) * leftType.ArrayType.Size();
            object resultValue =
              new StaticAddress(leftExpression.Symbol.UniqueName,
                                -resultOffset);
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }
          break;

        case MiddleOperator.Index:
          if (leftExpression.Symbol.IsExternOrStatic() && leftType.IsArray()
              && (rightValue is BigInteger)) { // a[2]
            int size = leftType.ArrayType.Size();
            int offset = ((int) ((BigInteger) rightValue)) * size;
            StaticValue resultValue =
              new StaticValue(leftExpression.Symbol.UniqueName, offset);
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            resultSymbol.Addressable = !leftExpression.Symbol.IsRegister() &&
                                       !leftType.ArrayType.IsBitfield();
            resultSymbol.Assignable =
              !leftType.ArrayType.IsConstantRecursive() &&
              !leftType.ArrayType.IsArrayFunctionOrString();
            return (new Expression(resultSymbol, null, null));
          }
          else if ((leftValue is BigInteger) && rightType.IsArray() &&
                   rightExpression.Symbol.IsExternOrStatic()) { // [2]a
            int size = rightType.ArrayType.Size();
            int offset = ((int) ((BigInteger) leftValue)) * size;
            StaticValue resultValue =
              new StaticValue(rightExpression.Symbol.UniqueName, offset);
            Symbol resultSymbol = new Symbol(rightType, resultValue);
            resultSymbol.Addressable = !leftExpression.Symbol.IsRegister() &&
                                       !leftType.ArrayType.IsBitfield();
            resultSymbol.Assignable =
              !leftType.ArrayType.IsConstantRecursive() &&
              !leftType.ArrayType.IsArrayFunctionOrString();
            return (new Expression(resultSymbol, null, null));
          }
          break;

        case MiddleOperator.Dot:
          if (leftExpression.Symbol.IsExternOrStatic()) {
            object resultValue =
              new StaticValue(leftExpression.Symbol.UniqueName,
                              rightExpression.Symbol.Offset); // s.i*/
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }
          break;
      }
    
      return null;
    }  

    public static Symbol Unary(MiddleOperator middleOp, Symbol symbol) {
      Type type = symbol.Type;
    
      switch (middleOp) {
        case MiddleOperator.Address:
          if (symbol.Value is StaticValue) { // &a[i], &s.i
            StaticValue staticValue = (StaticValue) symbol.Value;
            StaticAddress staticAddress =
              new StaticAddress(staticValue.UniqueName, staticValue.Offset);
            return (new Symbol(new Type(type), staticAddress));
          }
          else if (symbol.IsExternOrStatic()) { // &i
            StaticAddress staticAddress =
              new StaticAddress(symbol.UniqueName, 0);
            return (new Symbol(new Type(type), staticAddress));
          }
          break;
      }

      return null;
    }  
  }
}
