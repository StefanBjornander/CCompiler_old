﻿using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class Type {
    private Sort m_sort;

    public Sort Sort {
      get { return m_sort; }
    }

    public Type(Sort sort) { // arithmetic or logical
      m_sort = sort;
    }
  
    // ------------------------------------------------------------------------
  
    private BigInteger? m_bitfieldMask = null;

    public bool IsBitfield() {
      return (m_bitfieldMask != null);
    }

    public BigInteger? GetBitfieldMask() {
      return m_bitfieldMask;
    }

    public void SetBitfieldMask(int bits) {
      m_bitfieldMask = (BigInteger) (Math.Pow(2, bits) - 1);
    }

    // ------------------------------------------------------------------------
  
    private Type m_pointerType;

    public Type(Type pointerType) {
      m_sort = Sort.Pointer;
      m_pointerType = pointerType;
    }

    public Type PointerType {
      get { return m_pointerType; }
      set { m_pointerType = value; }
    }

    // ------------------------------------------------------------------------
  
    private int m_arraySize;
    private Type m_arrayType;

    public Type(int arraySize, Type arrayType) {
      m_sort = Sort.Array;
      m_arraySize = arraySize;
      m_arrayType = arrayType;
    }

    public int ArraySize {
      get { return m_arraySize; }
      set { m_arraySize = value; }
    }

    public Type ArrayType {
      get { return m_arrayType; }
      set { m_arrayType = value; }
    }

    public Type PointerOrArrayType {
      get { return (m_sort == Sort.Pointer) ? m_pointerType : m_arrayType; }
    }

    // ------------------------------------------------------------------------

    public enum FunctionStyle {Old, New};
    private FunctionStyle m_functionStyle;

    private Type m_returnType;
    private List<string> m_nameList;
    private List<Symbol> m_parameterList; 
    private List<Type> m_typeList;
    private bool m_variadic;

    public Type(Type returnType, List<string> nameList) {
      m_sort = Sort.Function;
      m_functionStyle = FunctionStyle.Old;
      m_returnType = returnType;
      m_nameList = nameList;
      m_parameterList = null;
      m_variadic = false;

      Assert.Error(nameList.Count == new HashSet<string>(nameList).Count,
                   null, Message.Duplicate_name_in_parameter_list);
    }

    public Type(Type returnType, List< Symbol> parameterList,
                bool variadic) {
      Assert.ErrorXXX(parameterList != null);
      m_sort = Sort.Function;
      m_functionStyle = FunctionStyle.New;
      m_returnType = returnType;
      m_nameList = null;
      m_parameterList = parameterList;
      m_variadic = variadic;
      m_typeList = null;

      if (parameterList != null) {
        m_typeList = new List<Type>();

        foreach (Symbol symbol in parameterList) {
          m_typeList.Add(symbol.Type);
        }
      }
    }

    public FunctionStyle Style {
      get { return m_functionStyle; }
    }

    public List<string> NameList {
      get { return m_nameList; }
    }

    public List<Symbol> ParameterList {
      get { return m_parameterList; }
    }

    public List<Type> TypeList {
      get { return m_typeList; }
    }

    public Type ReturnType {
      get { return m_returnType; }
      set { m_returnType = value; }
    }

    public bool IsVariadic() {
      return m_variadic;
    }

    // ------------------------------------------------------------------------
  
    private IDictionary<string,Symbol> m_memberMap;
    private List<Symbol> m_memberList;
    
    public Type(Sort sort, IDictionary<string,Symbol> symbolMap,
                List<Symbol> symbolList) {
      m_sort = sort;
      m_memberMap = symbolMap;
      m_memberList = symbolList;
    }

    public IDictionary<string,Symbol> MemberMap {
      get { return m_memberMap; }
      set { m_memberMap = value; }
    }

    public List<Symbol> MemberList {
      get { return m_memberList; }
      set { m_memberList = value; }
    }

    // ------------------------------------------------------------------------

    private ISet<Symbol> m_enumItemSet;

    public Type(ISet<Symbol> enumItemSet) {
      m_sort = Sort.SignedInt;
      m_enumItemSet = enumItemSet;
    }

    public ISet<Symbol> EnumItemSet {
      get { return m_enumItemSet; }
    }

    // ------------------------------------------------------------------------

    public static bool IsSigned(Sort sort) {
      return (sort == Sort.SignedChar) || (sort == Sort.SignedShortInt) ||
             (sort == Sort.SignedInt) || (sort == Sort.Signed_Long_Int);
    }
        
    public int Size() {
      switch (m_sort) {
        case Sort.Array:
          return m_arraySize * m_arrayType.Size();

        case Sort.Struct:
            if (m_memberMap != null) {
              int size = 0;

              foreach (Symbol symbol in m_memberMap.Values) {
                size += symbol.Type.Size();
              }

              return size;
            }        
            else {
              return 0;
            }

        case Sort.Union:
            if (m_memberMap == null) {
              int size = 0;

              foreach (Symbol symbol in m_memberMap.Values) {
                size = Math.Max(size, symbol.Type.Size());
              }

              return size;
            }
            else {
              return 0;
            }

        case Sort.Logical:
            return TypeSize.SignedIntegerSize;

        default:
          return TypeSize.Size(m_sort);
      }
    }

    public int ReturnSize() {
      switch (m_sort) {
        case Sort.Struct:
        case Sort.Union:
        case Sort.Array:
          return TypeSize.PointerSize;

        default:
          return Size();
      }
    }

    public int SizeArray() {
      switch (m_sort) {
        case Sort.Array:
        case Sort.Function:
        case Sort.String:
          return TypeSize.PointerSize;

        default:
          return Size();
      }
    }

    public bool IsComplete() {
      switch (m_sort) {
        case Sort.Array:
          return (m_arraySize > 0);

        case Sort.Struct:
        case Sort.Union:
          return (m_memberMap != null);

        default:
          return true;
      }
    }

    // ------------------------------------------------------------------------

    private bool m_constant, m_volatile;

    public bool Constant {
      get { return m_constant; }
      set { m_constant = value; }
    }
  
    public bool Volatile {
      get { return m_volatile; }
      set { m_volatile = value; }
    }

    public bool IsConstantRecursive() {
      if (m_constant) {
        return true;
      }  
      else if (IsStructOrUnion() && (m_memberMap != null)) {
        foreach (Symbol symbol in m_memberMap.Values) {
          if (symbol.Type.IsConstantRecursive()) {
            return true;
          }
        }
      }
    
      return false;
    }

    // ------------------------------------------------------------------------
  
    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is Type) {
        Type type = (Type) obj;

        if ((m_constant == type.m_constant) &&
            (m_volatile == type.m_volatile) && (m_sort == type.m_sort)) {
          switch (m_sort) {
            case Sort.Pointer:
              return m_pointerType.Equals(type.m_pointerType);

            case Sort.Array:
              return ((m_arraySize == 0) || (type.m_arraySize == 0) ||
                      (m_arraySize == type.m_arraySize)) &&
                      m_arrayType.Equals(type.m_arrayType);

            case Sort.Struct:
            case Sort.Union:
              return ((m_memberMap == null) && (type.m_memberMap == null)) ||
                     ((m_memberMap != null) && (type.m_memberMap != null) &&
                      m_memberMap.Equals(type.m_memberMap));

            case Sort.Function:
              return m_returnType.Equals(type.m_returnType) &&
                     (((m_typeList == null) && (type.m_typeList == null)) ||
                      ((m_typeList != null) && (type.m_typeList != null) &&
                     m_typeList.SequenceEqual(type.m_typeList))) &&
                     (m_variadic == type.m_variadic);

            default:
              return true;
          }
        }
      }

      return false;
    }

    // ------------------------------------------------------------------------
  
    public bool IsVoid() {
      return (m_sort == Sort.Void);
    }

    public bool IsChar() {
      return (m_sort == Sort.SignedChar) || (m_sort == Sort.UnsignedChar);
    }

    public bool IsShort() {
      return (m_sort == Sort.SignedShortInt) ||
             (m_sort == Sort.UnsignedShortInt);
    }

    public bool IsInteger() {
      return (m_sort == Sort.SignedInt) || (m_sort == Sort.Unsigned_Int);
    }

    public bool IsIntegral() {
      return IsSigned() || IsUnsigned();
    }

    public bool IsSigned() {
      switch (m_sort) {
        case Sort.SignedChar:
        case Sort.SignedShortInt:
        case Sort.SignedInt:
        case Sort.Signed_Long_Int:
          return true;

        default:
          return false;
      }
    }

    public bool IsUnsigned() {
      switch (m_sort) {
        case Sort.UnsignedChar:
        case Sort.UnsignedShortInt:
        case Sort.Unsigned_Int:
        case Sort.UnsignedLongInt:
          return true;

        default:
          return false;
      }
    }

    public bool IsFloat() {
      return (m_sort == Sort.Float);
    }
  
    public bool IsFloating() {
      switch (m_sort) {
        case Sort.Float:
        case Sort.Double:
        case Sort.LongDouble:
          return true;

        default:
          return false;
      }
    }

    public bool IsLogical() {
      return (m_sort == Sort.Logical);
    }

    public bool IsPointer() {
      return (m_sort == Sort.Pointer);
    }

    public bool IsArray() {
      return (m_sort == Sort.Array);
    }

    public bool IsPointerOrArray() {
      return IsPointer() || IsArray();
    }

    public bool IsPointerArrayOrString() {
      return IsPointerOrArray() || IsString();
    }

    public bool IsFunction() {
      return (m_sort == Sort.Function);
    }

    public bool IsPointerArrayStringOrFunction() {
      return IsPointer() || IsArrayFunctionOrString ();
    }

    public bool IsString() {
      return (m_sort == Sort.String);
    }

    public bool IsArrayFunctionOrString() {
      return IsArray() || IsFunction() || IsString();
    }

    public bool IsFunctionPointer() {
      return (m_sort == Sort.Pointer) && m_pointerType.IsFunction();
    }

    public bool IsArithmetic() {
      return IsIntegral() || IsFloating();
    }

    public bool IsIntegralOrPointer() {
      return IsIntegral() || IsPointer();
    }

    public bool IsIntegralLogicalOrPointer() {
      return IsIntegral() || IsLogical() || IsPointer();
    }

    public bool IsIntegralPointerOrArray() {
      return IsIntegral() || IsPointerOrArray();
    }

    public bool IsIntegralPointerArrayOrString() {
      return IsIntegralPointerOrArray() || IsString();
    }

    public bool IsIntegralPointerArrayStringOrFunction() {
      return IsIntegralPointerArrayOrFunction() || IsString();
    }

    public bool IsIntegralPointerOrFunction() {
      return IsIntegralOrPointer() || IsFunction();
    }

    public bool IsIntegralPointerArrayOrFunction() {
      return IsIntegralPointerOrArray() || IsFunction();
    }

    public bool IsArithmeticOrPointer() {
      return IsArithmetic() || IsPointer();
    }

    public bool IsArithmeticPointerArrayStringOrFunction() {
      return IsArithmeticOrPointer() || IsArray() || IsFunction() || IsString();
    }

    public bool IsStruct() {
      return (m_sort == Sort.Struct);
    }

    public bool IsUnion() {
      return (m_sort == Sort.Union);
    }
  
    public bool IsStructOrUnion() {
      return IsStruct() || IsUnion();
    }
  
    public bool IsEnumerator() {
      return (m_enumItemSet != null);
    }

    public override string ToString() {
      return Enum.GetName(typeof(Sort), m_sort).Replace("_", " ").ToLower();
    }

    public static Type SignedShortIntegerType =
      new Type(Sort.SignedShortInt);
    public static Type UnsignedShortIntegerType =
      new Type(Sort.UnsignedShortInt);
    public static Type SignedIntegerType = new Type(Sort.SignedInt);
    public static Type UnsignedIntegerType = new Type(Sort.Unsigned_Int);
    public static Type SignedLongIntegerType = new Type(Sort.Signed_Long_Int);
    public static Type UnsignedLongIntegerType =
      new Type(Sort.UnsignedLongInt);
    public static Type FloatType = new Type(Sort.Float);
    public static Type DoubleType = new Type(Sort.Double);
    public static Type LongDoubleType = new Type(Sort.LongDouble);
    public static Type SignedCharType = new Type(Sort.SignedChar);
    public static Type UnsignedCharType = new Type(Sort.UnsignedChar);
    public static Type StringType = new Type(Sort.String);
    public static Type IntegerPointerType = new Type(SignedIntegerType);
    public static Type VoidPointerType = new Type(new Type(Sort.Void));
    public static Type LogicalType = new Type(Sort.Logical);
  }
}
