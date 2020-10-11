using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class Symbol {
    public const string NumberId = "#";
    public const string TemporaryId = "£";
    public const string SeparatorId = "$";
    public const string SeparatorDot = ".";
    public const string FileMarker = "@";

    private bool m_externalLinkage, m_functionDefinition;
    private string m_name, m_uniqueName;
    private bool m_parameter;
    private Storage m_storage;
    private Type m_type;
    private object m_value;
    private int m_offset;
    private Symbol m_addressSymbol;
    private int m_addressOffset;
    private ISet<MiddleCode> m_trueSet, m_falseSet;
    private static int UniqueNameCount = 0, TemporaryNameCount = 0;

    public Symbol(string name, bool externalLinkage, Storage storage,
                  Type type, bool parameter = false, object value = null) {
      m_name = name;
      m_externalLinkage = externalLinkage;
      m_storage = storage;

      if (m_externalLinkage) {
        m_uniqueName = m_name;
      }
      else {
        m_uniqueName = Symbol.FileMarker + (UniqueNameCount++) +
                       Symbol.SeparatorId + m_name;
      }

      m_type = type;
      m_parameter = parameter;
      m_value = value;
      CheckValue(m_type, m_value);
    }

    private static void CheckValue(Type type, object value) {
      if (value is BigInteger) {
        BigInteger bigValue = (BigInteger) value;
        Assert.Error((bigValue >= TypeSize.GetMinValue(type.Sort)) &&
                     (bigValue <= TypeSize.GetMaxValue(type.Sort)),
                     type + ": " + value, Message.Value_overflow);
      }
    }

    public Symbol(Type type) {
      m_name = Symbol.TemporaryId + "temporary" + (TemporaryNameCount++);
      m_externalLinkage = false;
      m_storage = Storage.Auto;
      m_type = type;
      m_parameter = false;
    }

    public Symbol(ISet<MiddleCode> trueSet, ISet<MiddleCode> falseSet) {
      m_name = Symbol.TemporaryId + "logical" + (TemporaryNameCount++);
      m_storage = Storage.Auto;
      m_type = new Type(Sort.Logical);
      m_trueSet = (trueSet != null) ? trueSet : (new HashSet<MiddleCode>());
      m_falseSet = (falseSet != null) ? falseSet
                                      : (new HashSet<MiddleCode>());
      m_parameter = false;
    }

    public Symbol(Type type, object value) {
      Assert.ErrorXXX(!(value is bool));
      m_uniqueName = ValueName(type, value);
      m_storage = Storage.Static;
      m_type = type;
      m_value = value;
      m_parameter = false;
      CheckValue(m_type, m_value);
    }

    public static string ValueName(CCompiler.Type type, object value) {
      Assert.ErrorXXX(value != null);

      if (value is string) {
        string text = (string) value;
        StringBuilder buffer = new StringBuilder();

        for (int index = 0; index < text.Length; ++index) {
          if (char.IsLetterOrDigit(text[index]) ||
              (text[index] == '_')) {
            buffer.Append(text[index]);
          }
          else if (text[index] != '\0') {
            int asciiValue = (int) text[index];
            char hex1 = "0123456789ABCDEF"[asciiValue / 16],
                 hex2 = "0123456789ABCDEF"[asciiValue % 16];
            buffer.Append(hex1.ToString() + hex2.ToString());
          }
        }

        return "string_" + buffer.ToString() + Symbol.NumberId;
      }
      else if (value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) value;
        return "staticaddress" + Symbol.SeparatorId + staticAddress.UniqueName
               + Symbol.SeparatorId + staticAddress.Offset + Symbol.NumberId;
      }
      else if (type.IsArray()) {
        return "Array_" + value.ToString() + Symbol.NumberId;
      }
      else if (type.IsFloating()) {
        return "float" + type.Size().ToString() + Symbol.SeparatorId +
               value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }
      else if (type.IsLogical()) {
        return "int" + type.Size().ToString() + Symbol.SeparatorId +
               value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }
      else {
        return "int" + type.Size().ToString() + Symbol.SeparatorId +
               value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }
    }

    public string Name {
      get { return m_name; }
      set { m_name = value; }
    }

    public string UniqueName {
      get { return m_uniqueName; }
      set { m_uniqueName = value; }
    }

    public bool ExternalLinkage {
      get { return m_externalLinkage; }
    }

    public bool FunctionDefinition {
      get { return m_functionDefinition; }
      set { m_functionDefinition = value; }
    }

    public Storage Storage {
      get { return m_storage; }
      set { m_storage = value; }
    }

    public Type Type {
      get { return m_type; }
      set { m_type = value; }
    }

    public int Offset {
      get { return m_offset; }
      set { m_offset = value; }
    }

    public bool IsExtern() {
      return (m_storage == Storage.Extern);
    }
  
    public bool IsStatic() {
      return (m_storage == Storage.Static);
    }
  
    public bool IsExternOrStatic() {
      return IsExtern() || IsStatic();
    }
  
    public bool IsTypedef() {
      return (m_storage == Storage.Typedef);
    }
  
    public bool IsAuto() {
      return (m_storage == Storage.Auto);
    }

    public bool IsRegister() {
      return (m_storage == Storage.Register);
    }

    public bool IsAutoOrRegister() {
      return IsAuto() || IsRegister();
    }

    public ISet<MiddleCode> TrueSet {
      get { return m_trueSet; }
    }

    public ISet<MiddleCode> FalseSet {
      get { return m_falseSet; }
    }

    public bool Parameter
    {
      get { return m_parameter; }
    }
          
    public object Value {
      get { return m_value; }
      set { m_value = value; }
    }
  
    public Symbol AddressSymbol {
      get { return m_addressSymbol; }
      set { m_addressSymbol = value; }
    }  

    public int AddressOffset {
      get { return m_addressOffset; }
      set { m_addressOffset = value; }
    }

    public bool IsValue() {
      return (m_name != null) && m_name.Contains(NumberId);
    }

    public bool IsTemporary() {
      return (m_name != null) && m_name.Contains(TemporaryId) &&
             (m_addressSymbol == null);             
    }

    public bool IsAssignable() {
      return !IsValue() && !m_type.IsConstantRecursive() &&
             !m_type.IsArrayFunctionOrString();
    }

    public override string ToString() {
      if (m_name != null) {
        if (m_addressSymbol != null) {
          return ((m_name != null) ? m_name : "") + " -> " +
                 m_addressSymbol.ToString();
        }
        else {
          return ((m_name != null) ? m_name : "");
        }
      }
      else {
        if (m_addressSymbol != null) {
          return ((m_uniqueName != null) ? m_uniqueName : "") + " -> " +
                 m_addressSymbol.ToString();
        }
        else {
          return ((m_uniqueName != null) ? m_uniqueName : "");
        }
      }
    }

    public static string SimpleName(string name) {
      int index = name.LastIndexOf(Symbol.SeparatorId);
      return (index != -1) ? name.Substring(index + 1).Replace("#", "")
                           : name;
    }
  }
}

    /* global space:
       int i;        => external linkage
       extern int i; => external linkage
       static int i; => no external linkage
  
       int f(int i);        => external linkage
       extern int f(int i); => external linkage
       static int f(int i); => no external linkage

       int f(int i) {}        => external linkage
       extern int f(int i) {} => external linkage
       static int f(int i) {} => no external linkage

       int x = 1;        => external linkage
       static int x = 1; => external linkage
       extern int x = 1; => external linkage
  
       int  f() {}
       extern int  f() {}
       static int  f() {}

       int i;
       extern int i;      
       static int i;*/