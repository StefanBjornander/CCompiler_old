using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class StaticSymbolLinux : StaticSymbol {
    private List<String> m_textList;
    private ISet<string> m_externSet;

    public enum TextOrData {Text, Data, None};

    private TextOrData m_textOrData = TextOrData.None;

    public StaticSymbolLinux(TextOrData textOrData, string uniqueName, // Linux function definitializerion or static object
                             List<string> textList,ISet<string> externSet)
     :base(uniqueName) {
      m_textOrData = textOrData;
      m_textList = textList;
      m_externSet = externSet;
    }

    public List<string> TextList {
      get { return m_textList; }
    }

    public ISet<string> ExternSet {
      get { return m_externSet; }
    }

    public TextOrData TextOrDataX {
      get { return m_textOrData; }    
    }

    public override void Save(BinaryWriter outStream) {
      base.Save(outStream);
      outStream.Write(Enum.GetName(typeof(TextOrData), m_textOrData));

      if (m_textList != null) {
        outStream.Write(m_textList.Count);
        foreach (string text in m_textList) {
          outStream.Write(text);
        }
      }
      else {
        outStream.Write(0);
      }

      if (m_externSet != null) {
        outStream.Write(m_externSet.Count);
        foreach (string name in m_externSet) {
          outStream.Write(name);
        }
      }
      else {
        outStream.Write(0);
      }
    }

    public override void Load(BinaryReader inStream) {
      base.Load(inStream);
      m_textOrData = (TextOrData) Enum.Parse(typeof(TextOrData), inStream.ReadString());

      { m_textList = new List<string>();
        int textListSize = inStream.ReadInt32();

        for (int index = 0; index < textListSize; ++index) {
          string text = inStream.ReadString();
          m_textList.Add(text);
        }
      }

      { m_externSet = new HashSet<string>();
        int externSetSize = inStream.ReadInt32();
        for (int index = 0; index < externSetSize; ++index) {
          string text = inStream.ReadString();
          m_externSet.Add(text);
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