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
        ConstantExpression.Cast(fromExpression, toType);
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

    public static Expression ExplicitCast(Expression fromExpression,
                                          Type toType) {
      Expression constantExpression =
        ConstantExpression.Cast(fromExpression, toType);
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
