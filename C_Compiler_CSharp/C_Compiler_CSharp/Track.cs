using System;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class Track {
    private string m_name;
    private Register? m_register = null;
    private List<TrackEntry> m_entryList = new List<TrackEntry>();
    private bool m_pointer;
    private int m_minSize, m_currSize, m_maxSize;

    private static int TrackCount = 0;

    public Track(Symbol symbol, Register? register = null) {
      m_name = "track" + (TrackCount++);
      m_register = register;

      Assert.ErrorA(symbol != null);
      Type type = symbol.Type;
      Assert.ErrorA(!type.IsFunction() && !type.IsStructOrUnion());
    
      if (type.IsArray() || type.IsString()) {
        m_minSize = m_currSize = m_maxSize = Type.PointerSize;
      }
      else {
        m_minSize = m_currSize = m_maxSize = type.Size();
      }

      Assert.ErrorA((m_currSize == 1) || (m_currSize == 2) || (m_currSize == 4) || (m_currSize == 8));
    }

    public Track(Type type) {
      m_name = "track" + (TrackCount++);
      Assert.ErrorA(type != null);
      Assert.ErrorA(!type.IsArrayFunctionStringStructOrUnion());
      m_minSize = m_currSize = m_maxSize = type.Size();
      //m_maxSize = m_currSize = type.IsArray() ? Type.PointerSize : type.Size();
      Assert.ErrorA((m_currSize == 1) || (m_currSize == 2) || (m_currSize == 4) || (m_currSize == 8));
    }

    public void Replace(List<AssemblyCode> assemblyCodeList, Track newTrack) {
      foreach (TrackEntry entry in m_entryList) {
        assemblyCodeList[entry.Line()][entry.Position()] = newTrack;
      }
    }

    public int Size {
      get {
        return m_currSize;
      }

      set {
        m_currSize = value;
        m_minSize = Math.Min(m_minSize, m_currSize);
        m_maxSize = Math.Max(m_maxSize, m_currSize);
        Assert.ErrorA((m_currSize == 1) || (m_currSize == 2) || (m_currSize == 4) || (m_currSize == 8));
      }
    }
  
    public void AddEntry(int position, int line) {
      m_entryList.Add(new TrackEntry(position, line, m_currSize));
    }

    public int MinSize {
      get {return m_minSize; }
    }

    public int MaxSize {
      get {return m_maxSize; }
    }

    public Register? Register {
      get { return m_register; }
      set {
        Assert.ErrorA((value == null) || (m_register == null) ||
                      AssemblyCode.RegisterOverlap(value, m_register));
        m_register = value;
      }
    }

    public bool Pointer {
      get { return m_pointer; }
      set { m_pointer = value; }
    }
  
    public static bool Overlaps(Track track1, Track track2) {
      if ((track1.m_entryList.Count == 0) || (track2.m_entryList.Count == 0)) {
        return false;
      }
    
      TrackEntry minEntry1 = track1.m_entryList[0],
                 minEntry2 = track2.m_entryList[0],
                 maxEntry1 = track1.m_entryList[track1.m_entryList.Count - 1],
                 maxEntry2 = track2.m_entryList[track2.m_entryList.Count - 1];

      return !(((maxEntry1.Line() < minEntry2.Line()) ||
                (maxEntry2.Line() < minEntry1.Line())));
    }

    public void Generate(List<AssemblyCode> objectCodeList) {
      foreach (TrackEntry entry in m_entryList) {
        Register sizeRegister = AssemblyCode.RegisterToSize(m_register.Value, entry.Size());
        AssemblyCode objectCode = objectCodeList[entry.Line()];
        objectCode[entry.Position()] = sizeRegister;
      }
    }

    public override string ToString() {
       return m_name;
    }
  }
}