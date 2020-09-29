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

      Symbol sourceSymbol = sourceExpression.Symbol, targetSymbol;
      Type sourceType = sourceSymbol.Type;

      List<MiddleCode> shortList = sourceExpression.ShortList,
                       longList = sourceExpression.LongList;

      if (sourceType.IsFloating()) {
        MiddleCode popCode = new MiddleCode(MiddleOperator.PopEmpty);
        shortList.Add(popCode);
        longList.Add(popCode);
      }

      if (targetType.IsVoid()) {
        targetSymbol = new Symbol(targetType);
      }
      else if (sourceType.IsStructOrUnion() && targetType.IsStructOrUnion()) {
        Assert.Error(sourceType.Equals(targetType), sourceType + " to " + targetType,
                     Message.Invalid_type_cast);
        targetSymbol = new Symbol(targetType);
      }
      else if (sourceType.IsLogical()) {
        if (targetType.IsFloating()) {
          List<MiddleCode> codeList = sourceExpression.LongList;
          Symbol resultSymbol = new Symbol(targetType);
          MiddleCode oneCode = MiddleCodeGenerator.AddMiddleCode(codeList,
                                                   MiddleOperator.PushOne);
          MiddleCodeGenerator.Backpatch(sourceSymbol.TrueSet, oneCode);
          MiddleCode toCode = new MiddleCode(MiddleOperator.Empty);
          MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Goto,
                                            toCode);
          MiddleCode zeroCode = MiddleCodeGenerator.AddMiddleCode(codeList,
                                                    MiddleOperator.PushZero);
          MiddleCodeGenerator.Backpatch(sourceSymbol.FalseSet, zeroCode);
          codeList.Add(toCode);
          return (new Expression(resultSymbol, sourceExpression.ShortList,
                                 codeList));
        }
        else if (targetType.IsIntegralArrayOrPointer()) {
          List<MiddleCode> codeList = sourceExpression.LongList;
          Symbol resultSymbol = new Symbol(targetType);

          Symbol oneSymbol = new Symbol(targetType, BigInteger.One); 
          MiddleCode assignTrue =
            MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Assign,
                                              resultSymbol, oneSymbol);
          MiddleCodeGenerator.Backpatch(sourceSymbol.TrueSet, assignTrue);
      
          MiddleCode toCode = new MiddleCode(MiddleOperator.Empty);
          MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Goto,
                                            toCode);

          Symbol zeroSymbol = new Symbol(targetType, ((BigInteger) 0));
          MiddleCode assignFalse =
            MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Assign,
                                              resultSymbol, zeroSymbol);
          MiddleCodeGenerator.Backpatch(sourceSymbol.FalseSet, assignFalse);
      
          codeList.Add(toCode);
          return (new Expression(resultSymbol, sourceExpression.ShortList,
                                 codeList));
        }

        Assert.Error(sourceType + " to " + targetType, Message.Invalid_type_cast);
      }
      else if (sourceType.IsFloating()) {
        if (targetType.IsFloating()) {
          return (new Expression(new Symbol(targetType), sourceExpression.ShortList,
                                 sourceExpression.LongList));
        }
        else if (targetType.IsIntegralArrayOrPointer()) {
          List<MiddleCode> codeList = sourceExpression.LongList;
          Symbol resultSymbol = new Symbol(targetType);

          if (targetType.Size() == 1) {
            Type tempType = sourceType.IsSigned() ? Type.SignedIntegerType
                                                  : Type.UnsignedIntegerType;
            Symbol tempSymbol = new Symbol(tempType);
            MiddleCode tempCode =
              new MiddleCode(MiddleOperator.FloatingToIntegral, tempSymbol,
                             sourceSymbol);
            codeList.Add(tempCode);
            MiddleCode resultCode =
              new MiddleCode(MiddleOperator.IntegralToIntegral, resultSymbol,
                             tempSymbol);
            codeList.Add(resultCode);
          }
          else {
            MiddleCode resultCode =
              new MiddleCode(MiddleOperator.FloatingToIntegral, resultSymbol,
                             sourceSymbol);
            codeList.Add(resultCode);
          }

          return (new Expression(resultSymbol, sourceExpression.ShortList,
                                 codeList));
        }
        else if (targetType.IsLogical()) {
          List<MiddleCode> codeList = sourceExpression.LongList;

          ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
          Symbol zeroSymbol = new Symbol(targetType, (decimal) 0);
          MiddleCode testCode =
            new MiddleCode(MiddleOperator.NotEqual, null,
                           sourceExpression.Symbol, zeroSymbol);
          codeList.Add(testCode);
          trueSet.Add(testCode);

          ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
          MiddleCode gotoCode = new MiddleCode(MiddleOperator.Goto);
          codeList.Add(gotoCode);
          falseSet.Add(testCode);

          Symbol symbol = new Symbol(trueSet, falseSet);
          return (new Expression(symbol, null, codeList));
        }
      
        Assert.Error(sourceType + " to " + targetType, Message.Invalid_type_cast);
      }
      else if (sourceType.IsIntegralArrayOrPointer()) {
        if (targetType.IsFloating()) {
          List<MiddleCode> codeList = sourceExpression.LongList;
          Symbol resultSymbol = new Symbol(targetType);

          if (sourceType.Size() == 1) {
            Type tempType = sourceType.IsSigned() ? Type.SignedIntegerType
                                                  : Type.UnsignedIntegerType;
            Symbol tempSymbol = new Symbol(tempType);
            MiddleCodeGenerator.
              AddMiddleCode(codeList, MiddleOperator.IntegralToIntegral,
                            tempSymbol, sourceSymbol);
            MiddleCodeGenerator.
              AddMiddleCode(codeList, MiddleOperator.IntegralToFloating,
                            resultSymbol, tempSymbol);
          }
          else {
            MiddleCodeGenerator.
              AddMiddleCode(codeList, MiddleOperator.IntegralToFloating,
                            resultSymbol, sourceSymbol);
          }

          return (new Expression(resultSymbol, sourceExpression.ShortList,
                                 codeList));
        }
        else if (targetType.IsIntegralArrayOrPointer()) {
          List<MiddleCode> codeList = sourceExpression.LongList;
          Symbol resultSymbol = new Symbol(targetType);
          MiddleCodeGenerator.
            AddMiddleCode(codeList, MiddleOperator.IntegralToIntegral,
                          resultSymbol, sourceSymbol);
          return (new Expression(resultSymbol, sourceExpression.ShortList,
                                 codeList));
        }
        else if (targetType.IsLogical()) {
          List<MiddleCode> codeList = sourceExpression.LongList;

          ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
          Symbol zeroSymbol = new Symbol(targetType, BigInteger.Zero);
          MiddleCode testCode =
            new MiddleCode(MiddleOperator.NotEqual, null,
                           sourceExpression.Symbol, zeroSymbol);
          codeList.Add(testCode);
          trueSet.Add(testCode);

          ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
          MiddleCode gotoCode = new MiddleCode(MiddleOperator.Goto);
          codeList.Add(gotoCode);
          falseSet.Add(gotoCode);

          Symbol symbol = new Symbol(trueSet, falseSet);
          return (new Expression(symbol, null, codeList));
        }

        Assert.Error(sourceType + " to " + targetType, Message.Invalid_type_cast);
      }
      else {
        Assert.Error(sourceType + " to " + targetType, Message.Invalid_type_cast);
      }

      return (new Expression(targetSymbol, shortList, longList));                                          
    }

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
