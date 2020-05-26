using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class  {
    private static List<object> TextToCharList(string text) {
      List<object> list = new List<object>();

      foreach (char c in text) {
        Symbol charSymbol = new Symbol(Type.SignedCharType, (long) c);
        list.Add(new Expression(charSymbol, new List<MiddleCode>(), new List<MiddleCode>()));
      }
      
      Symbol endSymbol = new Symbol(Type.SignedCharType, (long) 0);
      list.Add(new Expression(endSymbol, new List<MiddleCode>(), new List<MiddleCode>()));
      return list;
    }
  
    public static void GenerateStatic(Type toType, object fromInit,
                                      List<sbyte> byteList, IDictionary<int,string> accessMap) {
      if (fromInit is Expression) {
        Symbol fromSymbol = ((Expression) fromInit).Symbol();
        Assert.Error(fromSymbol.IsStaticOrExtern(), fromSymbol,
                     Message.Non__static_initializer);
        Type fromType = fromSymbol.Type;

        if (toType.IsArray() && fromType.IsString()) {
          Assert.Error(toType.ArrayType.IsChar());
          fromInit = TextToCharList((string) fromSymbol.Value);
        }
    
        if (fromType.IsArrayFunctionOrString() && toType.IsPointer()) {
          Assert.Error((fromType.IsString() && toType.PointerType.IsChar()) ||
                       (fromType.IsArray() && fromType.ArrayType.Equals(toType.PointerType)) ||
                       (fromType.IsFunction() && fromType.Equals(toType.PointerType)));
          accessMap[byteList.Count] = fromSymbol.UniqueName;
          byteList.Add((sbyte) 0);
          byteList.Add((sbyte) 0);
        }
        else if (fromSymbol.Value is StaticAddress) {
          Assert.Error(fromType.IsPointer()  && toType.IsPointer());
          StaticAddress staticAddress = (StaticAddress) fromSymbol.Value;
          accessMap[byteList.Count] = staticAddress.UniqueName;
          byteList.Add((sbyte) staticAddress.Offset);
          byteList.Add((sbyte) (staticAddress.Offset >> 8));
        }
        else if (fromSymbol.Value is StaticValue) {
          Assert.Error(fromType.Equals(toType));
          StaticValue staticValue = (StaticValue) fromSymbol.Value;

          foreach (KeyValuePair<int,string> entry in staticValue.AccessMap) {
            accessMap[byteList.Count + entry.Key] = entry.Value;
          }

          byteList.AddRange(staticValue.ByteList);
        }
        else {
          Symbol toSymbol = TypeCast.ImplicitCast(null, fromSymbol, toType);
        
          foreach (KeyValuePair<int,string> entry in toSymbol.StaticSymbol.AccessMap) {
            accessMap[byteList.Count + entry.Key] = entry.Value;
          }

          byteList.AddRange(toSymbol.StaticSymbol.ByteList);
        }
      }
      else {
        List<object> fromList = (List<object>) fromInit;
      
        switch (toType.Sort) {
          case Sort.Array: {
              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }

              int toByteListSize = byteList.Count + toType.Size();
              Assert.Error(fromList.Count <= toType.ArraySize, toType, Message.Too_many_initializers);
              //Assert.Warning(fromList.Count == toType.ArraySize, toType, Message.Too_few_initializers);
              Type subType = toType.ArrayType;           
            
              foreach (object value in fromList) {
                GenerateStatic(subType, value, byteList, accessMap);
              }

              GenerateZeroByteList(toByteListSize - byteList.Count, byteList);
            }          
            break;
          
          case Sort.Struct: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;
              Assert.Error(fromList.Count <= memberMap.Count,
                           toType, Message.Too_many_initializers);
              /*Assert.Warning(fromList.Count == memberMap.Count,
                             toType, Message.Too_few_initializers);*/
            
              int toByteListSize = byteList.Count + toType.Size();
              KeyValuePair<string,Symbol>[] memberArray = new KeyValuePair<string,Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberArray[index].Value;
                object init = ModifyInitializer.DoInit(memberSymbol.Type, fromList[index]);
                GenerateStatic(memberSymbol.Type, init, byteList, accessMap);
              }

              GenerateZeroByteList(toByteListSize - byteList.Count, byteList);
            }
            break;
          
          case Sort.Union: {
              Assert.Error(fromList.Count == 1, toType, Message.A_union_can_be_initalized_by_one_value_only);
              int toByteListSize = byteList.Count + toType.Size();
              IDictionary<string, Symbol> memberMap = toType.MemberMap;
              Symbol firstSymbol = memberMap.Values.GetEnumerator().Current;
              object init = ModifyInitializer.DoInit(firstSymbol.Type, fromList[0]);
              GenerateStatic(firstSymbol.Type, init, byteList, accessMap);
              GenerateZeroByteList(toByteListSize - byteList.Count, byteList);
            }
            break;
        }
      }
    }
  
    public static void GenerateAuto(List<MiddleCode> codeList, Symbol toSymbol, object fromInit) {
      Type toType = toSymbol.Type;
    
      if (toType.IsArray() && toType.ArrayType.IsChar() &&
          (fromInit is Expression) && ((Expression) fromInit).Symbol().Type.IsString()) {
        fromInit = TextToCharList((string) ((Expression) fromInit).Symbol().Value);
      }
    
      if (fromInit is Expression) {
        Expression initExpr = (Expression) fromInit;
        codeList.AddRange(initExpr.LongList());      
        Symbol initSymbol = TypeCast.ImplicitCast(codeList, initExpr.Symbol(), toType);
      
        if (toType.IsFloating()) {
          MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.PopFloat, toSymbol);
        }
        else {
          MiddleCodeGenerator.AddMiddleCode(codeList, MiddleOperator.Assign, toSymbol, initSymbol);
        }
      }
      else {
        List<object> fromList = (List<object>) fromInit;
      
        switch (toType.Sort) {
          case Sort.Array: {
              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }

              Assert.Error(fromList.Count <= toType.ArraySize, toType, Message.Too_many_initializers);
              //Assert.Warning(fromList.Count == toType.ArraySize, toType, Message.Too_few_initializers);
              Type arrayType = toType.ArrayType;
            
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol subSymbol = new Symbol(arrayType, false);
                subSymbol.Offset = toSymbol.Offset + (index * arrayType.Size());
                subSymbol.Name = toSymbol.Name + "[" + index + "]";
                GenerateAuto(codeList, subSymbol, fromList[index]);
              }
            }
            break;
          
          case Sort.Struct: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;            
              Assert.Error(fromList.Count <= memberMap.Count,
                           toType, Message.Too_many_initializers);
              /*Assert.Warning(fromList.Count == memberMap.Count,
                             toType, Message.Too_few_initializers);*/

              KeyValuePair<string, Symbol>[] memberArray = new KeyValuePair<string, Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);
              
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberArray[index].Value;
                Symbol subSymbol = new Symbol(memberSymbol.Type, false);
                subSymbol.Name = toSymbol.Name + Symbol.SeparatorId + memberSymbol.Name;
                subSymbol.Offset = toSymbol.Offset + memberSymbol.Offset;
                GenerateAuto(codeList, subSymbol, fromList[index]);
              }
            }
            break;
          
          case Sort.Union: {
              Assert.Error(fromList.Count == 1, toType,
                           Message.A_union_can_be_initalized_by_one_value_only);
              IDictionary<string, Symbol> memberMap = toType.MemberMap;
              Symbol firstSymbol = memberMap.Values.GetEnumerator().Current;
              Symbol subSymbol = new Symbol(firstSymbol.Type);
              subSymbol.Name = toSymbol.Name + Symbol.SeparatorId + firstSymbol.Name;
              subSymbol.Offset = toSymbol.Offset;
              GenerateAuto(codeList, subSymbol, fromList[0]);
            }
            break;
        }
      }
    }

    public static void GenerateByteList(Type type, object value, List<sbyte> sbyteList,
                                        IDictionary<int,string> accessMap) {
      if (type.IsString()) {
        foreach (char c in ((string) value)) {
          sbyteList.Add((sbyte) c);
        }
        sbyteList.Add((sbyte) 0);
      }
      else if (value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) value;
        accessMap[sbyteList.Count] = staticAddress.UniqueName;
        sbyteList.Add((sbyte) ((short) staticAddress.Offset));
        sbyteList.Add((sbyte) (((short) staticAddress.Offset) >> 8));
      }
      else if (value is StaticValue) {
        StaticValue staticValue = (StaticValue) value;

        foreach (KeyValuePair<int,string> pair in staticValue.AccessMap) {
          accessMap[pair.Key + sbyteList.Count] = pair.Value;
        }

        sbyteList.AddRange(staticValue.ByteList);
      }
      else {
        switch (type.Sort) {
          case Sort.Signed_Short_Int:
          case Sort.Unsigned_Short_Int: {              
              sbyteList.Add((sbyte) ((long) value));
            }
            break;

          case Sort.Signed_Char:
          case Sort.Unsigned_Char: {
              sbyteList.Add((sbyte)((long) value));
          }
            break;

          case Sort.Signed_Int:
          case Sort.Unsigned_Int:
          case Sort.Pointer: {
              long longValue = (long) value;
              sbyteList.Add((sbyte) longValue);
              sbyteList.Add((sbyte) (longValue >> 8));
            }
            break;

          case Sort.Signed_Long_Int:
          case Sort.Unsigned_Long_Int: {
              long longValue = (long) value;
              sbyteList.Add((sbyte) longValue);
              sbyteList.Add((sbyte) (longValue >> 8));
              sbyteList.Add((sbyte) (longValue >> 16));
              sbyteList.Add((sbyte) (longValue >> 24));
            }
            break;

          case Sort.Float: {
              float floatValue = (float)((decimal) value);
              byte[] byteArray = BitConverter.GetBytes(floatValue);
            
              foreach (byte b in byteArray) {
                sbyteList.Add((sbyte) b);
              }
            }
            break;

          case Sort.Double:
          case Sort.Long_Double: {
              double doubleValue = (double) ((decimal)value);
              byte[] byteArray = BitConverter.GetBytes(doubleValue);

              foreach (byte b in byteArray) {
                sbyteList.Add((sbyte) b);
              }
            }
            break;
        }
      }
    }

    public static void GenerateZeroByteList(int size, List<sbyte> byteList) {
      Assert.Error(size >= 0);
    
      if (size > 0) {
        for (int count = 0; count < size; ++count) {
          byteList.Add((sbyte) 0);
        }
      }
    }
  }
}