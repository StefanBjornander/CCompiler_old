using System.Collections.Generic;

namespace CCompiler {
  public class RegisterAllocator {
    public RegisterAllocator(ISet<Track> totalTrackSet,
                             List<AssemblyCode> assemblyCodeList) {
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
        Assert.Error(DeepFirstSearch(trackList, 0, trackGraph),
                     Message.Out_of_registers);
        ++index;
      }

      foreach (Track track in totalTrackSet) {
        track.Generate(assemblyCodeList);
      }
    }
//The DeepFirstSearch method searches the graph in a deep-first manner. It takes the track list and the current index in that list as well as the track graph.
    private bool DeepFirstSearch(List<Track> trackList, int listIndex,
                                 Graph<Track> trackGraph) {
//If the index equals the size of the track list, we return true because we have iterated through the list and found a match; that is, each track has been assigned a register and no overlapping tracks have the same register.
      if (listIndex == trackList.Count) {
        return true;
      }
//If the current track has already been assigned a register, we just call DeepFirstSearch with the next index.
      Track track = trackList[listIndex];
      if (track.Register != null) {
        return DeepFirstSearch(trackList, listIndex + 1, trackGraph);
      }
//If the current track has not been assigned a register, we look up the set of possible register and the set of neighbor vertices; that is, the set of overlapping tracks.
      ISet<Register> possibleSet = GetPossibleSet(track);
      ISet<Track> neighbourSet = trackGraph.GetNeighbourSet(track);
//We iterate throught the set of possible register and, for each register that does not cause an overlapping, we assign the track the register and call DeepFirstSearch recursively with the next index. If the call returns true, we have found a total mapping of registers to the track, and we just return true. In this way, every call to DeepFirstSearch, including the first call in RegisterAllocator. However, if the call does not return false, we just try with another of the possible register. If the none of the registers causes a match, we clear the register of the track and return false.
      foreach (Register possibleRegister in possibleSet) {
        if (!OverlapNeighbourSet(possibleRegister, neighbourSet)) {
          track.Register = possibleRegister;

          if (DeepFirstSearch(trackList, listIndex + 1, trackGraph)) {
            return true;
          }

          track.Register = null;
        }
      }

      track.Register = null;
      return false;
    }
//The OverlapNeighbourSet method return true if the register overlaps any of its neighbors. The RegisterOverlap method in the AssemblyCode class test whether two register overlaps.
    private bool OverlapNeighbourSet(Register register,
                                     ISet<Track> neighbourSet) {
      foreach (Track neighbourTrack in neighbourSet) {
        if (AssemblyCode.RegisterOverlap(register, neighbourTrack.Register)) {
          return true;
        }
      }

      return false;
    }
//The PointerRegisterSetWithEllipse set holds the possible pointer registers of an elliptic function while PointerRegisterSetWithoutEllipse holds the possible pointer registers of an non-elliptic function. The Byte1RegisterSet ste holds all registers of one byte while Byte2RegisterSet holds all registers of two bytes.
    public static ISet<Register>
      PointerRegisterSetWithEllipse = new HashSet<Register>(),
      PointerRegisterSetWithoutEllipse = new HashSet<Register>(),
      Byte1RegisterSet = new HashSet<Register>(),
      Byte2RegisterSet = new HashSet<Register>();

    static RegisterAllocator() {
      PointerRegisterSetWithEllipse.
        Add(AssemblyCode.RegisterToSize(Register.si, TypeSize.PointerSize));
      PointerRegisterSetWithEllipse.
        Add(AssemblyCode.RegisterToSize(Register.di, TypeSize.PointerSize));
      PointerRegisterSetWithEllipse.
        Add(AssemblyCode.RegisterToSize(Register.bx, TypeSize.PointerSize));
//The PointerRegisterSetWithoutEllipse holds the registers of PointerRegisterSetWithEllipse minus the EllipseRegister register since we need it to keep track of the ellipse frame pointer in elliptic functions.
      PointerRegisterSetWithoutEllipse.
        UnionWith(PointerRegisterSetWithEllipse);
      PointerRegisterSetWithoutEllipse.Remove(AssemblyCode.EllipseRegister);

      Byte1RegisterSet.Add(Register.al);
      Byte1RegisterSet.Add(Register.ah);
      Byte1RegisterSet.Add(Register.bl);
      Byte1RegisterSet.Add(Register.bh);
      Byte1RegisterSet.Add(Register.cl);
      Byte1RegisterSet.Add(Register.ch);
      Byte1RegisterSet.Add(Register.dl);
      Byte1RegisterSet.Add(Register.dh);

      Byte2RegisterSet.Add(Register.ax);
      Byte2RegisterSet.Add(Register.bx);
      Byte2RegisterSet.Add(Register.cx);
      Byte2RegisterSet.Add(Register.dx);
    }
//The GetPossibleSet method returns the possible set a track, depending on whether the track holds a pointer, or the size of the track.
    private static ISet<Register> GetPossibleSet(Track track) {
      if (track.Pointer) {
        if (SymbolTable.CurrentFunction.Type.IsEllipse()) {
          return PointerRegisterSetWithoutEllipse;
        }
        else {
          return PointerRegisterSetWithEllipse;
        }
      }
//If the track does not hold a pointer wee look into its size. If the size is one, we have a larger set to choose from. There are eight non-pointer registers of size one while there is four registers of the other sizes. 
      else if (track.MaxSize == 1) {
        return Byte1RegisterSet;
      }
//We return the set of registers of size two, even if the size is actually larger than two. In that case, the RegisterToSize method in the AssemblyCode class will find the register of correct size.
      else {
        return Byte2RegisterSet;
      }
    }
  }
}