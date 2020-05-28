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

    public Symbol() {
      // Empty.
    }

    public Symbol(string name, Storage? storage, Type type) // Variable or function
     :this(name, storage, type, false) {
      // Empty.
    }

    public Symbol(string name, Storage? storage, Type type, bool parameter) { // Maybe parameter
      m_name = name;

      m_type = type;
      m_externalLinkage = HasExternalLinkage(storage, name);
      m_uniqueName = GetUniqueName(storage, name);
      m_storage = SetStorage(storage);
      m_temporary = false;
      m_parameter = parameter;
      m_assignable = GetAssignable();
      m_addressable = GetAddressable();

      if (m_parameter) {
        if (type.IsArray()) {
          m_type = new Type(type.ArrayType);
          m_type.IsConstant = true;
        }
        else if (type.IsFunction()) {
          m_type = new Type(type);
          m_type.IsConstant = true;
        }
      }
    }

    public Symbol(string name, Storage? storage, // Variable with value
                  Type type, object value) {
      m_name = name;
      m_type = type;
      m_externalLinkage = HasExternalLinkage(storage, name);
      m_uniqueName = GetUniqueName(storage, name);
      m_storage = SetStorage(storage);
      m_value = value;
      m_temporary = false;
      m_parameter = false;
      m_assignable = GetAssignable();
      m_addressable = GetAddressable();
    }

    public Symbol(Type type) // Temporary
     :this(type, true) {
      // Empty.
    }

    //private static Symbol m_lastFunction = null;

    public Symbol(Type type, bool temporary) { //Maybe temporary
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

      if (m_type.IsIntegralOrPointer() && (value is BigInteger)) {
        BigInteger bigValue = (BigInteger) value;
        Assert.Error((bigValue >= m_type.GetMinValue()) && (bigValue <= m_type.GetMaxValue()),
                     m_type + ": " + value, Message.Value_overflow);
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

    private static bool HasExternalLinkage(Storage? storage, string name) {
      return ((name != null) && (name.EndsWith(Symbol.NumberId) || (storage == Storage.Extern) ||
                                 ((SymbolTable.CurrentTable.Scope == Scope.Global) && (storage == null))));
    }

    private static string GetUniqueName(Storage? storage, string name) {
      if (HasExternalLinkage(storage, name)) {
        return (name.Equals("abs") ? "abs_" : name);
      }
      else {
        return Symbol.FileMarker + (UniqueNameCount++) + Symbol.SeparatorId + name;
      }
    }

    public bool ExternalLinkage {
      get { return m_externalLinkage; }
    }

    private static Storage SetStorage(Storage? storage) {
      if (storage != null) {
        return storage.Value;
      }
      else {
        if (CCompiler_Main.Parser.CallDepth > 0) {
          return Storage.Auto;
        }
        else if (SymbolTable.CurrentTable.Scope == Scope.Global) {
          return Storage.Static;
        }
        else {
          return Storage.Auto;
        }
      }
    }

    public ISet<MiddleCode> TrueSet {
      get { return m_trueSet; }
    }

    public ISet<MiddleCode> FalseSet {
      get { return m_falseSet; }
    }

    public string Name {
      get { return m_name; }
      set { m_name = value; }
    }

    public string UniqueName {
      get { return m_uniqueName; }
      set { m_uniqueName = value; }
    }

    public CCompiler.Storage Storage {
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

    public bool IsRegister() {
      return (m_storage == Storage.Register);
    }

    public bool IsAutoOrRegister() {
      return (m_storage == Storage.Auto) || (m_storage == Storage.Register);
    }

    public bool IsStatic() {
      return (m_storage == Storage.Static);
    }
  
    public bool IsExtern() {
      return (m_storage == Storage.Extern);
    }
  
    public bool IsStaticOrExtern() {
      return IsStatic() || IsExtern();
    }
  
    public bool IsTypedef() {
      return (m_storage == Storage.Typedef);
    }
  
    public bool IsParameter() {
      return m_parameter;
    }
          
    public bool IsValue() {
      return (m_uniqueName != null) && m_uniqueName.EndsWith(Symbol.NumberId);
    }

    public bool Temporary {
      get { return m_temporary; }
      set { m_temporary = value; }
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