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

    private bool m_temporary, m_parameter, m_externalLinkage;
    private string m_name, m_uniqueName;
    private Storage m_storage;
    private Type m_type;
    private object m_value;
    private int m_offset;
    private Symbol m_addressSymbol;
    private int m_addressOffset;
    private bool m_assignable, m_addressable;
    private ISet<MiddleCode> m_trueSet, m_falseSet;

    private static int UniqueNameCount = 0, TemporaryNameCount = 0;

    public Symbol(string name, bool externalLinkage, Storage storage, // Regular
                  Type type, bool parameter = false, object value = null) {
      m_name = name;
      m_externalLinkage = externalLinkage;
      m_storage = storage;

      if (m_externalLinkage) {
        m_uniqueName = (m_name.Equals("abs") ? "abs_" : m_name);
      }
      else {
        m_uniqueName = Symbol.FileMarker + (UniqueNameCount++) + Symbol.SeparatorId + m_name;
      }

      m_type = type;
      m_parameter = parameter;
      m_value = value;
      m_temporary = false;
      m_assignable = GetAssignable();
      m_addressable = GetAddressable();
      CheckValue(m_type, m_value);
    }

    public Symbol(Type type, bool temporary = true) { //Maybe temporary
      m_name = Symbol.TemporaryId + "temporary" + (TemporaryNameCount++);
      m_storage = Storage.Auto;
      m_type = type;
      m_temporary = temporary;
      m_parameter = false;

      if (temporary) {
        m_assignable = false;
        m_addressable = false;
      }
      else {
        m_assignable = GetAssignable();
        m_addressable = GetAddressable();
      }
    }

    public Symbol(Type type, object value) { // Value
      m_uniqueName = StaticSymbolWindows.ValueName(type, value);
      m_storage = Storage.Static;
      m_type = type;
      m_value = value;
      m_temporary = false;
      m_parameter = false;
      m_assignable = false;
      m_addressable = false;
      CheckValue(m_type, m_value);
    }

    private static void CheckValue(Type type, object value) {
      if (value is BigInteger) {
        BigInteger bigValue = (BigInteger) value;
        Assert.Error((bigValue >= type.GetMinValue()) &&
                     (bigValue <= type.GetMaxValue()),
                     type + ": " + value, Message.Value_overflow);
      }
    }

    public Symbol(ISet<MiddleCode> trueSet, ISet<MiddleCode> falseSet) {
      m_name = Symbol.TemporaryId + "logical" + (TemporaryNameCount++);
      m_storage = Storage.Auto;
      m_type = new Type(Sort.Logical);
      m_trueSet = (trueSet != null) ? trueSet : (new HashSet<MiddleCode>());
      m_falseSet = (falseSet != null) ? falseSet : (new HashSet<MiddleCode>());
      m_temporary = false;
      m_parameter = false;
      m_assignable = false;
      m_addressable = false;
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

    public bool IsParameter() {
      return m_parameter;
    }
          
    public bool IsTemporary() {
      return m_temporary;
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

    public bool Assignable {
      get { return m_assignable; }
      set { m_assignable = value; }
    }

    public bool Addressable {
      get { return m_addressable; }
      set { m_addressable = value; }
    }

    private bool GetAssignable() {
      return !m_type.IsConstantRecursive() && m_type.IsComplete() && !m_type.IsArrayOrFunction();
    }

    private bool GetAddressable() {
      return ((m_storage != Storage.Register) && !m_type.IsBitfield());
    }

    public override string ToString() {
      if (m_name != null) {
        if (m_addressSymbol != null) {
          return ((m_name != null) ? m_name : "") + " -> " + m_addressSymbol.ToString();
        }
        else {
          return ((m_name != null) ? m_name : "");
        }
      }
      else {
        if (m_addressSymbol != null) {
          return ((m_uniqueName != null) ? m_uniqueName : "") + " -> " + m_addressSymbol.ToString();
        }
        else {
          return ((m_uniqueName != null) ? m_uniqueName : "");
        }
      }
    }

    private static string SimpleName(string name) {
      int index = name.LastIndexOf(Symbol.SeparatorId);
      return (index != -1) ? name.Substring(index + 1).Replace("#", "") : name;
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