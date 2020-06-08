using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class GenerateStaticInitializer {
    public static List<MiddleCode> GenerateStatic(Type toType, object fromInitializer) {      
      List<MiddleCode> middleCodeList = new List<MiddleCode>();

      if (fromInitializer is Expression) {
        Expression fromExpression = (Expression) fromInitializer;
        Symbol fromSymbol = ((Expression) fromInitializer).Symbol;
        Assert.Error(fromSymbol.IsExternOrStatic(), fromSymbol,
                     Message.Non__static_initializer);
        Type fromType = fromSymbol.Type;
        object fromValue = fromSymbol.Value;

        if (toType.IsArray() && fromType.IsString()) {
          Assert.ErrorA(toType.ArrayType.IsChar());
          string text = (string) fromSymbol.Value;
          middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                     fromSymbol.Type.Sort, fromSymbol.Value));
        }
        else if (toType.IsPointer() && fromType.IsString()) {
          Assert.ErrorA(toType.PointerType.IsChar());
          middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer, Sort.Pointer,
                             Symbol.ValueName(toType, fromSymbol.Value)));
        }
        else if (toType.IsPointer() && (fromSymbol.Value is StaticAddress)) {
          Assert.ErrorA(fromType.IsPointer()  && toType.IsPointer());
          middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer, toType.Sort,
                                            fromSymbol.Value));
        }
        else if (toType.IsPointer() && fromType.IsArrayFunctionOrString()) {
          Assert.ErrorA((fromType.IsString() && toType.PointerType.IsChar()) ||
                       (fromType.IsArray() &&
                        fromType.ArrayType.Equals(toType.PointerType)) ||
                       (fromType.IsFunction() &&
                        fromType.Equals(toType.PointerType)));
          if (fromType.IsArray()) {
            StaticAddress staticAddress =
              new StaticAddress(fromSymbol.UniqueName, 0);
            middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                              toType.Sort, staticAddress));
          }
          else {
            middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                              toType.Sort, fromSymbol.Value));
          }
        }
        else {
          Expression toExpression =
            TypeCast.ImplicitCast(fromExpression, toType);
          Symbol toSymbol = toExpression.Symbol;
          Assert.Error(toSymbol.Value != null, toSymbol,
                       Message.Non__constant_expression);
          middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                     fromSymbol.Type.Sort, fromSymbol.Value));
        }
      }
      else {
        List<object> fromList = (List<object>) fromInitializer;
      
        switch (toType.Sort) {
          case Sort.Array: {
              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }

              Assert.Error(!(fromList.Count > toType.ArraySize),
                           toType, Message.Too_many_initializers);
            
              foreach (object value in fromList) {
                middleCodeList.AddRange(GenerateStatic(toType.ArrayType,
                                                       value));
              }

              int restSize = toType.Size() -
                             (fromList.Count * toType.ArrayType.Size());
              middleCodeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                                restSize));
            }
            break;
          
          case Sort.Struct: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;
              Assert.Error(!(fromList.Count > memberMap.Count),
                           toType, Message.Too_many_initializers);
              KeyValuePair<string,Symbol>[] memberArray =
                new KeyValuePair<string,Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);

              int size = 0;
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberArray[index].Value;
                object initializer = ModifyInitializer.DoInitializer(memberSymbol.Type,
                                                       fromList[index]);
                middleCodeList.AddRange(GenerateStatic(memberSymbol.Type,
                                                       initializer));
                size += memberSymbol.Type.Size();
              }

              middleCodeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                                toType.Size() - size));
            }
            break;
          
          case Sort.Union: {
              Assert.Error(fromList.Count == 1, toType,
                       Message.A_union_can_be_initializeralized_by_one_value_only);
              IDictionary<string, Symbol> memberMap = toType.MemberMap;
              Symbol firstSymbol = memberMap.Values.GetEnumerator().Current;
              object initializer =
                ModifyInitializer.DoInitializer(firstSymbol.Type, fromList[0]);
              middleCodeList.AddRange(GenerateStatic(firstSymbol.Type, initializer));
              middleCodeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                    toType.Size() - firstSymbol.Type.Size()));
            }
            break;

          default:
            Assert.Error(toType,
            Message.Only_array_struct_or_union_can_be_initializerialized_by_a_list);
            break;
        }
      }

      return middleCodeList;
    }
  }
}
