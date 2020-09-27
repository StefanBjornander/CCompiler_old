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

    private bool /*m_temporary, */ m_parameter, m_externalLinkage;
    private string m_name, m_uniqueName;
    private Storage m_storage;
    private Type m_type;
    private object m_value;
    private int m_offset;
    private Symbol m_addressSymbol;
    private int m_addressOffset;
    //private bool m_assignable;//, m_addressable;
    private ISet<MiddleCode> m_trueSet, m_falseSet;

    private static int UniqueNameCount = 0, TemporaryNameCount = 0;

    public Symbol(string name, bool externalLinkage, Storage storage,
                  Type type, bool parameter = false, object value = null) {
      m_name = name;
      m_externalLinkage = externalLinkage;
      m_storage = storage;

      if ((m_name != null) && m_name.Contains("$")) {
        int i = 1;
      }

      if (m_externalLinkage) {
        m_uniqueName = m_name.Equals("abs") ? "_abs" : m_name;
      }
      else {
        m_uniqueName = Symbol.FileMarker + (UniqueNameCount++) +
                       Symbol.SeparatorId + m_name;
      }

      m_type = type;
      m_parameter = parameter;
      //m_temporary = false;
      /*m_assignable = m_type.IsComplete() &&
                     !m_type.IsConstantRecursive() &&
                     !m_type.IsArrayOrFunction();*/
      //m_addressable = !IsRegister() && !m_type.IsBitfield();

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

    public Symbol(Type type, bool assignable, bool addressable = false) {
      m_name = Symbol.TemporaryId + "field" + (TemporaryNameCount++);
      m_externalLinkage = false;
      m_storage = Storage.Auto;
      m_type = type;
      //m_temporary = false;
      m_parameter = false;
      //m_assignable = assignable;
      //m_addressable = addressable;
    }

    public Symbol(Type type) {
      m_name = Symbol.TemporaryId + "temporary" + (TemporaryNameCount++);
      m_externalLinkage = false;
      m_storage = Storage.Auto;
      m_type = type;
      //m_temporary = true;
      m_parameter = false;
      //m_assignable = false;
      //m_addressable = false;
    }

    public Symbol(ISet<MiddleCode> trueSet, ISet<MiddleCode> falseSet) {
      m_name = Symbol.TemporaryId + "logical" + (TemporaryNameCount++);
      m_storage = Storage.Auto;
      m_type = new Type(Sort.Logical);
      m_trueSet = (trueSet != null) ? trueSet : (new HashSet<MiddleCode>());
      m_falseSet = (falseSet != null) ? falseSet : (new HashSet<MiddleCode>());
      //m_temporary = false;
      m_parameter = false;
      //m_assignable = false;
      //m_addressable = false;
    }

    public Symbol(Type type, object value) {
      Assert.ErrorXXX(!(value is bool));
      m_uniqueName = ValueName(type, value);
      m_storage = Storage.Static;
      m_type = type;
      m_value = value;
      //m_temporary = false;
      m_parameter = false;
      //m_assignable = false;
      //m_addressable = false;
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
        return "Array_" + value.ToString() + Symbol.NumberId; // + ((value != null) ? value : "");
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
      set { m_name = value; 
            if (m_name.Contains("$")) {
              int i = 1;
            }
          }
    }

    public string UniqueName {
      get { 
        if (m_value is StaticAddress) {
          return ((StaticAddress) m_value).UniqueName;
        }
        else {
          return m_uniqueName; 
        }
      }
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
      get { 
        if (m_value is StaticAddress) {
          return ((StaticAddress) m_value).Offset;
        }
        else {
          return m_offset;
        }
      }
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

    public bool Parameter {
      get { return m_parameter; }
    }
          
    public bool Temporary {
      get { return (m_name != null) && m_name.Contains(TemporaryId) &&
                   (m_addressSymbol == null); }
      /*get { return (m_storage == Storage.Auto) &&
                   (m_addressSymbol == null) && (m_offset == 0); }*/
      //get { return m_temporary; }
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
      get { return (!m_type.IsArrayFunctionOrString() && (m_value == null) &&
                   // !Temporary &&
                    !m_type.IsConstantRecursive()); }
//      get { return m_assignable; }
//      set { m_assignable = value; }
    }

    /*public bool AddressableX {
      get { return (!IsRegister() && !m_type.IsBitfield()); }
//      get { return m_addressable; }
      //set { m_addressable = value; }
    }*/

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