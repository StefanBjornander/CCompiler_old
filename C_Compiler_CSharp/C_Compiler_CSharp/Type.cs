using System;
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

    public BigInteger? BitfieldMask() {
      return m_bitfieldMask;
    }

    public void SetBitfieldMask(int bits) {
      m_bitfieldMask = ((BigInteger) Math.Pow(2, bits) - ((BigInteger) 1));
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
      get {
        return (m_sort == Sort.Pointer) ? m_pointerType : m_arrayType;
      }
    }

    // ------------------------------------------------------------------------

    public enum FunctionStyle {Old, New};
    private FunctionStyle m_functionStyle;

    private Type m_returnType;
    private List<string> m_nameList;
    private List<Pair<string,Symbol>> m_parameterList;
    private List<Type> m_typeList;
    private bool m_ellipse;

    public Type(Type returnType, List<string> nameList) {
      m_sort = Sort.Function;
      m_functionStyle = FunctionStyle.Old;
      m_returnType = returnType;
      m_nameList = nameList;
      m_parameterList = null;
      m_ellipse = false;

      Assert.Error(nameList.Count == new HashSet<string>(nameList).Count,
                   null, Message.Duplicate_name_in_parameter_list);
    }

    public Type(Type returnType, List<Pair<string,Symbol>> parameterList,
                bool ellipse) {
      Assert.ErrorXXX(parameterList != null);
      m_sort = Sort.Function;
      m_functionStyle = FunctionStyle.New;
      m_returnType = returnType;
      m_nameList = null;
      m_parameterList = parameterList;
      m_ellipse = ellipse;
      m_typeList = null;
    
      if (parameterList != null) {
        m_typeList = new List<Type>();

        foreach (Pair<string,Symbol> pair in parameterList) {
          m_typeList.Add(pair.Second.Type);
        }
      }
    }

    public FunctionStyle Style {
      get { return m_functionStyle; }
    }

    public List<string> NameList {
      get { return m_nameList; }
    }

    public List<Pair<string,Symbol>> ParameterList {
      get { return m_parameterList; }
    }

    public List<Type> TypeList {
      get { return m_typeList; }
    }

    public Type ReturnType {
      get { return m_returnType; }
      set { m_returnType = value; }
    }

    public bool IsEllipse() {
      return m_ellipse;
    }

    // ------------------------------------------------------------------------
  
    private IDictionary<string,Symbol> m_memberMap;
    
    public Type(Sort sort, IDictionary<string,Symbol> symbolMap) {
      m_sort = sort;
      m_memberMap = symbolMap;
    }

    public IDictionary<string,Symbol> MemberMap {
      get { return m_memberMap; }
      set { m_memberMap = value; }
    }

    // ------------------------------------------------------------------------
  
    private ISet<Pair<Symbol,bool>> m_enumeratorItemSet;
    private bool m_enumeratorItem;

    public Type(Sort sort, bool enumeratorItem) {
      m_sort = sort;
      m_enumeratorItem = enumeratorItem;
    }

    public bool EnumeratorItem {
      get { return m_enumeratorItem; }
    }

    public Type(Sort sort, ISet<Pair<Symbol,bool>> enumSet) {
      m_sort = sort;
      m_enumeratorItemSet = enumSet;
    }

    public ISet<Pair<Symbol,bool>> EnumerationItemSet {
      get { return m_enumeratorItemSet; }
    }

    // ------------------------------------------------------------------------

    public static bool IsSigned(Sort sort) {
      return (sort == Sort.Signed_Char) || (sort == Sort.Signed_Short_Int) ||
             (sort == Sort.Signed_Int) || (sort == Sort.Signed_Long_Int);
    }
        
    public int SizeX() {
      switch (m_sort) {
        case Sort.Array:
          return TypeSize.PointerSize;

        default:
          return Size();
      }
    }

    public int Size() {
      switch (m_sort) {
        case Sort.Array:
          return m_arraySize * m_arrayType.Size();

        case Sort.Struct: {
            int size = 0;

            foreach (Symbol symbol in m_memberMap.Values) {
              size += symbol.Type.Size();
            }
        
            return size;
          }

        case Sort.Union: {
            int size = 0;

            foreach (Symbol symbol in m_memberMap.Values) {
              size = Math.Max(size, symbol.Type.Size());
            }

            return size;
          }

        case Sort.Logical:
            return TypeSize.SignedIntegerSize;

        default:
          return TypeSize.Size(m_sort);
      }
    }

    public int ConvertedSize() {
      switch (m_sort) {
        case Sort.Array:
        case Sort.Function:
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
  
    private bool m_constant;
    private bool m_volatile;

    public bool IsConstant {
      get { return m_constant; }
      set { m_constant = value; }
    }
  
    public bool IsVolatile {
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

    public bool Volatile {
      get { return m_volatile; }
      set { m_volatile = value; }
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
                       m_typeList.SequenceEqual(type.m_typeList)));

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
      return (m_sort == Sort.Signed_Char) || (m_sort == Sort.Unsigned_Char);
    }

    public bool IsShort() {
      return (m_sort == Sort.Signed_Short_Int) ||
             (m_sort == Sort.Unsigned_Short_Int);
    }

    public bool IsInteger() {
      return (m_sort == Sort.Signed_Int) || (m_sort == Sort.Unsigned_Int);
    }

    public bool IsIntegral() {
      return IsSigned() || IsUnsigned();
    }

    public bool IsSigned() {
      switch (m_sort) {
        case Sort.Signed_Char:
        case Sort.Signed_Short_Int:
        case Sort.Signed_Int:
        case Sort.Signed_Long_Int:
          return true;

        default:
          return false;
      }
    }

    public bool IsUnsigned() {
      switch (m_sort) {
        case Sort.Unsigned_Char:
        case Sort.Unsigned_Short_Int:
        case Sort.Unsigned_Int:
        case Sort.Unsigned_Long_Int:
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
        case Sort.Long_Double:
          return true;

        default:
          return false;
      }
    }

    public bool IsLogical() {
      return (m_sort == Sort.Logical);
    }

    public bool IsLogicalOrIntegral() {
      return IsLogical() || IsIntegral();
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

    public bool IsFunction() {
      return (m_sort == Sort.Function);
    }

    public bool IsArrayOrFunction() {
      return IsArray() || IsFunction();
    }

    public bool IsPointerArrayStringOrFunction() {
      return IsPointer() || IsArrayOrFunction()  || IsString();
    }

    public bool IsString() {
      return (m_sort == Sort.String);
    }

    public bool IsArrayFunctionOrString() {
      return IsArrayOrFunction() || IsString();
    }

    public bool IsFunctionPointer() {
      return (m_sort == Sort.Pointer) && m_pointerType.IsFunction();
    }

    public bool IsFunctionOrArray() {
      return IsFunction() || IsArray();
    }

    public bool IsArrayStringOrFunction() {
      return IsFunctionOrArray() || IsString();
    }

    public bool IsArrayPointerStringOrFunction() {
      return IsPointer() || IsFunctionOrArray();
    }

    public bool IsArrayFunctionStringStructOrUnion() {
      return IsFunctionOrArray() || IsString() || IsStructOrUnion();
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

    public bool IsIntegralOrArray() {
      return IsIntegral() || IsArray();
    }

    public bool IsIntegralArrayOrPointer() {
      return IsIntegral() || IsPointer() || IsArray();
    }

    public bool IsIntegralPointerArrayOrString() {
      return IsIntegralArrayOrPointer() || IsString();
    }

    public bool IsIntegralPointerArrayStringOrFunction() {
      return IsIntegralPointerArrayOrString() || IsFunction();
    }

    
    public bool IsIntegralPointerOrFunction() {
      return IsIntegralOrPointer() || IsFunction();
    }

    public bool IsIntegralPointerArrayOrFunction() {
      return IsIntegralArrayOrPointer() || IsFunction();
    }

    public bool IsArithmeticOrPointer() {
      return IsArithmetic() || IsPointer();
    }

    public bool IsArithmeticPointerOrArray() {
      return IsArithmeticOrPointer() || IsArray();
    }

    public bool IsArithmeticPointerArrayOrFunction() {
      return IsArithmeticPointerOrArray() || IsFunction();
    }

    public bool IsArithmeticPointerArrayStringOrFunction() {
      return IsArithmeticPointerArrayOrFunction() || IsString();
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
  
    public bool IsArithmeticPointerStructOrUnion() {
      return IsArithmeticOrPointer() || IsStructOrUnion();
    }
  
    public bool IsEnumerator() {
      return (m_enumeratorItemSet != null);
    }

    public override string ToString() {
      return Enum.GetName(typeof(Sort), m_sort).
                  Replace("__", "-").Replace("_", " ").ToLower();
    }

    public static Type SignedShortIntegerType =
      new Type(Sort.Signed_Short_Int);
    public static Type UnsignedShortIntegerType =
      new Type(Sort.Unsigned_Short_Int);
    public static Type SignedIntegerType = new Type(Sort.Signed_Int);
    public static Type UnsignedIntegerType = new Type(Sort.Unsigned_Int);
    public static Type SignedLongIntegerType = new Type(Sort.Signed_Long_Int);
    public static Type UnsignedLongIntegerType =
      new Type(Sort.Unsigned_Long_Int);
    public static Type FloatType = new Type(Sort.Float);
    public static Type DoubleType = new Type(Sort.Double);
    public static Type LongDoubleType = new Type(Sort.Long_Double);
    public static Type SignedCharType = new Type(Sort.Signed_Char);
    public static Type UnsignedCharType = new Type(Sort.Unsigned_Char);
    public static Type StringType = new Type(Sort.String);
    public static Type VoidType = new Type(Sort.Void);
    public static Type PointerTypeX = new Type(SignedIntegerType);
    public static Type VoidPointerType = new Type(new Type(Sort.Void));
    public static Type LogicalType = new Type(Sort.Logical);
  }
}
