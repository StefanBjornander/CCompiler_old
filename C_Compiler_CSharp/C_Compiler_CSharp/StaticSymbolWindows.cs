using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class StaticSymbolWindows : StaticSymbol {
    private List<byte> m_byteList;
    private IDictionary<int,string> m_accessMap, m_callMap;
    private ISet<int> m_returnSet;

    public StaticSymbolWindows() {
      // Empty.
    }

    public StaticSymbolWindows(string uniqueName, List<byte> byteList, // Windows static object
                        IDictionary<int,string> accessMap)
     :base(uniqueName) {
      m_byteList = byteList;
      m_accessMap = accessMap;
      m_callMap = new Dictionary<int,string>();
      m_returnSet = new HashSet<int>();
    }

    public StaticSymbolWindows(string uniqueName, List<byte> byteList, // Windows function definition
                        IDictionary<int,string> accessMap,
                        IDictionary<int,string> callMap, ISet<int> returnSet)
     :base(uniqueName) {
      m_byteList = byteList;
      m_accessMap = accessMap;
      m_callMap = callMap;
      m_returnSet = returnSet;
    }

    public static string ValueName(CCompiler.Type type, object value) {
      Assert.ErrorA(value != null);

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

        //string name = Enum.GetName(typeof(Sort), Sort.StringX);
        return "string_" + buffer.ToString() + Symbol.NumberId;
      }
      else if (value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) value;
        return "staticaddress" + Symbol.SeparatorId + staticAddress.UniqueName +
                Symbol.SeparatorId + staticAddress.Offset + Symbol.NumberId;
      }
      else if (type.IsArray()) {
        return "Array_" + Symbol.NumberId; // + ((value != null) ? value : "");
      }
      else if (type.IsFloating()) {
        return "float" + type.Size().ToString() + Symbol.SeparatorId + value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }
      else if (type.IsLogical()) {
        return "int" + type.Size().ToString() + Symbol.SeparatorId + value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }
      else {
        return "int" + type.Size().ToString() + Symbol.SeparatorId + value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }
    }

    public List<byte> ByteList {
      get { return m_byteList; }
    }
  
    public IDictionary<int,string> AccessMap {
      get { return m_accessMap; }
    }
  
    public IDictionary<int,string> CallMap {
      get { return m_callMap; }
    }  
  
    public ISet<int> ReturnSet {
      get { return m_returnSet; }
    }
    
    public override void Save(BinaryWriter outStream) {
      base.Save(outStream);

      if (m_byteList != null) {
        outStream.Write(m_byteList.Count);
        foreach (sbyte b in m_byteList) {
          outStream.Write(b);
        }
      }
      else {
        outStream.Write(0);
      }

      if (m_accessMap != null) {
        outStream.Write(m_accessMap.Count);
        foreach (KeyValuePair<int,string> entry in m_accessMap) {
          outStream.Write(entry.Key);
          outStream.Write(entry.Value);
        }
      }
      else {
        outStream.Write(0);
      }
    
      if (m_callMap != null) {
        outStream.Write(m_callMap.Count);
        foreach (KeyValuePair<int,string> entry in m_callMap) {
          outStream.Write(entry.Key);
          outStream.Write(entry.Value);
        }
      }
      else {
        outStream.Write(0);
      }

      if (m_returnSet != null) {
        outStream.Write(m_returnSet.Count);
        foreach (int address in m_returnSet) {
          outStream.Write(address);
        }
      }
      else {
        outStream.Write(0);
      }

    }

    public override void Load(BinaryReader inStream) {
      base.Load(inStream);

      { m_byteList = new List<byte>();
        int byteListSize = inStream.ReadInt32();

        for (int index = 0; index < byteListSize; ++index) {
          byte b = inStream.ReadByte();
          m_byteList.Add(b);
        }
      }
    
      { m_accessMap = new Dictionary<int,string>();
        int accessMapSize = inStream.ReadInt32();
        for (int index = 0; index < accessMapSize; ++index) {
          int address = inStream.ReadInt32();
          string name = inStream.ReadString();
          m_accessMap.Add(address, name);
        }
      }
    
      { m_callMap = new Dictionary<int,string>();
        int callMapSize = inStream.ReadInt32();
        for (int index = 0; index < callMapSize; ++index) {
          int address = inStream.ReadInt32();
          string name = inStream.ReadString();
          m_callMap.Add(address, name);
        }
      }
    
      { m_returnSet = new HashSet<int>();
        int returnSetSize = inStream.ReadInt32();
        for (int index = 0; index < returnSetSize; ++index) {
          int address = inStream.ReadInt32();
          m_returnSet.Add(address);
        }
      }
    }
  }
}

    // global space:
    //   int i;        => external linkage
    //   extern int i; => external linkage
    //   static int i; => no external linkage
  
    //   int f(int i);        => external linkage
    //   extern int f(int i); => external linkage
    //   static int f(int i); => no external linkage

    //   int f(int i) {}        => external linkage
    //   extern int f(int i) {} => external linkage
    //   static int f(int i) {} => no external linkage

    //   int x = 1;        => external linkage
    //   static int x = 1; => external linkage
    //   extern int x = 1; => external linkage
  
    // int  f() {}
    // extern int  f() {}
    // static int  f() {}

    // int i;
    // extern int i;      
    // static int i;