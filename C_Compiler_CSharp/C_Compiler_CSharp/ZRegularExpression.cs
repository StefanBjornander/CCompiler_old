using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCompiler {
  class RegularExpression {
/*    public static Expression Addition(Expression leftExpression, Expression rightExpression) {
      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);    

      if (leftType.IsPointerOrArray()) {
        rightSymbol = TypeCast.ImplicitCast(rightExpression.LongList, rightSymbol, leftType);
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol = new Symbol(new Type(leftType.PointerOrArrayType));

        if (rightSymbol.Value is BigInteger) {
          int rightValue = (int) ((BigInteger) rightSymbol.Value);
          Symbol sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) (rightValue * leftType.PointerOrArrayType.Size()));
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftSymbol, sizeSymbol);
        }
        else if (leftType.PointerOrArrayType.Size() > 1) {
          Symbol multSymbol = new Symbol(Type.PointerTypeX),
                 sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) leftType.PointerOrArrayType.Size());
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, rightSymbol, sizeSymbol);
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftSymbol, multSymbol);
        }
        else {
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftSymbol, rightSymbol);
        }

        return (new Expression(resultSymbol, shortList, longList));
      }
      else if (rightType.IsPointerOrArray()) {
        leftSymbol = TypeCast.ImplicitCast(leftExpression.LongList, leftSymbol, rightType);
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol;

        if (leftSymbol.Value is BigInteger) {
          int leftValue = (int) ((BigInteger) leftSymbol.Value);
          Symbol sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) (leftValue * rightType.PointerOrArrayType.Size()));
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          resultSymbol = new Symbol(new Type(rightType.PointerOrArrayType));
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, sizeSymbol, rightSymbol);
        }
        else if (rightType.PointerOrArrayType.Size() > 1) {
          Symbol multSymbol = new Symbol(Type.PointerTypeX),
                 sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) rightType.PointerOrArrayType.Size());
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, leftSymbol, sizeSymbol);
          resultSymbol = new Symbol(new Type(rightType.PointerOrArrayType));
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, multSymbol, rightSymbol);
        }
        else {
          resultSymbol = new Symbol(new Type(rightType.PointerOrArrayType));
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftSymbol, rightSymbol);
        }

        return (new Expression(resultSymbol, shortList, longList));
      }
      else {
        Type maxType = TypeCast.MaxType(leftType, rightType);
        leftSymbol = TypeCast.ImplicitCast(leftExpression.LongList, leftSymbol, maxType);
        rightSymbol = TypeCast.ImplicitCast(rightExpression.LongList, rightSymbol, maxType);
        Symbol resultSymbol = new Symbol(maxType);

        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftSymbol, rightSymbol);
        return (new Expression(resultSymbol, shortList, longList));
      }
    }

    public static Expression Subtraction(Expression leftExpression, Expression rightExpression) {
      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);    

      if (leftType.IsPointerOrArray() && rightType.IsArithmetic()) {
        rightSymbol = TypeCast.ImplicitCast(rightExpression.LongList, rightSymbol, leftType);
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol = new Symbol(new Type(leftType.PointerOrArrayType));

        if (rightSymbol.Value is BigInteger) {
          int rightValue = (int) ((BigInteger) rightSymbol.Value);
          Symbol sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) (rightValue * leftType.PointerOrArrayType.Size()));
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol, leftSymbol, sizeSymbol);
        }
        else if (leftType.PointerOrArrayType.Size() > 1) {
          Symbol multSymbol = new Symbol(Type.PointerTypeX),
                 sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) leftType.PointerOrArrayType.Size());
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, rightSymbol, sizeSymbol);
          AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol, leftSymbol, multSymbol);
        }
        else {
          AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol, leftSymbol, rightSymbol);
        }

        return (new Expression(resultSymbol, shortList, longList));
      }
      else if (leftType.IsPointerOrArray() && rightType.IsPointerOrArray()) {
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol = new Symbol(Type.SignedIntegerType);
        
        if (rightType.PointerOrArrayType.Size() > 1) {
          Symbol subtractSymbol = new Symbol(Type.PointerTypeX),
                 sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) rightType.PointerOrArrayType.Size());
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.BinarySubtract, subtractSymbol, leftSymbol, rightSymbol);
          AddMiddleCode(longList, MiddleOperator.UnsignedDivide, resultSymbol, subtractSymbol, sizeSymbol);
        }
        else {
          AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol, leftSymbol, rightSymbol);
        }

        return (new Expression(resultSymbol, shortList, longList));
      }
      else {
        Type maxType = TypeCast.MaxType(leftType, rightType);
        leftSymbol = TypeCast.ImplicitCast(leftExpression.LongList, leftSymbol, maxType);
        rightSymbol = TypeCast.ImplicitCast(rightExpression.LongList, rightSymbol, maxType);
        Symbol resultSymbol = new Symbol(maxType);

        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol, leftSymbol, rightSymbol);
        return (new Expression(resultSymbol, shortList, longList));
      }
    }
    
    public static Expression Multiply(MiddleOperator middleOp, Expression leftExpression, Expression rightExpression) {
      Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);

      if (MiddleCode.IsModulo(middleOp)) {
        Assert.Error(maxType.IsIntegralPointerArrayStringOrFunction(),
                      maxType, Message.Invalid_type_in_expression);
      }
      else {
        Assert.Error(maxType.IsArithmeticPointerArrayStringOrFunction(),
                      maxType, Message.Invalid_type_in_expression);
      }

      leftSymbol = TypeCast.ImplicitCast(leftExpression.LongList, leftSymbol, maxType);
      rightSymbol = TypeCast.ImplicitCast(rightExpression.LongList, rightSymbol, maxType);
      Symbol resultSymbol = new Symbol(maxType);

      if (maxType.IsUnsigned()) {
        string name = Enum.GetName(typeof(MiddleOperator), middleOp);
        middleOp = (MiddleOperator) Enum.Parse(typeof(MiddleOperator), name.Replace("Signed", "Unsigned"));
      }

      List<MiddleCode> shortList = new List<MiddleCode>(),
                       longList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);    
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);

      AddMiddleCode(longList, middleOp, resultSymbol, leftSymbol, rightSymbol);
      return (new Expression(resultSymbol, shortList, longList));      
    }*/
  }
}
