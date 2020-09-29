using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class TypeCast {
    public static Expression LogicalToIntegral(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return ImplicitCast(expression, Type.SignedIntegerType);
      }

      return expression;
    }

    public static Expression LogicalToFloating(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return ImplicitCast(expression, Type.DoubleType);
      }

      return expression;
    }

    public static Expression ToLogical(Expression expression) {
      if (!expression.Symbol.Type.IsLogical()) {
        return ImplicitCast(expression, Type.LogicalType);
      }

      return expression;
    }


    public static Expression ImplicitCast(Expression fromExpression,
                                          Type toType) {
      Expression constantExpression =
        ConstantExpression.ConstantCast(fromExpression, toType);
      if (constantExpression != null) {
        return constantExpression;
      }
      Type fromType = fromExpression.Symbol.Type;

      if (fromType.Equals(toType) ||
          (fromType.IsLogical() && toType.IsLogical()) ||
          (fromType.IsPointerArrayStringOrFunction() &&
           toType.IsPointerOrArray()) ||
          (((fromType.IsFloating() && toType.IsFloating()) ||
            (fromType.IsIntegralPointerOrFunction() &&
             toType.IsIntegralPointerArrayOrFunction())) &&
           (fromType.SizeArray() == toType.SizeArray()))) {        
        return fromExpression;
      }
      else {
        return ExplicitCast(fromExpression, toType);
      }
    }

    public static Expression ExplicitCast(Expression sourceExpression,
                                          Type targetType) {
      Expression constantExpression =
        ConstantExpression.ConstantCast(sourceExpression, targetType);
      if (constantExpression != null) {
        return constantExpression;
      }

      Symbol sourceSymbol = sourceExpression.Symbol, targetSymbol = null;
      Type sourceType = sourceSymbol.Type;
      List<MiddleCode> longList = sourceExpression.LongList;

      /*if (sourceType.IsFloating()) {
        MiddleCode popCode = new MiddleCode(MiddleOperator.PopEmpty);
        longList.Add(popCode);
      }*/

      if (targetType.IsVoid()) {
        targetSymbol = new Symbol(targetType);
      }
      else if (sourceType.IsStructOrUnion() && targetType.IsStructOrUnion()) {
        Assert.Error(sourceType.Equals(targetType), sourceType + " to " + targetType,
                     Message.Invalid_type_cast);
        targetSymbol = new Symbol(targetType);
      }
      else if (sourceType.IsLogical() &&
               targetType.IsArithmeticPointerOrArray()) {
        targetSymbol = new Symbol(targetType);
        MiddleCode trueCode, falseCode;

        if (targetType.IsIntegralArrayOrPointer()) {
          Symbol oneSymbol = new Symbol(targetType, BigInteger.One);
          trueCode = new MiddleCode(MiddleOperator.Assign, targetSymbol, oneSymbol);
          Symbol zeroSymbol = new Symbol(targetType, BigInteger.Zero);
          falseCode = new MiddleCode(MiddleOperator.Assign, targetSymbol, zeroSymbol);
        }
        else {
          trueCode = new MiddleCode(MiddleOperator.PushOne);
          falseCode = new MiddleCode(MiddleOperator.PushZero);
        }

        longList.Add(trueCode);
        MiddleCodeGenerator.Backpatch(sourceSymbol.TrueSet, trueCode);
        MiddleCode jumpCode = new MiddleCode(MiddleOperator.Empty);

        longList.Add(new MiddleCode(MiddleOperator.Goto, jumpCode));

        longList.Add(falseCode);
        MiddleCodeGenerator.Backpatch(sourceSymbol.FalseSet, falseCode);
        
        longList.Add(jumpCode);
      }
      else if (sourceType.IsArithmeticPointerArrayStringOrFunction() &&
               targetType.IsLogical()) {
        Symbol zeroSymbol;
        if (sourceType.IsIntegralPointerArrayStringOrFunction()) {
          zeroSymbol = new Symbol(targetType, BigInteger.Zero);
        }
        else {
          zeroSymbol = new Symbol(targetType, decimal.Zero);
        }

        MiddleCode testCode =
          new MiddleCode(MiddleOperator.NotEqual, null,
                         sourceSymbol, zeroSymbol);
        longList.Add(testCode);
        ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
        trueSet.Add(testCode);

        MiddleCode gotoCode = new MiddleCode(MiddleOperator.Goto);
        longList.Add(gotoCode);
        ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
        falseSet.Add(gotoCode);

        targetSymbol = new Symbol(trueSet, falseSet);
      }
      else if (sourceType.IsFloating()) {
        if (targetType.IsFloating()) {
          targetSymbol = new Symbol(targetType);
        }
        else if (targetType.IsIntegralArrayOrPointer()) {
          targetSymbol = new Symbol(targetType);

          if (targetType.Size() == 1) {
            Type tempType = sourceType.IsSigned() ? Type.SignedIntegerType
                                                  : Type.UnsignedIntegerType;
            Symbol tempSymbol = new Symbol(tempType);
            MiddleCode tempCode =
              new MiddleCode(MiddleOperator.FloatingToIntegral, tempSymbol,
                             sourceSymbol);
            longList.Add(tempCode);
            MiddleCode resultCode =
              new MiddleCode(MiddleOperator.IntegralToIntegral, targetSymbol,
                             tempSymbol);
            longList.Add(resultCode);
          }
          else {
            MiddleCode resultCode =
              new MiddleCode(MiddleOperator.FloatingToIntegral, targetSymbol,
                             sourceSymbol);
            longList.Add(resultCode);
          }
        }
      }
      else if (sourceType.IsIntegralArrayOrPointer()) {
        if (targetType.IsFloating()) {
          targetSymbol = new Symbol(targetType);

          if (sourceType.Size() == 1) {
            Type tempType = sourceType.IsSigned() ? Type.SignedIntegerType
                                                  : Type.UnsignedIntegerType;
            Symbol tempSymbol = new Symbol(tempType);
            MiddleCodeGenerator.
              AddMiddleCode(longList, MiddleOperator.IntegralToIntegral,
                            tempSymbol, sourceSymbol);
            MiddleCodeGenerator.
              AddMiddleCode(longList, MiddleOperator.IntegralToFloating,
                            targetSymbol, tempSymbol);
          }
          else {
            MiddleCodeGenerator.
              AddMiddleCode(longList, MiddleOperator.IntegralToFloating,
                            targetSymbol, sourceSymbol);
          }
        }
        else if (targetType.IsIntegralArrayOrPointer()) {
          targetSymbol = new Symbol(targetType);
          MiddleCodeGenerator.
            AddMiddleCode(longList, MiddleOperator.IntegralToIntegral,
                          targetSymbol, sourceSymbol);
        }
      }

      Assert.Error(targetSymbol != null, sourceType + " to " +
                   targetType, Message.Invalid_type_cast);

      /*if (sourceType.IsFloating()) {
        MiddleCode popCode = new MiddleCode(MiddleOperator.PopEmpty);
        longList.Add(popCode);
      }*/

      return (new Expression(targetSymbol, null, longList));
    }

/*    public static Expression ExplicitCast(Expression fromExpression,
                                          Type toType) {
      Expression constantExpression =
        ConstantExpression.ConstantCast(fromExpression, toType);
      if (constantExpression != null) {
        return constantExpression;
      }

      Symbol fromSymbol = fromExpression.Symbol;
      Type fromType = fromSymbol.Type;

      if (toType.IsVoid()) {
        return (new Expression(new Symbol(toType), null, null));
      }
      else if (fromType.IsStructOrUnion() && toType.IsStructOrUnion()) {
        Assert.Error(fromType.Equals(toType), fromType + " to " + toType,
                     Message.Invalid_type_cast);
        return (new Expression(new Symbol(toType), fromExpression.ShortList,
                               fromExpression.LongList));
      }
      else if (fromType.IsLogical()) {
        if (toType.IsFloating()) {
          List<MiddleCode> codeList = fromExpression.LongList;
          Symbol resultSymbol = new Symbol(toType);
          MiddleCode oneCode = MiddleCodeGenerator.AddMiddleCode(codeList,
                                                   MiddleOperator.PushOne);
          MiddleCodeGenerator.Backpatch(fromSymbol.TrueSet, oneCode);
          MiddleCode toCode = new MiddleCode(MiddleOperator.Empty);
          MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Goto,
                                            toCode);
          MiddleCode zeroCode = MiddleCodeGenerator.AddMiddleCode(codeList,
                                                    MiddleOperator.PushZero);
          MiddleCodeGenerator.Backpatch(fromSymbol.FalseSet, zeroCode);
          codeList.Add(toCode);
          return (new Expression(resultSymbol, fromExpression.ShortList,
                                 codeList));
        }
        else if (toType.IsIntegralArrayOrPointer()) {
          List<MiddleCode> codeList = fromExpression.LongList;
          Symbol resultSymbol = new Symbol(toType);

          Symbol oneSymbol = new Symbol(toType, BigInteger.One); 
          MiddleCode assignTrue =
            MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Assign,
                                              resultSymbol, oneSymbol);
          MiddleCodeGenerator.Backpatch(fromSymbol.TrueSet, assignTrue);
      
          MiddleCode toCode = new MiddleCode(MiddleOperator.Empty);
          MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Goto,
                                            toCode);

          Symbol zeroSymbol = new Symbol(toType, ((BigInteger) 0));
          MiddleCode assignFalse =
            MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Assign,
                                              resultSymbol, zeroSymbol);
          MiddleCodeGenerator.Backpatch(fromSymbol.FalseSet, assignFalse);
      
          codeList.Add(toCode);
          return (new Expression(resultSymbol, fromExpression.ShortList,
                                 codeList));
        }

        Assert.Error(fromType + " to " + toType, Message.Invalid_type_cast);
      }
      else if (fromType.IsFloating()) {
        if (toType.IsFloating()) {
          return (new Expression(new Symbol(toType), fromExpression.ShortList,
                                 fromExpression.LongList));
        }
        else if (toType.IsIntegralArrayOrPointer()) {
          List<MiddleCode> codeList = fromExpression.LongList;
          Symbol resultSymbol = new Symbol(toType);

          if (toType.Size() == 1) {
            Type tempType = fromType.IsSigned() ? Type.SignedIntegerType
                                                  : Type.UnsignedIntegerType;
            Symbol tempSymbol = new Symbol(tempType);
            MiddleCode tempCode =
              new MiddleCode(MiddleOperator.FloatingToIntegral, tempSymbol,
                             fromSymbol);
            codeList.Add(tempCode);
            MiddleCode resultCode =
              new MiddleCode(MiddleOperator.IntegralToIntegral, resultSymbol,
                             tempSymbol);
            codeList.Add(resultCode);
          }
          else {
            MiddleCode resultCode =
              new MiddleCode(MiddleOperator.FloatingToIntegral, resultSymbol,
                             fromSymbol);
            codeList.Add(resultCode);
          }

          return (new Expression(resultSymbol, fromExpression.ShortList,
                                 codeList));
        }
        else if (toType.IsLogical()) {
          List<MiddleCode> codeList = fromExpression.LongList;

          ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
          Symbol zeroSymbol = new Symbol(toType, (decimal) 0);
          MiddleCode testCode =
            new MiddleCode(MiddleOperator.NotEqual, null,
                           fromExpression.Symbol, zeroSymbol);
          codeList.Add(testCode);
          trueSet.Add(testCode);

          ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
          MiddleCode gotoCode = new MiddleCode(MiddleOperator.Goto);
          codeList.Add(gotoCode);
          falseSet.Add(testCode);

          Symbol symbol = new Symbol(trueSet, falseSet);
          return (new Expression(symbol, null, codeList));
        }
      
        Assert.Error(fromType + " to " + toType, Message.Invalid_type_cast);
      }
      else if (fromType.IsIntegralArrayOrPointer()) {
        if (toType.IsFloating()) {
          List<MiddleCode> codeList = fromExpression.LongList;
          Symbol resultSymbol = new Symbol(toType);

          if (fromType.Size() == 1) {
            Type tempType = fromType.IsSigned() ? Type.SignedIntegerType
                                                  : Type.UnsignedIntegerType;
            Symbol tempSymbol = new Symbol(tempType);
            MiddleCodeGenerator.
              AddMiddleCode(codeList, MiddleOperator.IntegralToIntegral,
                            tempSymbol, fromSymbol);
            MiddleCodeGenerator.
              AddMiddleCode(codeList, MiddleOperator.IntegralToFloating,
                            resultSymbol, tempSymbol);
          }
          else {
            MiddleCodeGenerator.
              AddMiddleCode(codeList, MiddleOperator.IntegralToFloating,
                            resultSymbol, fromSymbol);
          }

          return (new Expression(resultSymbol, fromExpression.ShortList,
                                 codeList));
        }
        else if (toType.IsIntegralArrayOrPointer()) {
          List<MiddleCode> codeList = fromExpression.LongList;
          Symbol resultSymbol = new Symbol(toType);
          MiddleCodeGenerator.
            AddMiddleCode(codeList, MiddleOperator.IntegralToIntegral,
                          resultSymbol, fromSymbol);
          return (new Expression(resultSymbol, fromExpression.ShortList,
                                 codeList));
        }
        else if (toType.IsLogical()) {
          List<MiddleCode> codeList = fromExpression.LongList;

          ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
          Symbol zeroSymbol = new Symbol(toType, BigInteger.Zero);
          MiddleCode testCode =
            new MiddleCode(MiddleOperator.NotEqual, null,
                           fromExpression.Symbol, zeroSymbol);
          codeList.Add(testCode);
          trueSet.Add(testCode);

          ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
          MiddleCode gotoCode = new MiddleCode(MiddleOperator.Goto);
          codeList.Add(gotoCode);
          falseSet.Add(gotoCode);

          Symbol symbol = new Symbol(trueSet, falseSet);
          return (new Expression(symbol, null, codeList));
        }

        Assert.Error(fromType + " to " + toType, Message.Invalid_type_cast);
      }
      else {
        Assert.Error(fromType + " to " + toType, Message.Invalid_type_cast);
      }

      return null;
    }*/

    public static Type MaxType(Type leftType, Type rightType) {
      if ((leftType.IsFloating() && !rightType.IsFloating()) ||
          ((leftType.Size() == rightType.Size()) &&
           leftType.IsSigned() && !rightType.IsSigned())) {
        return leftType;
      }
      else if ((!leftType.IsFloating() && rightType.IsFloating()) ||
               ((leftType.Size() == rightType.Size()) &&
               !leftType.IsSigned() && rightType.IsSigned())) {
        return rightType;
      }
      else {
        return (leftType.Size() > rightType.Size()) ? leftType : rightType;
      }
    }
  }
}
