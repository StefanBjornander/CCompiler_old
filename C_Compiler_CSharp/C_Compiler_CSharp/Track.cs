using System;
using System.Collections.Generic;

namespace CCompiler {
  public class Track {
    private Register? m_register = null;
    private List<TrackEntry> m_entryList = new List<TrackEntry>();
    private bool m_pointer;
    private int m_currentSize, m_maxSize;

    public Track(Symbol symbol, Register? register = null) {
      m_register = register;
      Assert.ErrorXXX(symbol != null);
      Assert.ErrorXXX(!symbol.Type.IsStructOrUnion());
      m_currentSize = m_maxSize = symbol.Type.SizeArray();
    }

    public Track(Type type) {
      Assert.ErrorXXX(type != null);
      Assert.ErrorXXX(!type.IsStructOrUnion());
      Assert.ErrorXXX(!type.IsArrayFunctionOrString());
      m_currentSize = m_maxSize = type.Size();
    }

    public void Replace(List<AssemblyCode> assemblyCodeList, Track newTrack) {
      foreach (TrackEntry entry in m_entryList) {
        assemblyCodeList[entry.Line][entry.Position] = newTrack;
      }
    }

    public int CurrentSize {
      get { return m_currentSize; }

      set {
        m_currentSize = value;
        m_maxSize = Math.Max(m_maxSize, m_currentSize);
      }
    }
  
    public void AddEntry(int position, int line) {
      m_entryList.Add(new TrackEntry(position, line, m_currentSize));
    }

    public int MaxSize {
      get {return m_maxSize; }
    }

    public Register? Register {
      get { return m_register; }
      set {
        Assert.ErrorXXX((value == null) || (m_register == null) ||
                      AssemblyCode.RegisterOverlap(value, m_register));
        m_register = value;
      }
    }

    public bool Pointer {
      get { return m_pointer; }
      set { m_pointer = value; }
    }
  
    public static bool Overlaps(Track track1, Track track2) {
      if ((track1.m_entryList.Count == 0) || (track2.m_entryList.Count == 0)){
        return false;
      }

      TrackEntry minEntry1 = track1.m_entryList[0],
                 minEntry2 = track2.m_entryList[0],
                 maxEntry1 = track1.m_entryList[track1.m_entryList.Count - 1],
                 maxEntry2 = track2.m_entryList[track2.m_entryList.Count - 1];

      return !(((maxEntry1.Line < minEntry2.Line) ||
                (maxEntry2.Line < minEntry1.Line)));
    }

    public void Generate(List<AssemblyCode> objectCodeList) {
      foreach (TrackEntry entry in m_entryList) {
        Register sizeRegister =
          AssemblyCode.RegisterToSize(m_register.Value, entry.Size);
        AssemblyCode objectCode = objectCodeList[entry.Line];
        objectCode[entry.Position] = sizeRegister;
      }
    }
  }
}
