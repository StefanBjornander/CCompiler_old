using System;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CCompiler {
  class GenerateAutoInitializer {
    public static List<MiddleCode> GenerateAuto(Symbol toSymbol, object fromInit) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      Type toType = toSymbol.Type;
    
      if (toType.IsArray() && toType.ArrayType.IsChar() &&
          (fromInit is Expression) && ((Expression) fromInit).Symbol.Type.IsString()) {
        string text = (string) ((Expression) fromInit).Symbol.Value;

        if (toType.ArraySize == 0) {
          toType.ArraySize = text.Length + 1;
        }
        else {
          Assert.Error(text.Length < toType.ArraySize, toSymbol,
                       Message.String_does_not_fit_in_array);
        }
        List<object> list = new List<object>();

        for (int index = 0; index < text.Length; ++index) {
          Symbol toCharSymbol = new Symbol(toType.ArrayType, false);
          toCharSymbol.Name = toSymbol.Name + "[" + index + "]";
          toCharSymbol.Offset = toSymbol.Offset + (index * toType.ArrayType.Size());
          Symbol fromCharSymbol = new Symbol(toType.ArrayType, (BigInteger) text[index]);
          codeList.Add(new MiddleCode(MiddleOperator.Assign, toCharSymbol, fromCharSymbol));
        }

        { Symbol toCharSymbol = new Symbol(toType.ArrayType, false);
          toCharSymbol.Name = toSymbol.Name + "[" + text.Length + "]";
          toCharSymbol.Offset = toSymbol.Offset + (text.Length * toType.ArrayType.Size());
          Symbol fromCharSymbol = new Symbol(toType.ArrayType, (BigInteger) '\0');
          codeList.Add(new MiddleCode(MiddleOperator.Assign, toCharSymbol, fromCharSymbol));
        }
      }
      else if (fromInit is Expression) {
        Expression initExpression = TypeCast.ImplicitCast((Expression) fromInit, toType);
        codeList.AddRange(initExpression.LongList);      
      
        if (toType.IsFloating()) {
          codeList.Add(new MiddleCode(MiddleOperator.PopFloat, toSymbol));
        }
        else {
          codeList.Add(new MiddleCode(MiddleOperator.Assign, toSymbol, initExpression.Symbol));
        }
      }
      else {
        List<object> fromList = (List<object>) fromInit;
      
        switch (toType.Sort) {
          case Sort.Array: {
              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }

              Assert.Error(!(fromList.Count > toType.ArraySize), toType, Message.Too_many_initializers);
              //Assert.Error((fromList.Count < toType.ArraySize), toType, Message.Too_few_initializers);
            
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol subSymbol = new Symbol(toType.ArrayType, false);
                subSymbol.Offset = toSymbol.Offset + (index * toType.ArrayType.Size());
                subSymbol.Name = toSymbol.Name + "[" + index + "]";
                codeList.AddRange(GenerateAuto(subSymbol, fromList[index]));
              }
            }
            break;
          
          case Sort.Struct: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;            
              Assert.Error(!(fromList.Count > memberMap.Count), toType, Message.Too_many_initializers);
              Assert.Error(!(fromList.Count < memberMap.Count), toType, Message.Too_few_initializers);
              KeyValuePair<string, Symbol>[] memberArray = new KeyValuePair<string, Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);
              
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberArray[index].Value;
                Symbol subSymbol = new Symbol(memberSymbol.Type, false);
                subSymbol.Name = toSymbol.Name + Symbol.SeparatorDot + memberSymbol.Name;
                subSymbol.Offset = toSymbol.Offset + memberSymbol.Offset;
                codeList.AddRange(GenerateAuto(subSymbol, fromList[index]));
              }
            }
            break;
          
          case Sort.Union: {
              Assert.Error(fromList.Count == 1, toType, Message.A_union_can_be_initalized_by_one_value_only);
              IDictionary<string, Symbol> memberMap = toType.MemberMap;
              Symbol firstSymbol = memberMap.Values.GetEnumerator().Current;
              Symbol subSymbol = new Symbol(firstSymbol.Type, false);
              subSymbol.Name = toSymbol.Name + Symbol.SeparatorId + firstSymbol.Name;
              subSymbol.Offset = toSymbol.Offset;
              codeList.AddRange(GenerateAuto(subSymbol, fromList[0]));
            }
            break;

          default:
            Assert.Error(toType, Message.Only_array_struct_or_union_can_be_initialized_by_a_list);
            break;
        }
      }

      return codeList;
    }
  }
}
