/*using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class LinkerLinux : Linker {
    public static string StackTopName = Symbol.SeparatorId + "StackTop";
    //private int m_totalSize = 256;
  
    private IDictionary<string,StaticSymbol> m_globalMap = new Dictionary<string,StaticSymbol>();
    private List<StaticSymbol> m_globalList = new List<StaticSymbol>();
    private FileInfo m_targetFile;
  
    public LinkerLinux(FileInfo targetFile) {
      m_targetFile = targetFile;
    }
  
    public override void Add(StaticSymbol staticSymbol) {
      string uniqueName = staticSymbol.UniqueName;

      if (!m_globalMap.ContainsKey(uniqueName)) {
        m_globalMap.Add(uniqueName, staticSymbol);
      }
      else {
        Assert.Error(uniqueName.EndsWith(Symbol.NumberId),
                     SimpleName(uniqueName), Message.Duplicate_global_name);
      }
    }

    public override void Generate() {
      m_globalMap.Add(AssemblyCodeGenerator.PathName, GeneratePathSymbol());

      { StaticSymbol mainInfo;
        Assert.Error(m_globalMap.TryGetValue("main", out mainInfo),
                     "non-static main", Message.Function_missing);
        GenerateTrace(mainInfo);
      }

      StreamWriter streamWriter = new StreamWriter("C:\\Users\\Stefan\\Documents\\A A C_Compiler_Assembler - A 16 bits\\StdIO\\Linker2.debug");
      foreach (StaticSymbol symbol in m_globalList) {
        streamWriter.WriteLine(symbol.UniqueName.Replace("\n", "\\n"));
      }
      streamWriter.Close();

      { Console.Out.WriteLine("Generating \"" + m_targetFile.FullName + "\".");
        StreamWriter targetStream = new StreamWriter(File.OpenWrite(m_targetFile.FullName));
        targetStream.WriteLine("section .text");
        targetStream.WriteLine("\tglobal _start");
        targetStream.WriteLine();
        targetStream.WriteLine("_start:");
        //targetStream.WriteLine("\t;org 100h");

        foreach (StaticSymbol staticSymbol in m_globalList) {
          if (staticSymbol.TextOrDataX == StaticSymbolLinux.TextOrData.Text) {
            foreach (string text in staticSymbol.TextList) {
              targetStream.WriteLine(text);
            }
          }
        }

        targetStream.WriteLine();
        targetStream.WriteLine("section .data");

        foreach (StaticSymbol staticSymbol in m_globalList) {
          if (staticSymbol.TextOrDataX == StaticSymbolLinux.TextOrData.Data) {
            foreach (string text in staticSymbol.TextList) {
              targetStream.WriteLine(text);
            }
          }
        }

        targetStream.WriteLine();
        targetStream.WriteLine(StackTopName + ":");
        targetStream.WriteLine("\ttimes 65536 db 0");
        targetStream.Close();
      }
    }
 
    private StaticSymbol GeneratePathSymbol() {
      List<byte> byteList = new List<byte>();
      IDictionary<int,string> accessMap = new Dictionary<int,string>();
      StaticSymbol staticSymbol = ConstantExpression.Value(AssemblyCodeGenerator.PathName, Type.StringType, m_targetFile.FullName);
      //GenerateStaticInitializerWindows.ByteList(Type.StringType, m_comFile.FullName, byteList, accessMap);
      return staticSymbol;
    }
  
    private void GenerateTrace(StaticSymbol staticSymbol) {
      if (!m_globalList.Contains(staticSymbol)) {
        m_globalList.Add(staticSymbol);
      
        foreach (string accessName in staticSymbol.ExternSet) {
          if (!accessName.Equals(StackTopName)) {
            StaticSymbol accessSymbol;
            Assert.Error(m_globalMap.TryGetValue(accessName, out accessSymbol), accessName, Message.Object_missing_in_linking);
            Assert.Error(accessSymbol != null, SimpleName(accessName), 
                         Message.Missing_external_variable);
            GenerateTrace(accessSymbol);
          }
        }
      }
    }


    public static string SimpleName(string name) {
      int index = name.LastIndexOf(Symbol.SeparatorId);
      return (index != -1) ? name.Substring(0, index) : name;
    }  
  }
}*/