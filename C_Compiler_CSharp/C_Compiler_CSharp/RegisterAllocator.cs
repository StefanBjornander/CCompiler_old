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

/*      foreach (Track track in trackGraph.VertexSet) {
        track.Generate(assemblyCodeList);
      }*/

      ISet<Graph<Track>> split = totalTrackGraph.Split();
      foreach (Graph<Track> trackGraph in split) {
        List<Track> trackList = new List<Track>(trackGraph.VertexSet);
        Assert.Error(DeepFirstSearch(trackList, 0, trackGraph),
                     Message.Out_of_registers);
      }
    
      SetRegistersInCodeList(assemblyCodeList);
    }

    private static void SetRegistersInCodeList(List<AssemblyCode> assemblyCodeList) {
      foreach (AssemblyCode assemblyCode in assemblyCodeList) {
        if (assemblyCode.Operator == AssemblyOperator.set_track_size) {
          Track track = (Track) assemblyCode[0];
          object operand1 = assemblyCode[1];

          if (operand1 is int) {
            track.CurrentSize = (int) operand1;
          }
          else {
            track.CurrentSize = ((Track) operand1).CurrentSize;
          }

          assemblyCode.Operator = AssemblyOperator.empty;
        }
        else {
          Check(assemblyCode, 0);
          Check(assemblyCode, 1);
          Check(assemblyCode, 2);
        }
      }
    }

    private static void Check(AssemblyCode assemblyCode, int position) {
      if (assemblyCode[position] is Track) {
        Track track = (Track) assemblyCode[position];
        Assert.ErrorXXX(track.Register != null);
        assemblyCode[position] = AssemblyCode.RegisterToSize(track.Register.Value, track.CurrentSize);
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
      PointerRegisterSetWithEllipse = new HashSet<Register>() {
        AssemblyCode.RegisterToSize(Register.bp, TypeSize.PointerSize),
        AssemblyCode.RegisterToSize(Register.si, TypeSize.PointerSize),
        AssemblyCode.RegisterToSize(Register.di, TypeSize.PointerSize),
        AssemblyCode.RegisterToSize(Register.bx, TypeSize.PointerSize)
      },
      PointerRegisterSetWithoutEllipse = new HashSet<Register>(PointerRegisterSetWithEllipse),
      Byte1RegisterSet = new HashSet<Register>() {
        Register.al,Register.ah, Register.bl, Register.bh, 
        Register.cl, Register.ch, Register.dl, Register.dh
      },
      Byte2RegisterSet = new HashSet<Register>() {
        Register.ax, Register.bx, Register.cx, Register.dx
      };

    static RegisterAllocator() {
      PointerRegisterSetWithEllipse.Remove(AssemblyCode.FrameRegister);
      PointerRegisterSetWithoutEllipse.Remove(AssemblyCode.FrameRegister);
      PointerRegisterSetWithoutEllipse.Remove(AssemblyCode.EllipseRegister);
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