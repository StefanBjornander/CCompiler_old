using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class ConstantExpression {
    public static bool IsConstant(Expression expression) {
      Symbol resultSymbol = expression.Symbol;
      Type type = resultSymbol.Type;
      return (type.IsLogical() && ((resultSymbol.TrueSet.Count == 0) ||
              (resultSymbol.FalseSet.Count == 0))) ||
             (type.IsIntegralOrPointer() &&
              (resultSymbol.Value is BigInteger)) ||
             (type.IsFloating() && (resultSymbol.Value is decimal));
    }

    public static bool IsTrue(Expression expression) {
      return ((expression.Symbol.Value is BigInteger) &&
              !((BigInteger) expression.Symbol.Value).IsZero) ||
             ((expression.Symbol.Value is decimal) &&
              !((decimal) expression.Symbol.Value).Equals((decimal) 0)) ||
             (expression.Symbol.TrueSet.Count > 0);
    }

    private static Expression LogicalToIntegral(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return ConstantCast(expression, Type.SignedIntegerType);
      }

      return expression;
    }

    private static Expression LogicalToFloating(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return ConstantCast(expression, Type.DoubleType);
      }

      return expression;
    }

    private static Expression ToLogical(Expression expression) {
      if (!expression.Symbol.Type.IsLogical()) {
        return ConstantCast(expression, Type.LogicalType);
      }

      return expression;
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

    private static Expression RelationIntegral(MiddleOperator middleOp,
                                               Expression leftExpression,
                                               Expression rightExpression) {
      leftExpression = TypeCast.LogicalToIntegral(leftExpression);
      rightExpression = TypeCast.LogicalToIntegral(rightExpression);

      BigInteger leftValue = (BigInteger) leftExpression.Symbol.Value,
                 rightValue = (BigInteger) rightExpression.Symbol.Value;

      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Jump);
      longList.Add(jumpCode);
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

        case MiddleOperator.LessThan:
          resultValue = (leftValue < rightValue);
          break;

        case MiddleOperator.LessThanEqual:
          resultValue = (leftValue <= rightValue);
          break;

        case MiddleOperator.GreaterThan:
          resultValue = (leftValue > rightValue);
          break;

        case MiddleOperator.GreaterThanEqual:
          resultValue = (leftValue >= rightValue);
          break;
      }

      Symbol resultSymbol = resultValue ? (new Symbol(jumpSet, null))
                                        : (new Symbol(null, jumpSet));
      return (new Expression(resultSymbol, null, longList));
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

      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Jump);
      longList.Add(jumpCode);
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

        case MiddleOperator.LessThan:
          resultValue = (leftValue < rightValue);
          break;

        case MiddleOperator.LessThanEqual:
          resultValue = (leftValue <= rightValue);
          break;

        case MiddleOperator.GreaterThan:
          resultValue = (leftValue > rightValue);
          break;

        case MiddleOperator.GreaterThanEqual:
          resultValue = (leftValue >= rightValue);
          break;
      }

      Symbol resultSymbol = resultValue ? (new Symbol(jumpSet, null))
                                        : (new Symbol(null, jumpSet));
      return (new Expression(resultSymbol, null, longList));
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

      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Jump);
      longList.Add(jumpCode);
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

      Symbol resultSymbol = resultValue ? (new Symbol(jumpSet, null))
                                        : (new Symbol(null, jumpSet));
      return (new Expression(resultSymbol, null, longList));
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

        case MiddleOperator.Multiply:
          resultValue = leftValue * rightValue;
          break;
        
        case MiddleOperator.Divide:
          resultValue = leftValue / rightValue;
          break;
        
        case MiddleOperator.Modulo:
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

      if (leftExpression.Symbol.Value is BigInteger) {
        leftValue = (decimal) ((BigInteger) leftExpression.Symbol.Value);
      }
      else {
        leftValue = (decimal) leftExpression.Symbol.Value;
      }

      if (rightExpression.Symbol.Value is BigInteger) {
        rightValue = (decimal) ((BigInteger) rightExpression.Symbol.Value);
      }
      else {
        rightValue = (decimal) rightExpression.Symbol.Value;
      }

      switch (middleOp) {
        case MiddleOperator.BinaryAdd:
          resultValue = leftValue + rightValue;
          break;
        
        case MiddleOperator.BinarySubtract:
          resultValue = leftValue - rightValue;
          break;
        
        case MiddleOperator.Multiply:
          resultValue = leftValue * rightValue;
          break;
        
        case MiddleOperator.Divide:
          resultValue = leftValue / rightValue;
          break;
      }

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                      rightExpression.Symbol.Type);
      Symbol resultSymbol = new Symbol(maxType, resultValue);
      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCodeGenerator.AddMiddleCode(longList, MiddleOperator.PushFloat,
                                        resultSymbol);
      return (new Expression(resultSymbol, null, longList));
    }

    public static Expression LogicalNot(Expression expression) {
      if (IsConstant(expression)) {
        expression = ToLogical(expression);
        Symbol resultSymbol = new Symbol(expression.Symbol.FalseSet,
                                         expression.Symbol.TrueSet);
        return (new Expression(resultSymbol, null, expression.LongList));
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
      BigInteger value = (BigInteger) expression.Symbol.Value,
                 resultValue = 0;

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

      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCodeGenerator.AddMiddleCode(longList, MiddleOperator.PushFloat,
                                        resultSymbol);
      return (new Expression(resultSymbol, null, longList));
    }

    public static Expression ConstantCast(Expression sourceExpression,
                                          Type targetType) {
      Symbol sourceSymbol = sourceExpression.Symbol, targetSymbol;
      Type sourceType = sourceSymbol.Type;
      
      if (!IsConstant(sourceExpression) || sourceType.IsVoid()) {
        return null;
      }

      object sourceValue = sourceSymbol.Value;
      List<MiddleCode> longList = new List<MiddleCode>();

      if (targetType.IsVoid()) {
        targetSymbol = new Symbol(targetType);
      }
      else if (sourceType.IsIntegralPointerArrayStringOrFunction() &&
               targetType.IsFloating()) {
        decimal targetValue = ((decimal) ((BigInteger) sourceValue));
        targetSymbol = new Symbol(targetType, targetValue);
      }
      else if (sourceType.IsFloating() &&
               targetType.IsIntegralPointerOrArray()) {
        BigInteger targetValue = ((BigInteger) ((decimal) sourceValue));
        targetSymbol = new Symbol(targetType, targetValue);
      }
      else if (sourceType.IsLogical() &&
               targetType.IsIntegralPointerOrArray()) {
        bool isTrue = (sourceSymbol.TrueSet.Count > 0);
        BigInteger targetValue = isTrue ? BigInteger.One : BigInteger.Zero;
        targetSymbol = new Symbol(targetType, targetValue);
      }
      else if (sourceType.IsLogical() && targetType.IsFloating()) {
        bool isTrue = (sourceSymbol.TrueSet.Count > 0);
        decimal targetValue = isTrue ? decimal.One : decimal.Zero;
        targetSymbol = new Symbol(targetType, targetValue);
      }
      else if (sourceType.IsArithmeticPointerArrayStringOrFunction() &&
               targetType.IsLogical()) {
        bool isTrue = !sourceValue.Equals(BigInteger.Zero) &&
                      !sourceValue.Equals(decimal.Zero);

        MiddleCode gotoCode = new MiddleCode(MiddleOperator.Jump);
        longList.Add(gotoCode);

        ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                         falseSet = new HashSet<MiddleCode>();
        if (isTrue) {
          trueSet.Add(gotoCode);
        }
        else {
          falseSet.Add(gotoCode);
        }

        targetSymbol = new Symbol(trueSet, falseSet);
      }
      else {
        targetSymbol = new Symbol(targetType, sourceValue);
      }

      if (targetType.IsFloating()) {
        longList.Add(new MiddleCode(MiddleOperator.PushFloat, targetSymbol));
      }

      return (new Expression(targetSymbol, null, longList));
    }
/*
    public static Expression ConstantCast(Expression sourceExpression,
                                  Type targetType) {
      if (!IsConstant(sourceExpression)) {
        return null;
      }

      Symbol sourceSymbol = sourceExpression.Symbol;
      Type sourceType = sourceSymbol.Type;
      object sourceValue = sourceSymbol.Value;

      if (sourceType.Equals(targetType)) {
        return sourceExpression;
      }
      else if ((sourceType.Size() == targetType.Size()) &&
               ((sourceType.IsIntegralArrayOrPointer() &&
                 targetType.IsIntegralArrayOrPointer()) ||
               (sourceType.IsFloating() && targetType.IsFloating()))) {
        Symbol targetSymbol = new Symbol(targetType, sourceValue);
        return (new Expression(targetSymbol, sourceExpression.ShortList,
                               sourceExpression.LongList));
      }
      else if (sourceType.IsLogical() &&
               targetType.IsIntegralArrayOrPointer()) {
        if (sourceSymbol.TrueSet.Count > 0) {
          Symbol targetSymbol = new Symbol(targetType, BigInteger.One);
          return (new Expression(targetSymbol, null, null));
        }
        else {
          Symbol targetSymbol = new Symbol(targetType, BigInteger.Zero);
          return (new Expression(targetSymbol, null, null));
        }
      }
      else if ((sourceType.IsIntegralArrayOrPointer() ||
                sourceType.IsFloating()) &&
                targetType.IsLogical()) {
        List<MiddleCode> longList = new List<MiddleCode>();
        MiddleCode jumpCode = new MiddleCode(MiddleOperator.Goto);
        longList.Add(jumpCode);
        ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
        jumpSet.Add(jumpCode);

        Symbol targetSymbol;
        if (sourceValue.Equals(BigInteger.Zero) ||
            sourceValue.Equals((decimal) 0)) {
          targetSymbol = new Symbol(null, jumpSet);
        }
        else {
          targetSymbol = new Symbol(jumpSet, null);
        }

        return (new Expression(targetSymbol, null, longList));
      }
      else {
        Assert.ErrorXXX((sourceValue is BigInteger) || (sourceValue is decimal));
        Symbol targetSymbol = null;

        if (sourceType.IsIntegralArrayOrPointer() && targetType.IsFloating()){
          targetSymbol =
            new Symbol(targetType, (decimal) ((BigInteger) sourceValue));
        }
        else if (sourceType.IsFloating() &&
                 targetType.IsIntegralArrayOrPointer()) {
          targetSymbol =
            new Symbol(targetType, (BigInteger) ((decimal) sourceValue));
        }
        else {
          targetSymbol = new Symbol(targetType, sourceValue);
        }

        List<MiddleCode> longList = new List<MiddleCode>();
        if (targetType.IsFloating()) {
          MiddleCodeGenerator.
            AddMiddleCode(longList, MiddleOperator.PushFloat, targetSymbol);
        }

        return (new Expression(targetSymbol, null, longList));
      }
    }*/
    
    public static StaticSymbol Value(Symbol symbol) {
      return Value(symbol.UniqueName, symbol.Type, symbol.Value);
    }

    public static StaticSymbol Value(string uniqueName, Type type,
                                     object value) {
      List<MiddleCode> middleCodeList;

      if (value is List<MiddleCode>) {
        middleCodeList = (List<MiddleCode>) value;
      }
      else {
        middleCodeList = new List<MiddleCode>();
      
        if (value != null) {
          middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                            type.Sort, value));
        }
        else {
          middleCodeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                            type.Size()));
        }
      }

      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AssemblyCodeGenerator.GenerateAssembly(middleCodeList,
                                             assemblyCodeList);

      if (Start.Linux) {
        List<string> textList = new List<string>();
        textList.Add("section .data");
        textList.Add("\n" + uniqueName + ":");
        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList,
                                            externSet);
        return (new StaticSymbolLinux(uniqueName, textList, externSet));
      }

      if (Start.Windows) {
        List<byte> byteList = new List<byte>();
        IDictionary<int,string> accessMap = new Dictionary<int,string>();
        AssemblyCodeGenerator.
          GenerateTargetWindows(assemblyCodeList, byteList,
                                accessMap, null, null);
        return (new StaticSymbolWindows(uniqueName, byteList, accessMap));
      }
      
      return null;
    }
  }
}
