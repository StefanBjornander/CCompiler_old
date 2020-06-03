using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class ConstantExpression {
    public static bool IsConstant(Expression expression) {
      Symbol symbol = expression.Symbol;
      Type type = symbol.Type;
      return (type.IsLogical() && ((symbol.TrueSet.Count == 0) ||
              (symbol.FalseSet.Count == 0))) ||
             (type.IsIntegralOrPointer() && (symbol.Value != null)) ||
             (type.IsFloating() && (symbol.Value != null));
    }

    public static bool IsTrue(Expression expression) {
      return ((expression.Symbol.Value is BigInteger) &&
              !((BigInteger) expression.Symbol.Value).IsZero) ||
             ((expression.Symbol.Value is decimal) &&
              !((decimal) expression.Symbol.Value).Equals((decimal) 0)) ||
             (expression.Symbol.TrueSet.Count > 0);
    }

    public static Expression Relation(MiddleOperator middleOp,
                                      Expression leftExpression,
                                      Expression rightExpression) {
      if (!IsConstant(leftExpression) || !IsConstant(rightExpression)) {
        return null;
      }
      else if (leftExpression.Symbol.Type.IsFloating() ||
               rightExpression.Symbol.Type.IsFloating()) {
        return RelationFloating(middleOp, leftExpression, rightExpression);
      }
      else {
        return RelationIntegral(middleOp, leftExpression, rightExpression);
      }
    }

    private static Expression LogicalToIntegral(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return Cast(expression, Type.SignedIntegerType);
      }

      return expression;
    }

    private static Expression RelationIntegral(MiddleOperator middleOp,
                                               Expression leftExpression,
                                               Expression rightExpression) {
      leftExpression = LogicalToIntegral(leftExpression);
      rightExpression = LogicalToIntegral(leftExpression);

      BigInteger leftValue = (BigInteger) leftExpression.Symbol.Value,
                 rightValue = (BigInteger) rightExpression.Symbol.Value;

      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Goto);
      codeList.Add(jumpCode);
      ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
      jumpSet.Add(jumpCode);

      bool resultValue = false;
      switch (middleOp) {
        case MiddleOperator.Equal:
          resultValue = (leftValue == rightValue);
          break;

        case MiddleOperator.NotEqual:
          resultValue = (leftValue != rightValue);
          break;

        case MiddleOperator.SignedLessThan:
        case MiddleOperator.UnsignedLessThan:
          resultValue = (leftValue < rightValue);
          break;

        case MiddleOperator.SignedLessThanEqual:
        case MiddleOperator.UnsignedLessThanEqual:
          resultValue = (leftValue <= rightValue);
          break;

        case MiddleOperator.SignedGreaterThan:
        case MiddleOperator.UnsignedGreaterThan:
          resultValue = (leftValue > rightValue);
          break;

        case MiddleOperator.SignedGreaterThanEqual:
        case MiddleOperator.UnsignedGreaterThanEqual:
          resultValue = (leftValue >= rightValue);
          break;
      }

      Symbol symbol = resultValue ? (new Symbol(jumpSet, null))
                                  : (new Symbol(null, jumpSet));
      return (new Expression(symbol, null, codeList));
    }
      
    private static Expression LogicalToFloating(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return Cast(expression, Type.DoubleType);
      }

      return expression;
    }

    private static Expression RelationFloating(MiddleOperator middleOp,
                                               Expression leftExpression,
                                               Expression rightExpression) {
      leftExpression = LogicalToFloating(leftExpression);
      rightExpression = LogicalToFloating(leftExpression);

      decimal leftValue;      
      if (leftExpression.Symbol.Value is BigInteger) {
        leftValue = (decimal) ((BigInteger) leftExpression.Symbol.Value);
      }
      else {
        leftValue = (decimal) leftExpression.Symbol.Value;
      }

      decimal rightValue;
      if (rightExpression.Symbol.Value is BigInteger) {
        rightValue = (decimal) ((BigInteger) rightExpression.Symbol.Value);
      }
      else {
        rightValue = (decimal) rightExpression.Symbol.Value;
      }

      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Goto);
      codeList.Add(jumpCode);
      ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
      jumpSet.Add(jumpCode);

      bool resultValue = false;
      switch (middleOp) {
        case MiddleOperator.Equal:
          resultValue = (leftValue == rightValue);
          break;

        case MiddleOperator.NotEqual:
          resultValue = (leftValue != rightValue);
          break;

        case MiddleOperator.SignedLessThan:
          resultValue = (leftValue < rightValue);
          break;

        case MiddleOperator.SignedLessThanEqual:
          resultValue = (leftValue <= rightValue);
          break;

        case MiddleOperator.SignedGreaterThan:
          resultValue = (leftValue > rightValue);
          break;

        case MiddleOperator.SignedGreaterThanEqual:
          resultValue = (leftValue >= rightValue);
          break;
      }

      Symbol symbol = resultValue ? (new Symbol(jumpSet, null))
                                  : (new Symbol(null, jumpSet));
      return (new Expression(symbol, null, codeList));
    }

    private static Expression ToLogical(Expression expression) {
      if (!expression.Symbol.Type.IsLogical()) {
        return Cast(expression, Type.LogicalType);
      }

      return expression;
    }

    public static Expression Logical(MiddleOperator middleOp,
                                     Expression leftExpression,
                                     Expression rightExpression) {
      if (!IsConstant(leftExpression) || !IsConstant(rightExpression)) {
        return null;
      }

      Expression leftLogicalExpression = ToLogical(leftExpression),
                 rightLogicalExpression = ToLogical(rightExpression);
      bool leftValue = leftLogicalExpression.Symbol.TrueSet.Count > 0,
           rightValue = rightLogicalExpression.Symbol.TrueSet.Count > 0;

      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Goto);
      codeList.Add(jumpCode);
      ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
      jumpSet.Add(jumpCode);

      bool resultValue = false;
      switch (middleOp) {
        case MiddleOperator.LogicalAnd:
          resultValue = leftValue && rightValue;
          break;

        case MiddleOperator.LogicalOr:
          resultValue = leftValue || rightValue;
          break;
      }

      Symbol symbol = resultValue ? (new Symbol(jumpSet, null))
                                  : (new Symbol(null, jumpSet));
      return (new Expression(symbol, null, codeList));
    }
  
    public static Expression Arithmetic(MiddleOperator middleOp,
                                        Expression leftExpression,
                                        Expression rightExpression) {
      if (!IsConstant(leftExpression) || !IsConstant(rightExpression)) {
        return null;
      }
      else if (leftExpression.Symbol.Type.IsFloating() ||
               rightExpression.Symbol.Type.IsFloating()) {
        return ArithmeticFloating(middleOp, leftExpression, rightExpression);
      }
      else {
        return ArithmeticIntegral(middleOp, leftExpression, rightExpression);
      }
    }

    private static Expression ArithmeticIntegral(MiddleOperator middleOp,
                                                 Expression leftExpression,
                                                 Expression rightExpression){
      leftExpression = LogicalToIntegral(leftExpression);
      rightExpression = LogicalToIntegral(rightExpression);
      Symbol symbol = ArithmeticIntegral(middleOp, leftExpression.Symbol,
                                         rightExpression.Symbol);
      return (new Expression(symbol, null, null));
    }
    
    public static Symbol ArithmeticIntegral(MiddleOperator middleOp,
                                            Symbol leftSymbol,
                                            Symbol rightSymbol) {
      Type leftType = leftSymbol.Type,
           rightType = rightSymbol.Type;
           
      BigInteger leftValue = (BigInteger) leftSymbol.Value,
                 rightValue = (BigInteger) rightSymbol.Value,
                 resultValue = 0;

      switch (middleOp) {
        case MiddleOperator.BinaryAdd:
          if (leftType.IsPointerOrArray()) {
            resultValue = leftValue +
                          (rightValue * leftType.PointerOrArrayType.Size());
          }
          else if (leftType.IsPointerOrArray()) {
            resultValue = (leftValue * rightType.PointerOrArrayType.Size()) +
                          rightValue;
          }
          else {
            resultValue = leftValue + rightValue;
          }
          break;
        
        case MiddleOperator.BinarySubtract:
          if (leftType.IsPointerOrArray() && rightType.IsPointerOrArray()) {
            resultValue = (leftValue - rightValue) /
                          leftType.PointerOrArrayType.Size();
          }
          else if (leftType.IsPointerOrArray()) {
            resultValue = leftValue -
                          (rightValue * leftType.PointerOrArrayType.Size());
          }
          else {
            resultValue = leftValue - rightValue;
          }
          break;

        case MiddleOperator.SignedMultiply:
        case MiddleOperator.UnsignedMultiply:
          resultValue = leftValue * rightValue;
          break;
        
        case MiddleOperator.SignedDivide:
        case MiddleOperator.UnsignedDivide:
          resultValue = leftValue / rightValue;
          break;
        
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedModulo:
          resultValue = leftValue % rightValue;
          break;

        case MiddleOperator.BitwiseOr:
          resultValue = leftValue | rightValue;
          break;
        
        case MiddleOperator.BitwiseXOr:
          resultValue = leftValue ^ rightValue;
          break;
        
        case MiddleOperator.BitwiseAnd:
          resultValue = leftValue & rightValue;
          break;
        
        case MiddleOperator.ShiftLeft:
          resultValue = leftValue << ((int) rightValue);
          break;
        
        case MiddleOperator.ShiftRight:
          resultValue = leftValue >> ((int) rightValue);
          break;
        
      }
    
      Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);
      return (new Symbol(maxType, resultValue));
    }

    private static Expression ArithmeticFloating(MiddleOperator middleOp,
                                                 Expression leftExpression,
                                                 Expression rightExpression) {
      leftExpression = LogicalToFloating(leftExpression);
      rightExpression = LogicalToFloating(rightExpression);
      decimal leftValue, rightValue, resultValue = 0;

      if (leftExpression.Symbol.Type.IsFloating()) {
        leftValue = (decimal) leftExpression.Symbol.Value;
      }
      else {
        leftValue = (decimal) ((BigInteger) leftExpression.Symbol.Value);
      }

      if (rightExpression.Symbol.Type.IsFloating()) {
        rightValue = (decimal) rightExpression.Symbol.Value;
      }
      else {
        rightValue = (decimal) ((BigInteger) rightExpression.Symbol.Value);
      }
  
      switch (middleOp) {
        case MiddleOperator.BinaryAdd:
          resultValue = leftValue + rightValue;
          break;
        
        case MiddleOperator.BinarySubtract:
          resultValue = leftValue - rightValue;
          break;
        
        case MiddleOperator.SignedMultiply:
          resultValue = leftValue * rightValue;
          break;
        
        case MiddleOperator.SignedDivide:
          resultValue = leftValue / rightValue;
          break;
      }

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                      rightExpression.Symbol.Type);
      Symbol resultSymbol = new Symbol(maxType, resultValue);
      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.PushFloat,
                                        resultSymbol);
      return (new Expression(resultSymbol, null, codeList));
    }
  
    public static Expression LogicalNot(Expression expression) {
      if (IsConstant(expression)) {
        expression = ToLogical(expression);
        List<MiddleCode> codeList = new List<MiddleCode>();
        MiddleCode jumpCode = new MiddleCode(MiddleOperator.Goto);
        codeList.Add(jumpCode);
        ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
        jumpSet.Add(jumpCode);
        bool isTrue = (expression.Symbol.TrueSet.Count > 0);
        Symbol symbol = isTrue ? (new Symbol(jumpSet, null))
                               : (new Symbol(null, jumpSet));
        return (new Expression(symbol, null, codeList));
      }

      return null;
    }

    public static Expression Arithmetic(MiddleOperator middleOp,
                                        Expression expression) {
      if (!IsConstant(expression)) {
        return null;
      }
      else if (expression.Symbol.Type.IsFloating()) {
        return ArithmeticFloating(middleOp, expression);
      }
      else {
        return ArithmeticIntegral(middleOp, expression);
      }
    }
  
    private static Expression ArithmeticIntegral(MiddleOperator middleOp,
                                                 Expression expression) {
      expression = LogicalToIntegral(expression);
      BigInteger value = (BigInteger) expression.Symbol.Value, resultValue = 0;
    
      switch (middleOp) {
        case MiddleOperator.UnaryAdd:
          resultValue = value;
          break;
        
        case MiddleOperator.UnarySubtract:
          resultValue = -value;
          break;

        case MiddleOperator.BitwiseNot:
          resultValue = ~value;
          break;
      }

      Symbol resultSymbol = new Symbol(expression.Symbol.Type, resultValue);
      return (new Expression(resultSymbol, null, null));
    }

    private static Expression ArithmeticFloating(MiddleOperator middleOp,
                                                 Expression expression) {
      expression = LogicalToFloating(expression);
      decimal value = (decimal) expression.Symbol.Value;
      Symbol resultSymbol = null;

      switch (middleOp) {
        case MiddleOperator.UnaryAdd:
          resultSymbol = new Symbol(expression.Symbol.Type, value);
          break;
        
        case MiddleOperator.UnarySubtract:
          resultSymbol = new Symbol(expression.Symbol.Type, -value);
          break;
      }

      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.PushFloat,
                                        resultSymbol);
      return (new Expression(resultSymbol, null, codeList));
    }

    public static Expression Cast(Expression fromExpression, Type toType) {
      if (!IsConstant(fromExpression)) {
        return null;
      }

      Symbol fromSymbol = fromExpression.Symbol;
      Type fromType = fromSymbol.Type;
      object fromValue = fromSymbol.Value;

      if (fromType.Equals(toType)) {
        return fromExpression;
      }
      else if (((fromType.IsIntegralArrayOrPointer() &&
                 toType.IsIntegralArrayOrPointer()) ||
               (fromType.IsFloating() && toType.IsFloating())) &&
                (fromType.Size() == toType.Size())) {
        Symbol toSymbol = new Symbol(toType, fromValue);
        return (new Expression(toSymbol, fromExpression.ShortList,
                               fromExpression.LongList));
      }
      else if (fromType.IsLogical() && toType.IsIntegralArrayOrPointer()) {
        if ((fromSymbol.TrueSet.Count > 0) &&
            (fromSymbol.FalseSet.Count == 0)) {
          Symbol toSymbol = new Symbol(toType, (BigInteger) 1);
          return (new Expression(toSymbol, null, null));
        }
        else if ((fromSymbol.TrueSet.Count == 0) &&
                 (fromSymbol.FalseSet.Count > 0)) {
          Symbol toSymbol = new Symbol(toType, (BigInteger) 0);
          return (new Expression(toSymbol, null, null));
        }
      }
      else if (((fromType.IsIntegralArrayOrPointer() ||
                (fromValue is BigInteger)) ||
               (fromType.IsFloating() && (fromValue is decimal))) &&
                toType.IsLogical()) {
        List<MiddleCode> codeList = new List<MiddleCode>();
        MiddleCode jumpCode = new MiddleCode(MiddleOperator.Goto);
        codeList.Add(jumpCode);
        ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
        jumpSet.Add(jumpCode);

        Symbol toSymbol;
        if (fromValue.Equals(BigInteger.Zero) ||
            fromValue.Equals((decimal) 0)) {
          toSymbol = new Symbol(null, jumpSet);
        }
        else {
          toSymbol = new Symbol(jumpSet, null);
        }

        return (new Expression(toSymbol, null, codeList));
      }
      else if ((fromValue is BigInteger) || (fromValue is decimal)) {
        Symbol toSymbol = null;
        if (fromType.IsIntegralArrayOrPointer() &&
            toType.IsIntegralArrayOrPointer()) {
          toSymbol = new Symbol(toType, fromValue);
        }
        else if (fromType.IsIntegralArrayOrPointer() && toType.IsFloating()) {
          toSymbol = new Symbol(toType, (decimal) ((BigInteger) fromValue));
        }
        else if (fromType.IsFloating() && toType.IsIntegralArrayOrPointer()) {
          toSymbol = new Symbol(toType, (BigInteger) ((decimal) fromValue));
        }
        else if (fromType.IsFloating() && toType.IsFloating()) {
          toSymbol = new Symbol(toType, fromValue);
        }
        else {
          Assert.Error(fromType + " to " + toType, Message.Invalid_type_cast);
        }

        List<MiddleCode> codeList = new List<MiddleCode>();
        if (toType.IsFloating()) {
          MiddleCodeGenerator.
            AddMiddleCode(codeList, MiddleOperator.PushFloat, toSymbol);
        }

        return (new Expression(toSymbol, null, codeList));
      }

      return null;
    }
    
    public static StaticSymbol Value(Symbol symbol) {
      return Value(symbol.UniqueName, symbol.Type, symbol.Value);
    }

    public static StaticSymbol Value(string uniqueName, Type type,
                                     object value) {
      List<MiddleCode> middleCodeList = new List<MiddleCode>();
      
      if (value != null) {
        middleCodeList.Add(new MiddleCode(MiddleOperator.Init,
                                          type.Sort, value));
      }
      else {
        middleCodeList.Add(new MiddleCode(MiddleOperator.InitZero,
                                          type.Size()));
      }

      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AssemblyCodeGenerator.GenerateAssembly(middleCodeList,
                                             assemblyCodeList);

      if (Start.Windows) {
        List<byte> byteList = new List<byte>();
        IDictionary<int,string> accessMap = new Dictionary<int,string>();
        AssemblyCodeGenerator.
          GenerateTargetWindows(assemblyCodeList, byteList,
                                accessMap, null, null);
        return (new StaticSymbolWindows(uniqueName, byteList, accessMap));
      }
      
      if (Start.Linux) {
        List<string> textList = new List<string>();
        textList.Add("\n" + uniqueName + ":");
        ISet<string> externSet = new HashSet<string>();
        GenerateStaticInitializerLinux.TextList(assemblyCodeList, textList,
                                                externSet);
        return (new StaticSymbolLinux(StaticSymbolLinux.TextOrData.Data,
                                      uniqueName, textList, externSet));
      }

      return null;
    }
  }
}