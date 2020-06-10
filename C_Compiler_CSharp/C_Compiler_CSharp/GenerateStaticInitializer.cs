using System.Collections.Generic;

namespace CCompiler {
  public class GenerateStaticInitializer {
    public static List<MiddleCode> GenerateStatic(Type toType,
                                                  object fromInitializer) {
      Assert.ErrorXXX((fromInitializer is Expression) ||
                    (fromInitializer is List<object>));
      List<MiddleCode> codeList = new List<MiddleCode>();

      if (fromInitializer is Expression) {
        Expression fromExpression = (Expression) fromInitializer;
        Symbol fromSymbol = fromExpression.Symbol;
        Assert.Error(fromSymbol.IsExternOrStatic(), fromSymbol,
                     Message.Non__static_initializer);
        Type fromType = fromSymbol.Type;

        if (toType.IsArray() && toType.ArrayType.IsChar() &&
            fromType.IsString()) {
          string text = (string) fromSymbol.Value;
 
          if (toType.ArraySize == 0) {
            toType.ArraySize = text.Length + 1;
          }
          else {
            Assert.Error(text.Length < toType.ArraySize,
                         toType, Message.Too_many_initializers);
          }

          codeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                      fromSymbol.Type.Sort, text));
        }
        else if (toType.IsPointer() && fromType.IsArrayFunctionOrString()) {
          Assert.ErrorXXX((fromType.IsString() && toType.PointerType.IsChar())
                        ||(fromType.IsArray() &&
                           fromType.ArrayType.Equals(toType.PointerType)) ||
                        (fromType.IsFunction() &&
                         fromType.Equals(toType.PointerType)));
          StaticAddress staticAddress =
            new StaticAddress(fromSymbol.UniqueName, 0);
          codeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                      toType.Sort, staticAddress));
        }
        else {
          Expression toExpression =
            TypeCast.ImplicitCast(fromExpression, toType);
          Symbol toSymbol = toExpression.Symbol;
          Assert.Error(toSymbol.Value != null, toSymbol,
                       Message.Non__constant_expression);
          codeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                      toSymbol.Type.Sort, toSymbol.Value));
        }
      }
      else {
        Assert.Error(toType.IsArray() || toType.IsStructOrUnion(),
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

              foreach (object value in fromList) {
                codeList.AddRange(GenerateStatic(toType.ArrayType, value));
              }

              int restSize = toType.Size() -
                             (fromList.Count * toType.ArrayType.Size());
              if (restSize > 0) {
                codeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                            restSize));
              }
            }
            break;
          
          case Sort.Struct:
          case Sort.Union: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;
              Assert.Error((toType.IsStruct() &&
                           (fromList.Count <= memberMap.Count)) ||
                           (toType.IsUnion() && (fromList.Count == 1)),
                           toType, Message.Too_many_initializers);

              int size = 0;
              IEnumerator<Symbol> enumerator =
                memberMap.Values.GetEnumerator();
              foreach (object fromInitializor in fromList) {
                enumerator.MoveNext();
                Symbol memberSymbol = enumerator.Current;
                codeList.AddRange(GenerateStatic(memberSymbol.Type,
                                                 fromInitializor));
                size += memberSymbol.Type.Size();
              }

              int restSize = toType.Size() - size;
              if (restSize > 0) {
                codeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                            restSize));
              }
            }
            break;
        }
      }

      return codeList;
    }
  }
}
