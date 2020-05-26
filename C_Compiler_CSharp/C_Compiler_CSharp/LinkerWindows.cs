using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class LinkerWindows : Linker {
    public static string StackTopName = Symbol.SeparatorId + "StackTop";
    private int m_totalSize = 256;
  
    private IDictionary<string,StaticSymbolWindows> m_globalMap = new Dictionary<string,StaticSymbolWindows>();
    private List<StaticSymbolWindows> m_globalList = new List<StaticSymbolWindows>();
    private IDictionary<string,int> m_addressMap = new Dictionary<string,int>();
    private FileInfo m_targetFile;
    private StaticSymbolWindows m_pathNameSymbol = null;
  
    public LinkerWindows(FileInfo targetFile) {
      m_targetFile = targetFile;
    }
  
    public override void Add(StaticSymbol staticSymbol) {
      StaticSymbolWindows staticSymbolWindows = (StaticSymbolWindows) staticSymbol;
      string uniqueName = staticSymbolWindows.UniqueName;

      if (!m_globalMap.ContainsKey(uniqueName)) {
        m_globalMap.Add(uniqueName, staticSymbolWindows);
      }
      else {
        Assert.Error(uniqueName.EndsWith(Symbol.NumberId),
                     SimpleName(uniqueName), Message.Duplicate_global_name);
      }
    }

    public override void Generate() {
      //m_globalMap.Add(AssemblyCodeGenerator.PathName, GeneratePathSymbol());

      { Assert.ErrorA(m_globalMap.ContainsKey("$init"));
        StaticSymbolWindows initInfo = m_globalMap["$init"];
        m_globalList.Add(initInfo);
        m_totalSize += initInfo.ByteList.Count;
        m_addressMap.Add("$init", 0);
      }

      if (m_globalMap.ContainsKey("$args")) {
        { StaticSymbolWindows argsInfo = m_globalMap["$args"];
          m_globalList.Add(argsInfo);
          m_totalSize += argsInfo.ByteList.Count;
          m_addressMap.Add("$args", 0);
        }

        { List<byte> byteList = new List<byte>();
          IDictionary<int, string> accessMap = new Dictionary<int, string>();
          m_pathNameSymbol = (StaticSymbolWindows) ConstantExpression.Value(AssemblyCodeGenerator.PathName, Type.StringType, @"C:\D\Main.com");
          m_globalMap.Add(AssemblyCodeGenerator.PathName, (StaticSymbolWindows) m_pathNameSymbol);
        }
      }

      { StaticSymbolWindows mainInfo;
        Assert.Error(m_globalMap.TryGetValue("main", out mainInfo),
                     "non-static main", Message.Function_missing);
        GenerateTrace(mainInfo);
      }

      StreamWriter streamWriter = new StreamWriter("C:\\Users\\Stefan\\Documents\\A A C_Compiler_Assembler - A 16 bits\\StdIO\\Linker1.debug");
      foreach (StaticSymbolWindows symbol in m_globalList) {
        streamWriter.WriteLine(symbol.UniqueName.Replace("\n", "\\n"));
      }
      streamWriter.Close();

      m_addressMap.Add(StackTopName, m_totalSize);
    
      foreach (StaticSymbolWindows staticSymbol in m_globalList) {
        List<byte> byteList = staticSymbol.ByteList;
        int startAddress = m_addressMap[staticSymbol.UniqueName];
        GenerateAccess(staticSymbol.AccessMap, byteList);
        GenerateCall(startAddress, staticSymbol.CallMap, byteList);
        GenerateReturn(startAddress, staticSymbol.ReturnSet, byteList);
      }

      { Console.Out.WriteLine("Generating \"" + m_targetFile.FullName + "\".");
        m_targetFile.Delete();
        BinaryWriter targetStream = new BinaryWriter(File.OpenWrite(m_targetFile.FullName));

        foreach (StaticSymbolWindows staticSymbol in m_globalList) {
          foreach (sbyte b in staticSymbol.ByteList) {
            targetStream.Write(b);
          }
        }

        targetStream.Close();
      }
    }
 
/*    private StaticSymbolWindows GeneratePathSymbolX() {
      List<byte> byteList = new List<byte>();
      IDictionary<int,string> accessMap = new Dictionary<int,string>();
      StaticSymbolWindows staticSymbol = (StaticSymbolWindows)ConstantExpression.Value(AssemblyCodeGenerator.PathName, Type.StringType, m_targetFile.FullName);
      //GenerateStaticInitializerWindows.ByteList(Type.StringType, m_comFile.FullName, byteList, accessMap);
      return staticSymbol;
    }*/
  
    private void GenerateTrace(StaticSymbolWindows staticSymbol) {
      if (!m_globalList.Contains(staticSymbol)) {
        m_globalList.Add(staticSymbol);
        m_addressMap.Add(staticSymbol.UniqueName, m_totalSize);
        m_totalSize += (int) staticSymbol.ByteList.Count;
      
        if ((m_pathNameSymbol != null) && !m_globalList.Contains(m_pathNameSymbol)) {
          m_globalList.Add(m_pathNameSymbol);
          m_addressMap.Add(m_pathNameSymbol.UniqueName, m_totalSize);
          m_totalSize += (int) m_pathNameSymbol.ByteList.Count;
        }

        ISet<string> accessNameSet = new HashSet<string>(staticSymbol.AccessMap.Values);
        foreach (string accessName in accessNameSet) {
          if (!accessName.Equals(StackTopName)) {
            StaticSymbolWindows accessSymbol;
            Assert.Error(m_globalMap.TryGetValue(accessName, out accessSymbol), accessName, Message.Object_missing_in_linking);
            Assert.Error(accessSymbol != null, SimpleName(accessName), 
                         Message.Missing_external_variable);
            GenerateTrace(accessSymbol);
          }
        }

        ISet<string> callNameSet = new HashSet<string>(staticSymbol.CallMap.Values);
        foreach (string callName in callNameSet) {
          StaticSymbolWindows funcSymbol;
          Assert.Error(m_globalMap.TryGetValue(callName, out funcSymbol), callName, Message.Function_missing_in_linking);
          Assert.Error(funcSymbol != null, SimpleName(callName), 
                         Message.Missing_external_function);
          GenerateTrace(funcSymbol);
        }      
      }
    }

    private void GenerateAccess(IDictionary<int,string> accessMap,
                                List<byte> byteList) {
      foreach (KeyValuePair<int,string> entry in accessMap) {
        int address = entry.Key;
        string name = entry.Value;

        byte oldLowByte = byteList[address],
             oldHighByte = byteList[address + 1];

        int oldTarget = ((int) oldHighByte << 8) + oldLowByte;
        int newTarget = oldTarget + m_addressMap[name];

        byte newLowByte = (byte) newTarget,
             newHighByte = (byte) (newTarget >> 8);

        byteList[address] = newLowByte;
        byteList[address + 1] = newHighByte;
      }
    }

    private void GenerateCall(int startAddress, IDictionary<int,string> callMap,
                              List<byte> byteList) {
      foreach (KeyValuePair<int,string> entry in callMap) {
        int address = entry.Key;
        int callerAddress = startAddress + address + 2;
        int calleeAddress = m_addressMap[entry.Value];
        int relativeAddress = calleeAddress - callerAddress;

        if (relativeAddress == -129) {
          byteList[address - 1] = (byte) AssemblyCode.ShortJumpOperator;
          byteList[address] = (byte) (-128 + 256); // (byte)((sbyte)-128);
          byteList[address + 1] = (byte) AssemblyCode.NopOperator;
        }
        else if ((relativeAddress >= -128) && // 12849
               ((((startAddress + address) <= 12849) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 11492) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11488) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11449) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11501) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11507) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 12525) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 12522) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 12322) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11790) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11768) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11638) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11540) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 10812) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
          byteList[address - 1] = AssemblyCode.NopOperator;
          byteList[address] = AssemblyCode.ShortJumpOperator;
          byteList[address + 1] = (byte) relativeAddress;
        }
        else {
          byteList[address] = (byte) ((sbyte) relativeAddress);
          byteList[address + 1] = (byte) ((sbyte) (relativeAddress >> 8));
        }
      }
    }
  
    private void GenerateReturn(int startAddress, ISet<int> returnSet,
                                List<byte> byteList) {
      foreach (int address in returnSet) {
        int relativeLowByte = byteList[address],
            relativeHighByte = byteList[address + 1];

        int relativeAddress = (relativeHighByte << 8) + relativeLowByte;
        int globalAddress = startAddress + address + relativeAddress;
      
        byte globalLowByte = (byte) ((sbyte) globalAddress);
        byte globaHighByte = (byte) ((sbyte) (globalAddress >> 8));

        byteList[address] = globalLowByte;
        byteList[address + 1] = globaHighByte;
      }
    }

    public static string SimpleName(string name) {
      int index = name.LastIndexOf(Symbol.SeparatorId);
      return (index != -1) ? name.Substring(0, index) : name;
    }  
  }
}