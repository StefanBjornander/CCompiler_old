using System;
using System.Collections.Generic;

namespace CCompiler {
  public class Track {
    private static int m_count = 0;
    private int m_id;

    public Track(Symbol symbol, Register? register = null) {
      m_id = m_count++;
      Register = register;
      Assert.ErrorXXX(symbol != null);
      //Assert.ErrorXXX(!symbol.Type.IsStructOrUnion());
      CurrentSize = m_maxSize = symbol.Type.ReturnSize();
    }

    public Track(Type type) {
      m_id = m_count++;
      Assert.ErrorXXX(type != null);
      //Assert.ErrorXXX(!type.IsStructOrUnion());
      Assert.ErrorXXX(!type.IsArrayFunctionOrString());
      CurrentSize = m_maxSize = type.ReturnSize();
    }

    public int CurrentSize { get; set; }

    private int m_maxSize;
    public int MaxSize {
      get { return m_maxSize; }
      set { m_maxSize = Math.Max(m_maxSize, value); }
    }

    private int m_minIndex = -1, m_maxIndex = -1;

    public int Index {
      set {
        m_minIndex = (m_minIndex != -1) ? Math.Min(m_minIndex, value) : value;
        m_maxIndex = Math.Max(m_maxIndex, value);
      }
    }

    public Register? Register {get; set;}
    public bool Pointer {get; set;}

    public static bool Overlaps(Track track1, Track track2) {
      Assert.ErrorXXX((track1.m_minIndex != -1) && (track1.m_maxIndex != -1));
      Assert.ErrorXXX((track2.m_minIndex != -1) && (track2.m_maxIndex != -1));
      return !(((track1.m_maxIndex < track2.m_minIndex) ||
                (track2.m_maxIndex < track1.m_minIndex)));
    }

    public override string ToString() {
      return m_id.ToString();
    }
  }
}