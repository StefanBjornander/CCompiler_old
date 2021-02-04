using System.Numerics;

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
        case MiddleOperator.Add: // &i + 2, a + 2
          if (((leftValue is StaticAddress) ||
               (leftExpression.Symbol.IsExternOrStatic() && leftType.IsArray())) &&
              (rightValue is BigInteger)) {
            return GenerateAddition(leftExpression.Symbol, (BigInteger) rightValue);
          }
          else if ((leftValue is BigInteger) && // 2 + &i, 2 + a
                   ((rightValue is StaticAddress) || (rightType.IsArray() &&
                     rightExpression.Symbol.IsExternOrStatic()))) {
            return GenerateAddition(rightExpression.Symbol, (BigInteger) leftValue);
          }
          break;

        case MiddleOperator.Subtract: // &i - 2, a - 2
          if (((leftValue is StaticAddress) ||
               (leftExpression.Symbol.IsExternOrStatic() && leftType.IsArray())) &&
              (rightValue is BigInteger)) {
            return GenerateAddition(leftExpression.Symbol, -((BigInteger) rightValue));
          }
          break;

        case MiddleOperator.Index:
          if (((leftValue is StaticAddress) || (leftType.IsArray() &&
               leftExpression.Symbol.IsExternOrStatic())) && 
              (rightValue is BigInteger)) { // a[2]
            return GenerateIndex(leftExpression.Symbol,
                                 (BigInteger) rightValue);
          }
          else if ((leftValue is BigInteger) && ((rightType.IsArray() &&
                   rightExpression.Symbol.IsExternOrStatic()) ||
                   (rightValue is StaticAddress))) {
            return GenerateIndex(rightExpression.Symbol,
                                 (BigInteger) leftValue);
          }
          break;

        case MiddleOperator.Dot:
          if (leftExpression.Symbol.IsExternOrStatic()) {
            object resultValue =
              new StaticValue(leftExpression.Symbol.UniqueName,
                              rightExpression.Symbol.Offset); // s.i
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }
          break;
      }
    
      return null;
    }  

    private static Expression GenerateAddition(Symbol symbol,
                                               BigInteger value) {
      int offset = ((int) value) * symbol.Type.PointerOrArrayType.Size();
      StaticAddress resultValue;

      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        resultValue = new StaticAddress(staticAddress.UniqueName,
                                        staticAddress.Offset + offset);
      }
      else {
        resultValue = new StaticAddress(symbol.UniqueName, offset);
      }

      Symbol resultSymbol = new Symbol(symbol.Type, resultValue);
      return (new Expression(resultSymbol, null, null));
    }

    private static Expression GenerateIndex(Symbol symbol,
                                            BigInteger value) {
      int offset = ((int) value) * symbol.Type.ArrayType.Size();
      StaticValue resultValue;

      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        resultValue = new StaticValue(staticAddress.UniqueName,
                                      staticAddress.Offset + offset);
      }
      else {
        resultValue = new StaticValue(symbol.UniqueName, offset);
      }

      Symbol resultSymbol = new Symbol(symbol.Type, resultValue);
      return (new Expression(resultSymbol, null, null));
    }

    public static Expression Unary(MiddleOperator middleOp, Expression expression) {
      Symbol symbol = expression.Symbol;
    
      switch (middleOp) {
        case MiddleOperator.Address:
          if (symbol.Value is StaticValue) { // &a[i], &s.i
            StaticValue staticValue = (StaticValue) symbol.Value;
            StaticAddress staticAddress =
              new StaticAddress(staticValue.UniqueName, staticValue.Offset);
            Symbol resultSymbol =
              new Symbol(new Type(symbol.Type), staticAddress);
            return (new Expression(resultSymbol, null, null));
          }
          else if (symbol.IsExternOrStatic()) { // &i
            StaticAddress staticAddress =
              new StaticAddress(symbol.UniqueName, 0);
            Symbol resultSymbol =
              new Symbol(new Type(symbol.Type), staticAddress);
            return (new Expression(resultSymbol, null, null));
          }
          break;

        /*case MiddleOperator.Dereference:
          if (symbol.Value is StaticAddress) { // *&a[i], *&s.i
            StaticAddress staticAddress = (StaticAddress) symbol.Value;
            StaticValue staticValue =
              new StaticValue(staticAddress.UniqueName, staticAddress.Offset);
            return (new Symbol(new Type(symbol.Type), staticValue));
          }
          else if (symbol.IsExternOrStatic()) { // *&i
            StaticValue staticValue = new StaticValue(symbol.UniqueName, 0);
            return (new Symbol(new Type(symbol.Type), staticValue));
          }
          break;
        }*/
      }

      return null;
    }
  }
}