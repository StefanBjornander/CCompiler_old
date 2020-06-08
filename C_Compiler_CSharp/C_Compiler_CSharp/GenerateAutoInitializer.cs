using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  class GenerateAutoInitializer {
    public static List<MiddleCode> GenerateAuto(Symbol toSymbol,
                                                object fromInitializer) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      Type toType = toSymbol.Type;

      if (toType.IsArray() && toType.ArrayType.IsChar() &&
          (fromInitializer is Expression) &&
          ((Expression) fromInitializer).Symbol.Type.IsString()) {
        string text = ((string) (((Expression) fromInitializer).Symbol.Value))
                      + "0";

        if (toType.ArraySize == 0) {
          toType.ArraySize = text.Length;
        }
        else {
          Assert.Error(text.Length <= toType.ArraySize, toSymbol,
                       Message.String_does_not_fit_in_array);
        }
        List<object> list = new List<object>();

        for (int index = 0; index < text.Length; ++index) {
          Symbol toCharSymbol = new Symbol(toType.ArrayType, true);
          toCharSymbol.Name = toSymbol.Name + "[" + index + "]";
          toCharSymbol.Offset = toSymbol.Offset +
                                (index * toType.ArrayType.Size());
          Symbol fromCharSymbol = new Symbol(toType.ArrayType,
                                             (BigInteger) text[index]);
          codeList.Add(new MiddleCode(MiddleOperator.Assign,
                       toCharSymbol, fromCharSymbol));
        }
      }
      else if (fromInitializer is Expression) {
        Expression initializerExpression =
          TypeCast.ImplicitCast((Expression) fromInitializer, toType);
        codeList.AddRange(initializerExpression.LongList);      
      
        if (toType.IsFloating()) {
          codeList.Add(new MiddleCode(MiddleOperator.PopFloat, toSymbol));
        }
        else {
          codeList.Add(new MiddleCode(MiddleOperator.Assign, toSymbol,
                                      initializerExpression.Symbol));
        }
      }
      else {
        List<object> fromList = (List<object>) fromInitializer;

        switch (toType.Sort) {
          case Sort.Array: {
              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }
              else {
                Assert.Error(!(fromList.Count > toType.ArraySize),
                             toType, Message.Too_many_initializers);
              }

              for (int index = 0; index < fromList.Count; ++index) {
                Symbol subSymbol = new Symbol(toType.ArrayType, true);
                subSymbol.Offset = toSymbol.Offset +
                                   (index * toType.ArrayType.Size());
                subSymbol.Name = toSymbol.Name + "[" + index + "]";
                codeList.AddRange(GenerateAuto(subSymbol, fromList[index]));
              }
            }
            break;

          case Sort.Struct: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;            
              Assert.Error(!(fromList.Count > memberMap.Count),
                           toType, Message.Too_many_initializers);
              KeyValuePair<string,Symbol>[] memberArray =
                new KeyValuePair<string, Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);
              
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberArray[index].Value;
                Symbol subSymbol = new Symbol(memberSymbol.Type, true);
                subSymbol.Name = toSymbol.Name + Symbol.SeparatorDot +
                                 memberSymbol.Name;
                subSymbol.Offset = toSymbol.Offset + memberSymbol.Offset;
                codeList.AddRange(GenerateAuto(subSymbol, fromList[index]));
              }
            }
            break;

          case Sort.Union: {
              Assert.Error(fromList.Count == 1, toType,
                Message.A_union_can_be_initializeralized_by_one_value_only);
              IDictionary<string, Symbol> memberMap = toType.MemberMap;
              Symbol firstSymbol = memberMap.Values.GetEnumerator().Current;
              Symbol subSymbol = new Symbol(firstSymbol.Type, true);
              subSymbol.Name = toSymbol.Name + Symbol.SeparatorId +
                               firstSymbol.Name;
              subSymbol.Offset = toSymbol.Offset;
              codeList.AddRange(GenerateAuto(subSymbol, fromList[0]));
            }
            break;

          default:
            Assert.Error(toType, Message.
            Only_array_struct_or_union_can_be_initializerialized_by_a_list);
            break;
        }
      }

      return codeList;
    }
  }
}
