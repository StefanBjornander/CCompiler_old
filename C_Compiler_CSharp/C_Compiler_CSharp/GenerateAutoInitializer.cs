using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  class GenerateAutoInitializer {
    public static List<MiddleCode> GenerateAuto(Symbol toSymbol,
                                                object fromInitializer) {
      Assert.ErrorXXX((fromInitializer is Expression) ||
                      (fromInitializer is List<object>));
      Type toType = toSymbol.Type;
      List<MiddleCode> codeList = new List<MiddleCode>();

      if (fromInitializer is Expression) {
        Expression fromExpression = (Expression) fromInitializer;

        if (toType.IsArray() && toType.ArrayType.IsChar() &&
            fromExpression.Symbol.Type.IsString()) {
          string text = ((string) fromExpression.Symbol.Value) + "0";
          List<object> list = new List<object>();

          foreach (char c in text) {
            Symbol charSymbol = new Symbol(toType.ArrayType, (BigInteger) ((int) c));
            Expression charExpression = new Expression(charSymbol, null, null);
            list.Add(charExpression);
          }

          return GenerateAuto(toSymbol, list);
        }
        else {
          fromExpression = TypeCast.ImplicitCast(fromExpression, toType);
          codeList.AddRange(fromExpression.LongList);      
      
          if (toSymbol.Type.IsFloating()) {
            codeList.Add(new MiddleCode(MiddleOperator.PopFloat, toSymbol));
          }
          else {
            codeList.Add(new MiddleCode(MiddleOperator.Assign, toSymbol,
                                        fromExpression.Symbol));
          }
        }
      }
      else {
        Assert.Error(toType.IsArray() ||toType.IsStructOrUnion(),
                     toType, Message.
            Only_array_struct_or_union_can_be_initialized_by_a_list);
        List<object> fromList = (List<object>) fromInitializer;

        switch (toType.Sort) {
          case Sort.Array: {
              fromList = ModifyInitializer.ModifyArray(toType, fromList);

              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }
              else {
                Assert.Error(fromList.Count <= toType.ArraySize,
                             toType, Message.Too_many_initializers);
              }

              for (int index = 0; index < fromList.Count; ++index) {
                Symbol indexSymbol = new Symbol(toType.ArrayType);
                indexSymbol.Offset = toSymbol.Offset +
                                   (index * toType.ArrayType.Size());
                indexSymbol.Name = toSymbol.Name + "[" + index + "]";
                codeList.AddRange(GenerateAuto(indexSymbol, fromList[index]));
              }
            }
            break;

          case Sort.Struct:
          case Sort.Union: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;
              Assert.Error((toType.IsStruct() && (fromList.Count <= memberMap.Count)) ||
                           (toType.IsUnion() && (fromList.Count == 1)),
                           toType, Message.Too_many_initializers);

              IEnumerator<Symbol> enumerator = memberMap.Values.GetEnumerator();
              foreach (object fromInitializor in fromList) {
                enumerator.MoveNext();
                Symbol memberSymbol = enumerator.Current;
                Symbol subSymbol = new Symbol(memberSymbol.Type); 
                subSymbol.Name = toSymbol.Name + "." + memberSymbol.Name;
                subSymbol.Offset = toSymbol.Offset + memberSymbol.Offset;
                codeList.AddRange(GenerateAuto(subSymbol, fromInitializor));
              }
            }
            break;
        }
      }

      return codeList;
    }
  }
}