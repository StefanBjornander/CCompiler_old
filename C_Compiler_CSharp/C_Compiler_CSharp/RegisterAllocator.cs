using System.Collections.Generic;

namespace CCompiler {
  public class RegisterAllocator {
    public RegisterAllocator(ISet<Track> totalTrackSet, List<AssemblyCode> assemblyCodeList) {
      Graph<Track> totalTrackGraph = new Graph<Track>(totalTrackSet);

      foreach (Track track1 in totalTrackSet) {
        foreach (Track track2 in totalTrackSet) {
          if (!track1.Equals(track2) && Track.Overlaps(track1, track2)) {
            totalTrackGraph.AddEdge(track1, track2);
          }
        }
      }

      ISet<Graph<Track>> split = totalTrackGraph.Split();
      int index = 0;
      foreach (Graph<Track> trackGraph in split) {
        List<Track> trackList = new List<Track>(trackGraph.VertexSet);
        Assert.Error(DeepSearch(trackList, 0, trackGraph), Message.Out_of_registers);
        ++index;
      }

      foreach (Track track in totalTrackSet) {
        track.Generate(assemblyCodeList);
      }
    }
    
    private bool DeepSearch(List<Track> trackList, int listIndex, Graph<Track> trackGraph) {
      if (listIndex == trackList.Count) {
        return true;
      }

      Track track = trackList[listIndex];
      if (track.Register != null) {
        return DeepSearch(trackList, listIndex + 1, trackGraph);
      }

      ISet<Register> possibleSet = GetPossibleSet(track);
      ISet<Track> neighbourSet = trackGraph.GetNeighbourSet(track);

      foreach (Register possibleRegister in possibleSet) {
        if (!OverlapNeighbourSet(possibleRegister, neighbourSet)) {
          track.Register = possibleRegister;

          if (DeepSearch(trackList, listIndex + 1, trackGraph)) {
            return true;
          }

          track.Register = null;
        }
      }

      track.Register = null;
      return false;
    }

    private bool OverlapNeighbourSet(Register register, ISet<Track> neighbourSet) {
      foreach (Track neighbourTrack in neighbourSet) {
        if (AssemblyCode.RegisterOverlap(register, neighbourTrack.Register)) {
          return true;
        }
      }

      return false;
    }

    public static ISet<Register> PointerRegisterSetWithEllipse = new HashSet<Register>(),
                                 m_pointerRegisterSetWithEllipse = new HashSet<Register>(),
                                 m_pointerRegisterSetWithoutEllipse = new HashSet<Register>(),
                                 m_byte1RegisterSet = new HashSet<Register>(),
                                 m_byte2RegisterSet = new HashSet<Register>();
                                 /*m_byte4RegisterSet = new HashSet<Register>(),
                                 m_byte8RegisterSet = new HashSet<Register>(),
                                 m_extraRegisterSet = new HashSet<Register>();*/

    static RegisterAllocator() {
      if (Start.Windows) {
        PointerRegisterSetWithEllipse.Add(Register.si);
        PointerRegisterSetWithEllipse.Add(Register.di);
        PointerRegisterSetWithEllipse.Add(Register.bx);
        PointerRegisterSetWithEllipse.Add(Register.si);
        PointerRegisterSetWithEllipse.Add(Register.di);
        PointerRegisterSetWithEllipse.Add(Register.bx);
      }

      if (Start.Linux) {
        PointerRegisterSetWithEllipse.Add(Register.rsi);
        PointerRegisterSetWithEllipse.Add(Register.rdi);
        PointerRegisterSetWithEllipse.Add(Register.rbx);
        PointerRegisterSetWithEllipse.Add(Register.rsi);
        PointerRegisterSetWithEllipse.Add(Register.rdi);
        PointerRegisterSetWithEllipse.Add(Register.rbx);
      }

      m_pointerRegisterSetWithEllipse.Add(Register.si);
      m_pointerRegisterSetWithEllipse.Add(Register.di);
      m_pointerRegisterSetWithEllipse.Add(Register.bx);

      m_pointerRegisterSetWithoutEllipse.Add(Register.si);
      m_pointerRegisterSetWithoutEllipse.Add(Register.di);
      m_pointerRegisterSetWithoutEllipse.Add(Register.bx);
      m_pointerRegisterSetWithoutEllipse.Remove(AssemblyCode.EllipseRegister);

      m_byte1RegisterSet.Add(Register.al);
      m_byte1RegisterSet.Add(Register.ah);
      m_byte1RegisterSet.Add(Register.bl);
      m_byte1RegisterSet.Add(Register.bh);
      m_byte1RegisterSet.Add(Register.cl);
      m_byte1RegisterSet.Add(Register.ch);
      m_byte1RegisterSet.Add(Register.dl);
      m_byte1RegisterSet.Add(Register.dh);

      m_byte2RegisterSet.Add(Register.ax);
      m_byte2RegisterSet.Add(Register.bx);
      m_byte2RegisterSet.Add(Register.cx);
      m_byte2RegisterSet.Add(Register.dx);

      /*m_byte4RegisterSet.Add(Register.eax);
      m_byte4RegisterSet.Add(Register.ebx);
      m_byte4RegisterSet.Add(Register.ecx);
      m_byte4RegisterSet.Add(Register.edx);

      m_byte8RegisterSet.Add(Register.rax);
      m_byte8RegisterSet.Add(Register.rbx);
      m_byte8RegisterSet.Add(Register.rcx);
      m_byte8RegisterSet.Add(Register.rdx);

      m_extraRegisterSet.Add(Register.r0);
      m_extraRegisterSet.Add(Register.r1);
      m_extraRegisterSet.Add(Register.r2);
      m_extraRegisterSet.Add(Register.r3);
      m_extraRegisterSet.Add(Register.r4);
      m_extraRegisterSet.Add(Register.r5);
      m_extraRegisterSet.Add(Register.r6);
      m_extraRegisterSet.Add(Register.r7);
      m_extraRegisterSet.Add(Register.r8);
      m_extraRegisterSet.Add(Register.r9);
      m_extraRegisterSet.Add(Register.r10);
      m_extraRegisterSet.Add(Register.r11);
      m_extraRegisterSet.Add(Register.r12);
      m_extraRegisterSet.Add(Register.r13);
      m_extraRegisterSet.Add(Register.r14);*/
    }

    private static ISet<Register> GetPossibleSet(Track track) {
      if (track.Pointer) {
        if (SymbolTable.CurrentFunction.Type.IsEllipse()) {
          return m_pointerRegisterSetWithoutEllipse;
        }
        else {
          return m_pointerRegisterSetWithEllipse;
        }
      }
      else if (track.MaxSize == 1) {
        return m_byte1RegisterSet;
      }
      else {
        return m_byte2RegisterSet;
      }
      /*else {
        ISet<Register> registerSet = m_byte2RegisterSet;

        if (track.MinSize == 8) {
          registerSet.UnionWith(m_extraRegisterSet);
        }

        return registerSet;
      }
      else {
        switch (track.MaxSize) {
          case 1:
            return m_byte1RegisterSet;

          case 2:
            return m_byte2RegisterSet;

          case 4:
            return m_byte4RegisterSet;

          case 8:
            return m_byte8RegisterSet;
        }
      }

      return null;*/
    }
  }
}