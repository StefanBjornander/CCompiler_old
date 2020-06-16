using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCodeGenerator {
    public IDictionary<Symbol,Track> m_trackMap =
      new Dictionary<Symbol,Track>();
    public List<AssemblyCode> m_assemblyCodeList;
    public const int FloatingStackMaxSize = 7;

    private int m_floatStackSize = 0, m_totalRecordSize = 0;
    private bool m_returnFloating = false;
    private Stack<int> m_recordSizeStack = new Stack<int>();
    private Stack<IDictionary<Symbol,Track>> m_trackMapStack =
      new Stack<IDictionary<Symbol,Track>>();
    private Stack<IDictionary<Track, int>> m_postMapStack =
      new Stack<IDictionary<Track, int>>();
    private ISet<Track> m_syscallSet = new HashSet<Track>();

    public static string IntegralStorageName =
      Symbol.SeparatorId + "IntegralStorage" + Symbol.NumberId;
    public static string MainName = "main";
    public static string InitializerName = Symbol.SeparatorId + "initializer";
    public static string ArgsName = Symbol.SeparatorId + "args";
    public static string PathName = Symbol.SeparatorId + "PathName";
    public static string PathText = "";
  
    public AssemblyCodeGenerator(List<AssemblyCode> assemblyCodeList) {
      m_assemblyCodeList = assemblyCodeList;
    }

    private void RegisterAllocation(ISet<Track> trackSet) {
     new RegisterAllocator(trackSet, m_assemblyCodeList);
    }

    public static void GenerateAssembly(List<MiddleCode> middleCodeList,
                                   List<AssemblyCode> assemblyCodeList) {
      AssemblyCodeGenerator objectCodeGenerator =
        new AssemblyCodeGenerator(assemblyCodeList);
      objectCodeGenerator.AssemblyCodeList(middleCodeList);
      ISet<Track> trackSet = objectCodeGenerator.TrackSet();
      objectCodeGenerator.RegisterAllocation(trackSet);
    }
  
    public static void GenerateTargetWindows
      (List<AssemblyCode> assemblyCodeList, List<byte> byteList,
       IDictionary<int,string> accessMap, IDictionary<int,string> callMap,
       ISet<int> returnSet) {
      AssemblyCodeGenerator objectCodeGenerator =
        new AssemblyCodeGenerator(assemblyCodeList);
      objectCodeGenerator.JumpInfo();
      objectCodeGenerator.TargetByteList
                          (byteList, accessMap, callMap, returnSet);
    }

    public AssemblyCode AddAssemblyCode(AssemblyOperator objectOp,
                          object operand0 = null, object operand1 = null,
                          object operand2 = null) {
      AssemblyCode assemblyCode =
        new AssemblyCode(objectOp, operand0, operand1, operand2);
      m_assemblyCodeList.Add(assemblyCode);
      return assemblyCode;
    }

    public static AssemblyCode AddAssemblyCode(List<AssemblyCode> list,
                    AssemblyOperator objectOp, object operand0 = null,
                    object operand1 = null, object operand2 = null) {
      AssemblyCode assemblyCode =
        new AssemblyCode(objectOp, operand0, operand1, operand2);
      list.Add(assemblyCode);
      return assemblyCode;
    }

    public void AssemblyCodeList(List<MiddleCode> middleCodeList){
      for (int middleIndex = 0; middleIndex < middleCodeList.Count;
           ++middleIndex) {
        MiddleCode middleCode = middleCodeList[middleIndex];
        AddAssemblyCode(AssemblyOperator.new_middle_code, middleIndex);

        if (SymbolTable.CurrentFunction != null) {
          if (middleCode.Operator == MiddleOperator.Initializer) {
            AddAssemblyCode(AssemblyOperator.label, null,
                            middleCode.ToString());
          }
          else {
            string label = SymbolTable.CurrentFunction.UniqueName;
            
            if (middleIndex > 0) {
              label += Symbol.SeparatorId + middleIndex;
            }
            
            AddAssemblyCode(AssemblyOperator.label, label,
                            middleCode.ToString());
          }
        }

        switch (middleCode.Operator) {
          case MiddleOperator.CallHeader:
            CallHeader(middleCode);
            break;

          case MiddleOperator.Call:
            FunctionCall(middleCode, middleIndex);
            break;

          case MiddleOperator.PostCall:
            FunctionPostCall(middleCode);
            break;

          case MiddleOperator.Return:
            Return(middleCode);
            break;

          case MiddleOperator.Exit:
            Exit(middleCode);
            break;

          case MiddleOperator.Goto:
            Goto(middleCode);
            break;

          case MiddleOperator.AssignRegister:
            LoadToRegister(middleCode);
            break;

          case MiddleOperator.InspectRegister:
            InspectRegister(middleCode);
            break;

          case MiddleOperator.JumpRegister:
            JumpRegister(middleCode);
            break;

          case MiddleOperator.Interrupt:
            Interrupt(middleCode);
            break;

          case MiddleOperator.SysCall:
            SystemCall(middleCode);
            break;

          case MiddleOperator.Initializer:
            Initializer(middleCode);
            break;

          case MiddleOperator.InitializerZero:
            InitializerZero(middleCode);
            break;
          
          case MiddleOperator.Assign: {
              Symbol symbol = (Symbol)middleCode[0];

              if (symbol.Type.IsStructOrUnion()) {
                StructUnionAssign(middleCode, middleIndex);
              }
              else {
                IntegralAssign(middleCode);
              }
            }
            break;
        
          case MiddleOperator.BitwiseAnd:
          case MiddleOperator.BitwiseOr:
          case MiddleOperator.BitwiseXOr:
          case MiddleOperator.ShiftLeft:
          case MiddleOperator.ShiftRight:
            IntegralAdditionBitwiseShift(middleCode);
            break;
          
          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract: {
              Symbol resultSymbol = (Symbol) middleCode[1];

              if (resultSymbol.Type.IsFloating()) {
                FloatingBinary(middleCode);
              }
              else {
                IntegralAdditionBitwiseShift(middleCode);
              }
            }
            break;

          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide:
          case MiddleOperator.SignedModulo:
          case MiddleOperator.UnsignedMultiply:
          case MiddleOperator.UnsignedDivide:
          case MiddleOperator.UnsignedModulo: {
              Symbol symbol = (Symbol) middleCode[1];

              if (symbol.Type.IsFloating()) {
                FloatingBinary(middleCode);
              }
              else {
                IntegralMultiply(middleCode);
              }
            }
            break;

          case MiddleOperator.Carry:
          case MiddleOperator.NotCarry:
            CarryExpression(middleCode);
            break;

          case MiddleOperator.Equal:
          case MiddleOperator.NotEqual:
          case MiddleOperator.SignedLessThan:
          case MiddleOperator.SignedLessThanEqual:
          case MiddleOperator.SignedGreaterThan:
          case MiddleOperator.SignedGreaterThanEqual:
          case MiddleOperator.UnsignedLessThan:
          case MiddleOperator.UnsignedLessThanEqual:
          case MiddleOperator.UnsignedGreaterThan:
          case MiddleOperator.UnsignedGreaterThanEqual: {
              Symbol symbol = (Symbol) middleCode[1];

              if (symbol.Type.IsFloating()) {
                FloatingRelation(middleCode, middleIndex);
              }
              else {
                IntegralRelation(middleCode, middleIndex);
              }
            }
            break;
        
          case MiddleOperator.Case:
            Case(middleCode);
            break;

          case MiddleOperator.CaseEnd:
            CaseEnd(middleCode);
            break;

          case MiddleOperator.Increment:
          case MiddleOperator.Decrement:
            IntegralIncrementDecrement(middleCode);
            break;

          case MiddleOperator.UnaryAdd:
          case MiddleOperator.UnarySubtract:
          case MiddleOperator.BitwiseNot: {
              Symbol symbol = (Symbol) middleCode[1];

              if (symbol.Type.IsFloating()) {
                FloatingUnary(middleCode);
              }
              else {
                IntegralUnary(middleCode);
              }
            }
            break;
          
          case MiddleOperator.Address:
            Address(middleCode);
            break;

          case MiddleOperator.Dereference: {
              Symbol symbol = (Symbol) middleCode[1];

              if (symbol.Type.IsFloating()) {
                FloatingDereference(middleCode);
              }
              else {
                IntegralDereference(middleCode);
              }
            }
            break;

          case MiddleOperator.DecreaseStack:
            Assert.ErrorXXX((--m_floatStackSize) >= 0);
            break;

          case MiddleOperator.CheckTrackMapFloatStack:
            Assert.ErrorXXX((m_trackMap.Count == 0) &&
                          (m_floatStackSize == 0));
            break;

          case MiddleOperator.PushZero:
            PushSymbol(new Symbol(Type.DoubleType, (decimal) 0));
            break;

          case MiddleOperator.PushOne:
            PushSymbol(new Symbol(Type.DoubleType, (decimal) 1));
            break;

          case MiddleOperator.PushFloat:
            PushSymbol((Symbol) middleCode[0]);
            break;

          case MiddleOperator.TopFloat:
            TopPopSymbol((Symbol) middleCode[0], TopOrPop.Top);
            break;
          
          case MiddleOperator.PopFloat:
            TopPopSymbol((Symbol) middleCode[0], TopOrPop.Pop);
            break;

          case MiddleOperator.PopEmpty:
            PopEmpty();
            break;

          case MiddleOperator.IntegralToIntegral:
            IntegralToIntegral(middleCode, middleIndex);
            break;

          case MiddleOperator.IntegralToFloating:
            IntegralToFloating(middleCode);
            break;

          case MiddleOperator.FloatingToIntegral:
            FloatingToIntegral(middleCode);
            break;

          case MiddleOperator.Parameter: {
              Symbol symbol = (Symbol) middleCode[1];

              if (symbol.Type.IsFloating()) {
                FloatingParameter(middleCode);
              }
              else if (symbol.Type.IsStructOrUnion()) {
                StructUnionParameter(middleCode, middleIndex);
              }
              else {
                IntegralParameter(middleCode);
              }
            }
            break;
          
          case MiddleOperator.GetReturnValue: {
              Symbol symbol = (Symbol) middleCode[0];

              if (symbol.Type.IsStructOrUnion()) {
                StructUnionGetReturnValue(middleCode);
              }
              else if (symbol.Type.IsFloating()) {
                Assert.Error((++m_floatStackSize) <= FloatingStackMaxSize,
                             null, Message.Floating_stack_overflow);
              }
              else {
                IntegralGetReturnValue(middleCode);
              }
            }
            break;
          
          case MiddleOperator.SetReturnValue: {
              Symbol symbol = (Symbol) middleCode[1];

              if (symbol.Type.IsStructOrUnion()) {
                StructUnionSetReturnValue(middleCode);
              }
              else if (symbol.Type.IsFloating()) {
                Assert.ErrorXXX((--m_floatStackSize) == 0);
              }
              else {
                IntegralSetReturnValue(middleCode);
              }
            }
            break;

          case MiddleOperator.Dot:
          case MiddleOperator.FunctionEnd:
          case MiddleOperator.Empty:
            break;

          default:
            Assert.ErrorXXX(false);
            break;
        }
      }
    }

    private ISet<Track> TrackSet() {
      ISet<Track> trackSet = new HashSet<Track>();

      for (int index = 0; index < m_assemblyCodeList.Count; ++index) {
        AssemblyCode assamblyCode = m_assemblyCodeList[index];
     
        object operand0 = assamblyCode[0],
               operand1 = assamblyCode[1],
               operand2 = assamblyCode[2];

        if (assamblyCode.Operator == AssemblyOperator.set_track_size) {
          Track track = (Track) operand0;

          if (operand1 is int) {
            track.Size = (int) operand1;
          }
          else {
            track.Size = ((Track) operand1).Size;
          }

          assamblyCode.Operator = AssemblyOperator.empty;
        }
        else {
          CheckTrack(trackSet, operand0, 0, index);
          CheckTrack(trackSet, operand1, 1, index);
          CheckTrack(trackSet, operand2, 2, index);
        }
      }

      return trackSet;
    }

    private void CheckTrack(ISet<Track> trackSet, object operand,
                            int position, int index) {
      if (operand is Track) {
        Track track = (Track) operand;
        trackSet.Add(track);
        track.AddEntry(position, index);
      }
    }

    // -----------------------------------------------------------------------

    public void CallHeader(MiddleCode middleCode) {
      ISet<Symbol> integralSet = (ISet<Symbol>) middleCode[1];
      Assert.ErrorXXX(integralSet.SequenceEqual(m_trackMap.Keys));
      int stackSize = (int) middleCode[2];
      Assert.ErrorXXX(stackSize == m_floatStackSize);
      Register baseRegister = BaseRegister(null);
      int recordOffset = (int) middleCode[0], recordSize = 0;

      IDictionary<Track,int> postMap = new Dictionary<Track,int>();
      foreach (KeyValuePair<Symbol, Track> pair in m_trackMap) {
        Track track = pair.Value;
        AddAssemblyCode(AssemblyOperator.mov, baseRegister,
                        recordOffset + recordSize, track);
        postMap.Add(track, recordOffset + recordSize);
        Symbol symbol = pair.Key;
        recordSize += symbol.Type.Size();
      }

      for (int count = 0; count < m_floatStackSize; ++count) {
        AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister,
                        recordOffset + recordSize);
        recordSize += 8;
      }

      m_recordSizeStack.Push(recordSize);
      m_totalRecordSize += recordSize;
      m_trackMapStack.Push(m_trackMap);
      m_postMapStack.Push(postMap);
      m_trackMap = new Dictionary<Symbol, Track>();
    }

    public void FunctionCall(MiddleCode middleCode, int index) {
      int recordSize = ((int) middleCode[1]) +
                       m_totalRecordSize;
      Symbol calleeSymbol = (Symbol) middleCode[0];
      int extraSize = (int) middleCode[2];

      Type calleeType = calleeSymbol.Type.IsFunction()
                      ? calleeSymbol.Type : calleeSymbol.Type.PointerType;

      bool callerEllipse = SymbolTable.CurrentFunction.Type.IsEllipse(),
           calleeEllipse = calleeType.IsEllipse();
      Track jumpTrack = null;

      Register frameRegister = callerEllipse ? AssemblyCode.EllipseRegister : AssemblyCode.FrameRegister;               

      AddAssemblyCode(AssemblyOperator.address_return, frameRegister,
                      recordSize + SymbolTable.ReturnAddressOffset,
                      (BigInteger) (index + 1));

      AddAssemblyCode(AssemblyOperator.mov, frameRegister,
                      recordSize + SymbolTable.RegularFrameOffset,
                      AssemblyCode.FrameRegister);

      if (callerEllipse) {
        AddAssemblyCode(AssemblyOperator.mov, frameRegister,
                        recordSize + SymbolTable.EllipseFrameOffset,
                        AssemblyCode.EllipseRegister);
      }

      AddAssemblyCode(AssemblyOperator.add, frameRegister, // add di, 10
                      (BigInteger) recordSize);

      if (callerEllipse) {
        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister, // mov bp, di
                        AssemblyCode.EllipseRegister);
      }
      else {
        if (calleeEllipse) {
          AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                          AssemblyCode.FrameRegister);
        }
      }

      if (!calleeSymbol.Type.IsFunction()) {
        jumpTrack = LoadValueToRegister(calleeSymbol);
      }
      
      /*if (!calleeSymbol.Type.IsFunction()) {
        Symbol symbol = new Symbol(Type.VoidPointerType);
        jumpTrack = LoadValueToRegister(symbol);
      }*/

      if (calleeEllipse && (extraSize > 0)) {
        AddAssemblyCode(AssemblyOperator.add, AssemblyCode.EllipseRegister,
                        (BigInteger) extraSize);
      }

      if (calleeSymbol.Type.IsFunction()) {
        AddAssemblyCode(AssemblyOperator.call, calleeSymbol.UniqueName);
        m_returnFloating = calleeSymbol.Type.ReturnType.IsFloating();
      }
      else {
        AddAssemblyCode(AssemblyOperator.jmp, jumpTrack);
        m_returnFloating =
          calleeSymbol.Type.PointerType.ReturnType.IsFloating();
      }            
    }
  
    public void FunctionPostCall(MiddleCode middleCode) {
      Register baseRegister = BaseRegister(null);
      m_trackMap = m_trackMapStack.Pop();
      IDictionary<Track,int> postMap = m_postMapStack.Pop();

      foreach (KeyValuePair<Track,int> pair in postMap) {
        Track track = pair.Key;
        int offset = pair.Value;
        AddAssemblyCode(AssemblyOperator.mov, track, baseRegister,offset);
      }

      if (m_floatStackSize > 0) {
        int recordOffset = (int) middleCode[2];
        int recordSize = m_recordSizeStack.Pop();

        if (m_returnFloating) {
          AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister,
                          recordOffset + recordSize);
        }

        int currentOffset = recordOffset + recordSize;
        for (int count = 0; count < m_floatStackSize; ++count) {
          currentOffset -= 8;
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister,
                          currentOffset);
        }

        if (m_returnFloating) {
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister,
                          recordOffset + recordSize);
        }

        m_totalRecordSize -= recordSize;
      }
      else {
        m_totalRecordSize -= m_recordSizeStack.Pop();
      }
    }
	
    // -----------------------------------------------------------------------

    public Track LoadValueToRegister(Symbol symbol,
                                     Register? register = null) {
      if (register != null) {
        CheckRegister(symbol, register);
      }

      Track track;
      if (m_trackMap.TryGetValue(symbol, out track)) {
        m_trackMap.Remove(symbol);

        if ((register != null) && (track.Register != null) &&
            !AssemblyCode.RegisterOverlap(register, track.Register)) {
          Track newTrack = new Track(symbol, register);
          AddAssemblyCode(AssemblyOperator.set_track_size,
                          newTrack, track);
          AddAssemblyCode(AssemblyOperator.mov, newTrack, track);
          track = newTrack;
        }
        else if (register != null) {
          track.Register = register;
        }

        return track;
      }
      else {
        track = new Track(symbol, register);
        Assert.ErrorXXX(!(symbol.Type.IsFunction()));

        if ((symbol.Value is BigInteger) ||
            (symbol.IsExternOrStatic() &&
             symbol.Type.IsArrayFunctionStringStructOrUnion())) {
          AddAssemblyCode(AssemblyOperator.mov, track,
                          ValueOrAddress(symbol));
        }
        else if (symbol.Value is StaticAddress) {
          StaticAddress staticAddress = (StaticAddress) symbol.Value;
          AddAssemblyCode(AssemblyOperator.mov, track,
                          staticAddress.UniqueName);

          if (staticAddress.Offset > 0) {
            AddAssemblyCode(AssemblyOperator.add, track,
                            (BigInteger) staticAddress.Offset);
          }
        }
        else if (symbol.Type.IsArray()) {
          AddAssemblyCode(AssemblyOperator.mov, track, Base(symbol));

          int symbolOffset = Offset(symbol);
          if (symbolOffset != 0) {
            AddAssemblyCode(AssemblyOperator.add, track,
                            (BigInteger) symbolOffset);
          }
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, track,
                          Base(symbol), Offset(symbol));
        }
        
        return track;
      }
    }

    public void SaveValueFromRegister(Track track, Symbol symbol) {
      AddAssemblyCode(AssemblyOperator.mov, Base(symbol),
                      Offset(symbol), track);
    }

    public void CheckRegister(Symbol symbol, Register? register) {
      foreach (KeyValuePair<Symbol,Track> entry in m_trackMap) {
        Symbol oldSymbol = entry.Key;
        Track oldTrack = entry.Value;

        if (!oldSymbol.Equals(symbol) &&
            AssemblyCode.RegisterOverlap(register, oldTrack.Register)) {
          Track newTrack = new Track(oldSymbol);
          m_trackMap[oldSymbol] = newTrack;

          int lastLine;
          for (lastLine = m_assemblyCodeList.Count - 1; lastLine >= 0;
               --lastLine) {
            AssemblyCode assemblyCode = m_assemblyCodeList[lastLine];
            if (oldTrack.Equals(assemblyCode[0])) {
              break;
            }    
          }
          Assert.ErrorXXX(lastLine >= 0);

          AssemblyCode setCode =
            new AssemblyCode(AssemblyOperator.set_track_size,
                             newTrack, oldTrack);
          AssemblyCode movCode =
            new AssemblyCode(AssemblyOperator.mov, newTrack, oldTrack);
          m_assemblyCodeList.Insert(lastLine + 1, setCode);
          m_assemblyCodeList.Insert(lastLine + 2, movCode);
          break;
        }
      }
    }
  
    public void Return(MiddleCode middleCode) {
      Assert.ErrorXXX(m_floatStackSize == 0);
      //Type type = Type.SizeToUnsignedType(Type.ReturnAddressSize);
      Track track = new Track(Type.VoidPointerType);
      AddAssemblyCode(AssemblyOperator.mov, track,
                      AssemblyCode.FrameRegister,
                      SymbolTable.ReturnAddressOffset);                
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                      AssemblyCode.FrameRegister,
                      SymbolTable.EllipseFrameOffset);
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister,
                      AssemblyCode.FrameRegister,
                      SymbolTable.RegularFrameOffset);
      AddAssemblyCode(AssemblyOperator.jmp, track);
    }
  
    public void Exit(MiddleCode middleCode) {
      Symbol exitSymbol = (Symbol) middleCode[0];

      if (Start.Windows) {        
        if (exitSymbol == null) {
          AddAssemblyCode(AssemblyOperator.mov, Register.al,
                          BigInteger.Zero);
        }
        else {
          LoadValueToRegister(exitSymbol, Register.al);
        }

        AddAssemblyCode(AssemblyOperator.mov, Register.ah,
                        (BigInteger) 0x4C);
        AddAssemblyCode(AssemblyOperator.interrupt, (BigInteger) 33);
      }

      if (Start.Linux) {        
        if (exitSymbol == null) {
          AddAssemblyCode(AssemblyOperator.mov, Register.rdi,
                          BigInteger.Zero);
        }
        else {
          LoadValueToRegister(exitSymbol, Register.rdi);
        }

        AddAssemblyCode(AssemblyOperator.mov, Register.rax,
                        (BigInteger) 0x3C);
        AddAssemblyCode(AssemblyOperator.syscall);
      }
    }

    public void Goto(MiddleCode middleCode) {
      AddAssemblyCode(AssemblyOperator.jmp, null, null,
                      middleCode[0]);
    }

    public void LoadToRegister(MiddleCode middleCode) {
      Register register = (Register) middleCode[0];
      Symbol symbol = (Symbol) middleCode[1];
      Track track = LoadValueToRegister(symbol, register);
      m_syscallSet.Add(track);
    }

    public void InspectRegister(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode[0];
      Register register = (Register) middleCode[1];
      Track track = new Track(symbol, register);
      m_trackMap.Add(symbol, track);
    }
  
    public void JumpRegister(MiddleCode middleCode) {
      Register jumpRegister = (Register) middleCode[0];
      AddAssemblyCode(AssemblyOperator.jmp, jumpRegister);
    }

    public void Interrupt(MiddleCode middleCode) {
      foreach (Track track in m_syscallSet) {        
        AddAssemblyCode(AssemblyOperator.empty, track);
      }

      AddAssemblyCode(AssemblyOperator.interrupt,
                      (BigInteger) middleCode[0]);
      m_trackMap.Clear();
    }

    public void SystemCall(MiddleCode middleCode) {
      foreach (Track track in m_syscallSet) {        
        AddAssemblyCode(AssemblyOperator.empty, track);
      }

      AddAssemblyCode(AssemblyOperator.syscall);
      m_trackMap.Clear();
    }

    public void CarryExpression(MiddleCode middleCode) {
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null,
                      middleCode[0]);
    }

    private void Initializer(MiddleCode middleCode) {
      Sort sort = (Sort) middleCode[0];
      object value = middleCode[1];

      if (value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) value;
        string name = staticAddress.UniqueName;
        int offset = staticAddress.Offset;
        // dw name + offset
        AddAssemblyCode(AssemblyOperator.define_address, name, offset);
      }
      else {
        AddAssemblyCode(AssemblyOperator.define_value, sort, value);
      }
    }

    private void InitializerZero(MiddleCode middleCode) {
      int size = (int) middleCode[0];

      Assert.ErrorXXX(size >= 0);
      if (size > 0) {
        AddAssemblyCode(AssemblyOperator.define_zero_sequence, size);
      }
    }

    // -----------------------------------------------------------------------

    public void IntegralAssign(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0],
             assignSymbol = (Symbol) middleCode[1];

      Track resultTrack = null, assignTrack = null;
      m_trackMap.TryGetValue(resultSymbol, out resultTrack);
      m_trackMap.TryGetValue(assignSymbol, out assignTrack);

      if ((resultSymbol.IsTemporary()) &&
          (resultSymbol.AddressSymbol == null)) {
        if (assignTrack != null) {
          if (resultTrack != null) {
            resultTrack.Replace(m_assemblyCodeList, assignTrack);
          }

          m_trackMap[resultSymbol] = assignTrack;
          m_trackMap.Remove(assignSymbol);
        }
        else {
          if (resultTrack == null) {
            resultTrack = new Track(resultSymbol);
            m_trackMap.Add(resultSymbol, resultTrack);
          }

          if ((assignSymbol.Value is BigInteger) ||
              (assignSymbol.IsExternOrStatic() &&
               assignSymbol.Type.IsArrayFunctionOrString())) {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            ValueOrAddress(assignSymbol));
          }
          else if (assignSymbol.Type.IsArrayOrFunction()) {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            Base(assignSymbol));
            AddAssemblyCode(AssemblyOperator.add, resultTrack,
                            (BigInteger) Offset(assignSymbol));
          }
          else {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            Base(assignSymbol), Offset(assignSymbol));
          }
        }
      }
      else {
        if (assignSymbol.Value is BigInteger) {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.mov,
                                        assignSymbol.Type.Size());
          BigInteger assignValue = (BigInteger) assignSymbol.Value;
          AddAssemblyCode(sizeOperator, Base(resultSymbol),
                          Offset(resultSymbol), assignValue);
        }
        else if (assignTrack != null) {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), assignTrack);
          m_trackMap.Remove(assignSymbol);
        }
        else if (assignSymbol.Type.IsArrayFunctionOrString()) {
          if (assignSymbol.IsExternOrStatic()) {
            AssemblyOperator sizeMovOperator =
               AssemblyCode.OperatorToSize(AssemblyOperator.mov,
                                           TypeSize.PointerSize);
            AddAssemblyCode(sizeMovOperator, Base(resultSymbol),
                            Offset(resultSymbol), assignSymbol.UniqueName);
          }
          else  {
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), Base(assignSymbol));
            AssemblyOperator sizeAddOperator =
               AssemblyCode.OperatorToSize(AssemblyOperator.add,
                                           TypeSize.PointerSize);

            if (assignSymbol.Offset > 0) {
              AddAssemblyCode(sizeAddOperator, Base(resultSymbol),
                  Offset(resultSymbol), (BigInteger) assignSymbol.Offset);
            }
          }
        }
        /*else if (assignSymbol.Value is StaticAddress) {
          StaticAddress staticAddress = (StaticAddress) assignSymbol.Value;
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), staticAddress.UniqueName);

          if (staticAddress.Offset != 0) {
            AddAssemblyCode(AssemblyOperator.add, Base(resultSymbol),
                            Offset(resultSymbol), staticAddress.Offset);
          }
        }*/
        else {
          assignTrack = LoadValueToRegister(assignSymbol);
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), assignTrack);
          m_trackMap.Remove(assignSymbol);
        }
      }
    }

    public static IDictionary<MiddleOperator,AssemblyOperator>
                       m_middleToIntegralMap =
                       new Dictionary<MiddleOperator,AssemblyOperator>();

    public static IDictionary<int,Register> LeftMultiplyMap =
       new Dictionary<int,Register>();

    public static IDictionary<Pair<MiddleOperator,int>, Register>
     ResultMultiplyMap = new Dictionary<Pair<MiddleOperator,int>, Register>();

    public static IDictionary<Pair<MiddleOperator,int>,Register>
     ClearMultiplyMap = new Dictionary<Pair<MiddleOperator,int>, Register>();

    /*public static IDictionary<MiddleOperator, AssemblyOperator>
      m_middleToIntegralUnaryTargetMap =
        new Dictionary<MiddleOperator, AssemblyOperator>();*/

    public static IDictionary<Sort, AssemblyOperator> m_floatPushMap =
      new Dictionary<Sort, AssemblyOperator>();

    public static IDictionary<Sort,AssemblyOperator>
      m_floatTopMap = new Dictionary<Sort,AssemblyOperator>(),
      m_floatPopMap = new Dictionary<Sort,AssemblyOperator>();

    public static IDictionary<MiddleOperator,AssemblyOperator>
      m_middleToFloatingMap =
        new Dictionary<MiddleOperator,AssemblyOperator>();

    static AssemblyCodeGenerator() {
      m_middleToIntegralMap.
        Add(MiddleOperator.Assign, AssemblyOperator.mov);
      m_middleToIntegralMap.
        Add(MiddleOperator.Parameter, AssemblyOperator.mov);
      m_middleToIntegralMap.
        Add(MiddleOperator.Compare, AssemblyOperator.cmp);
      m_middleToIntegralMap.
        Add(MiddleOperator.BinaryAdd, AssemblyOperator.add);
      m_middleToIntegralMap.
        Add(MiddleOperator.BinarySubtract, AssemblyOperator.sub);
      m_middleToIntegralMap.
        Add(MiddleOperator.BitwiseAnd, AssemblyOperator.and);
      m_middleToIntegralMap.
        Add(MiddleOperator.BitwiseOr, AssemblyOperator.or);
      m_middleToIntegralMap.
        Add(MiddleOperator.BitwiseXOr, AssemblyOperator.xor);
      m_middleToIntegralMap.
        Add(MiddleOperator.ShiftLeft, AssemblyOperator.shl);
      m_middleToIntegralMap.
        Add(MiddleOperator.ShiftRight, AssemblyOperator.shr);

      m_middleToIntegralMap.
        Add(MiddleOperator.Equal, AssemblyOperator.je);
      m_middleToIntegralMap.
        Add(MiddleOperator.NotEqual, AssemblyOperator.jne);
      m_middleToIntegralMap.
        Add(MiddleOperator.Carry, AssemblyOperator.jc);
      m_middleToIntegralMap.
        Add(MiddleOperator.NotCarry, AssemblyOperator.jnc);
      m_middleToIntegralMap.
        Add(MiddleOperator.SignedLessThan, AssemblyOperator.jl);
      m_middleToIntegralMap.
        Add(MiddleOperator.SignedLessThanEqual,AssemblyOperator.jle);
      m_middleToIntegralMap.
        Add(MiddleOperator.SignedGreaterThan, AssemblyOperator.jg);
      m_middleToIntegralMap.
        Add(MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.jge);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnsignedLessThan, AssemblyOperator.jb);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnsignedLessThanEqual, AssemblyOperator.jbe);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnsignedGreaterThan, AssemblyOperator.ja);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnsignedGreaterThanEqual, AssemblyOperator.jae);

      LeftMultiplyMap.Add(1, Register.al);
      LeftMultiplyMap.Add(2, Register.ax);
      LeftMultiplyMap.Add(4, Register.eax);
      LeftMultiplyMap.Add(8, Register.rax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedMultiply, 1), Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedMultiply, 2), Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedMultiply, 4), Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedMultiply, 8), Register.rax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedMultiply, 1), Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedMultiply, 2), Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedMultiply, 4), Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedMultiply, 8), Register.rax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 1), Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 2), Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 4), Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 8), Register.rax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 1), Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 2), Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 4), Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 8), Register.rax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 1), Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 2), Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 4), Register.edx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 8), Register.rdx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 1), Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 2), Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 4), Register.edx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 8), Register.rdx);

      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedMultiply, 1), Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedMultiply, 2), Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
       (MiddleOperator.SignedMultiply, 4), Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedMultiply, 8), Register.rdx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
       (MiddleOperator.UnsignedMultiply, 1), Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedMultiply, 2), Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedMultiply, 4), Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedMultiply, 8), Register.rdx);

      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 1), Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 2), Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 4), Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedDivide, 8), Register.rdx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 1), Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 2), Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 4), Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedDivide, 8), Register.rdx);

      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 1), Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 2), Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 4), Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.SignedModulo, 8), Register.rdx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 1), Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 2), Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 4), Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator,int>
        (MiddleOperator.UnsignedModulo, 8), Register.rdx);

      m_middleToIntegralMap.
        Add(MiddleOperator.UnaryAdd, AssemblyOperator.empty);
      m_middleToIntegralMap.
        Add(MiddleOperator.BitwiseNot, AssemblyOperator.not);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnarySubtract, AssemblyOperator.neg);
      m_middleToIntegralMap.
        Add(MiddleOperator.Increment, AssemblyOperator.inc);
      m_middleToIntegralMap.
        Add(MiddleOperator.Decrement, AssemblyOperator.dec);
      m_middleToIntegralMap.
        Add(MiddleOperator.SignedMultiply, AssemblyOperator.imul);
      m_middleToIntegralMap.
        Add(MiddleOperator.SignedDivide, AssemblyOperator.idiv);
      m_middleToIntegralMap.
        Add(MiddleOperator.SignedModulo, AssemblyOperator.idiv);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnsignedMultiply, AssemblyOperator.mul);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnsignedDivide, AssemblyOperator.div);
      m_middleToIntegralMap.
        Add(MiddleOperator.UnsignedModulo, AssemblyOperator.div);

      m_floatPushMap.Add(Sort.Signed_Int, AssemblyOperator.fild_word);
      m_floatPushMap.Add(Sort.Unsigned_Int, AssemblyOperator.fild_word);
      m_floatPushMap.Add(Sort.Signed_Long_Int, AssemblyOperator.fild_dword);
      m_floatPushMap.Add(Sort.Unsigned_Long_Int, AssemblyOperator.fild_dword);
      m_floatPushMap.Add(Sort.Float, AssemblyOperator.fld_dword);
      m_floatPushMap.Add(Sort.Double, AssemblyOperator.fld_qword);
      m_floatPushMap.Add(Sort.Long_Double, AssemblyOperator.fld_qword);

      m_floatTopMap.Add(Sort.Signed_Int, AssemblyOperator.fist_word);
      m_floatTopMap.Add(Sort.Unsigned_Int, AssemblyOperator.fist_word);
      m_floatTopMap.Add(Sort.Pointer, AssemblyOperator.fist_word);
      m_floatTopMap.Add(Sort.Signed_Long_Int, AssemblyOperator.fist_dword);
      m_floatTopMap.Add(Sort.Unsigned_Long_Int, AssemblyOperator.fist_dword);
      m_floatTopMap.Add(Sort.Float, AssemblyOperator.fst_dword);
      m_floatTopMap.Add(Sort.Double, AssemblyOperator.fst_qword);
      m_floatTopMap.Add(Sort.Long_Double, AssemblyOperator.fst_qword);
  
      m_floatPopMap.Add(Sort.Signed_Int, AssemblyOperator.fistp_word);
      m_floatPopMap.Add(Sort.Unsigned_Int, AssemblyOperator.fistp_word);
      m_floatPopMap.Add(Sort.Pointer, AssemblyOperator.fistp_word);
      m_floatPopMap.Add(Sort.Signed_Long_Int, AssemblyOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Unsigned_Long_Int, AssemblyOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Float, AssemblyOperator.fstp_dword);
      m_floatPopMap.Add(Sort.Double, AssemblyOperator.fstp_qword);
      m_floatPopMap.Add(Sort.Long_Double, AssemblyOperator.fstp_qword);    

      m_middleToFloatingMap.
        Add(MiddleOperator.BinaryAdd, AssemblyOperator.fadd);
      m_middleToFloatingMap.
        Add(MiddleOperator.BinarySubtract, AssemblyOperator.fsub);
      m_middleToFloatingMap.
        Add(MiddleOperator.SignedMultiply, AssemblyOperator.fmul);
      m_middleToFloatingMap.
        Add(MiddleOperator.SignedDivide, AssemblyOperator.fdiv);

      m_middleToFloatingMap.
        Add(MiddleOperator.Equal, AssemblyOperator.je);
      m_middleToFloatingMap.
        Add(MiddleOperator.NotEqual, AssemblyOperator.jne);
      m_middleToFloatingMap.
        Add(MiddleOperator.SignedLessThan, AssemblyOperator.ja);
      m_middleToFloatingMap.
        Add(MiddleOperator.SignedLessThanEqual, AssemblyOperator.jae);
      m_middleToFloatingMap.
        Add(MiddleOperator.SignedGreaterThan, AssemblyOperator.jb);
      m_middleToFloatingMap.
        Add(MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.jbe);

      m_middleToFloatingMap.
        Add(MiddleOperator.UnaryAdd,AssemblyOperator.empty);
      m_middleToFloatingMap.
        Add(MiddleOperator.BitwiseNot,AssemblyOperator.empty);
      m_middleToFloatingMap.
        Add(MiddleOperator.UnarySubtract, AssemblyOperator.fchs);
    }

    public void FloatingRelation(MiddleCode middleCode, int index) {
      Assert.ErrorXXX((m_floatStackSize -= 2) >= 0);
      int target = (int) middleCode[0];
      AddAssemblyCode(AssemblyOperator.fcompp);
      AddAssemblyCode(AssemblyOperator.fstsw, Register.ax);
      AddAssemblyCode(AssemblyOperator.sahf);
      AssemblyOperator objectOperator =
        m_middleToFloatingMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, target);
    }

    public void IntegralAdditionBitwiseShift(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0],
             leftSymbol = (Symbol) middleCode[1],
             rightSymbol = (Symbol) middleCode[2];

      if (resultSymbol.Equals(leftSymbol) && !resultSymbol.IsTemporary()) {
        CompoundIntegralBinary(middleCode.Operator,
                                       leftSymbol, rightSymbol);
      }
      else {
        SimpleIntegralBinary(middleCode.Operator, resultSymbol,
                             leftSymbol, rightSymbol);
      }
    }
  
    public void CompoundIntegralBinary
      (MiddleOperator middleOperator, Symbol leftSymbol,
      Symbol rightSymbol) {
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleOperator];
      Assert.ErrorXXX(!m_trackMap.ContainsKey(leftSymbol));

      if (rightSymbol.Value is BigInteger) {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator,
                                      leftSymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(leftSymbol),
                        Offset(leftSymbol), ValueOrAddress(rightSymbol));
      }
      else if (rightSymbol.IsExternOrStatic() &&
               rightSymbol.Type.IsArrayFunctionOrString()) {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator, TypeSize.PointerSize);
        AddAssemblyCode(sizeOperator, Base(leftSymbol),
                        Offset(leftSymbol), ValueOrAddress(rightSymbol));
      }
      else if (MiddleCode.IsShift(middleOperator)) {
        Track rightTrack =
          LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
        AddAssemblyCode(AssemblyOperator.mov, Base(leftSymbol),
                        Offset(leftSymbol), rightTrack);
      }
      else {
        Track rightTrack = LoadValueToRegister(rightSymbol);
        AddAssemblyCode(AssemblyOperator.mov, Base(leftSymbol),
                        Offset(leftSymbol), rightTrack);
      }
    }

    public void SimpleIntegralBinary(MiddleOperator middleOperator,
                                     Symbol resultSymbol, Symbol leftSymbol,
                                     Symbol rightSymbol) {
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleOperator];
      Track leftTrack = LoadValueToRegister(leftSymbol), rightTrack;

      if (m_trackMap.TryGetValue(rightSymbol, out rightTrack)) {
        AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        m_trackMap.Remove(rightSymbol);
      }
      else if ((rightSymbol.Value is BigInteger) ||
               rightSymbol.Type.IsArrayFunctionOrString()) {
        AddAssemblyCode(objectOperator, leftTrack,
                        ValueOrAddress(rightSymbol));
      }
      else if (MiddleCode.IsShift(middleOperator)) {
        rightTrack =
          LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
        AddAssemblyCode(objectOperator, leftTrack, rightTrack);
      }
      else {
        AddAssemblyCode(objectOperator, leftTrack,
                        Base(rightSymbol), Offset(rightSymbol));
      }

      Assert.ErrorXXX(resultSymbol.IsTemporary() &&
                    (resultSymbol.AddressSymbol == null));
      m_trackMap.Add(resultSymbol, leftTrack);
    }

    public void IntegralRelationBinary(Symbol leftSymbol,
                                       Symbol rightSymbol) {
      Track leftTrack = null, rightTrack = null;
      m_trackMap.TryGetValue(leftSymbol, out leftTrack);
      m_trackMap.TryGetValue(rightSymbol, out rightTrack);

      if ((leftTrack == null) && (rightTrack == null)) {
        if ((leftSymbol.AddressSymbol != null) ||
            (leftSymbol.IsExternOrStatic() &&
             !leftSymbol.Type.IsArrayFunctionOrString()) ||
            (leftSymbol.IsAutoOrRegister() &&
             !leftSymbol.Type.IsArray())) {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.cmp,
                                        leftSymbol.Type.Size());

          if ((rightSymbol.Value is BigInteger) ||
              (rightSymbol.IsExternOrStatic() &&
              rightSymbol.Type.IsArrayFunctionOrString())) {
            AddAssemblyCode(sizeOperator, Base(leftSymbol),
                            Offset(leftSymbol), ValueOrAddress(rightSymbol));
            return;
          }
        }

        if (rightSymbol.IsAutoOrRegister() &&
            rightSymbol.Type.IsArray()) {
          rightTrack = LoadValueToRegister(rightSymbol);
        }
        else {
          leftTrack = LoadValueToRegister(leftSymbol);
        }
      }

      if (leftTrack != null) {
        if (rightSymbol.Type.IsArrayOrFunction()) {
          rightTrack = LoadValueToRegister(rightSymbol);
        }

        if ((rightSymbol.Value is BigInteger) ||
            (rightSymbol.IsExternOrStatic() &&
             rightSymbol.Type.IsArrayFunctionOrString())) {
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack,
                          ValueOrAddress(rightSymbol)); // cmp ax, 123
        }
        else if (rightTrack != null) { // cmp ax, bx
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack);
          m_trackMap.Remove(rightSymbol);
        }
        else { // cmp ax, [bp + 2]
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack,
                          Base(rightSymbol), Offset(rightSymbol));
        }

        m_trackMap.Remove(leftSymbol);
      }
      else { // rightTrack != null
        Assert.ErrorXXX(!(leftSymbol.Value is BigInteger));

        if ((leftSymbol.IsExternOrStatic() &&
             leftSymbol.Type.IsArrayFunctionOrString()) ||
             (leftSymbol.IsAutoOrRegister() &&
              leftSymbol.Type.IsArray())) {
          leftTrack = LoadValueToRegister(leftSymbol);
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack);
        }
        else {
          AddAssemblyCode(AssemblyOperator.cmp, Base(leftSymbol),
                          Offset(leftSymbol), rightTrack);
        }

        m_trackMap.Remove(rightSymbol);
      }
    }

    private object ValueOrAddress(Symbol symbol) {
      if (symbol.Value is BigInteger) {
        return ((BigInteger) symbol.Value);
      }
      else if (symbol.IsAutoOrRegister()) {
        Track track = new Track(symbol);
        AddAssemblyCode(AssemblyOperator.mov, track, Base(symbol));
        AddAssemblyCode(AssemblyOperator.add, track,
                        (BigInteger) Offset(symbol));
        return track;
      }
      else {
        return symbol.UniqueName;
      }
    }

    public Register BaseRegister(Symbol symbol) {
      Assert.ErrorXXX((symbol == null) || symbol.IsAutoOrRegister());
    
      if (SymbolTable.CurrentFunction.Type.IsEllipse() &&
          ((symbol == null) || !symbol.IsParameter())) {
        return AssemblyCode.EllipseRegister;
      }
      else {
        return AssemblyCode.FrameRegister;
      }
    }

    private object Base(Symbol symbol) {
      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        return staticAddress.UniqueName;
      }
      else if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        Assert.ErrorXXX((addressTrack.Register == null) ||
                        RegisterAllocator.PointerRegisterSetWithEllipse.
                        Contains(addressTrack.Register.Value));
        addressTrack.Pointer = true;
        m_trackMap.Remove(symbol.AddressSymbol);
        return addressTrack;
      }
      else if (symbol.IsExternOrStatic()) {
        return symbol.UniqueName;
      }
      else { //resultSymbol.IsAutoOrRegister()
        return BaseRegister(symbol);
      }
    }

    private int Offset(Symbol symbol) {
      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        return staticAddress.Offset;
      }
      else if (symbol.AddressSymbol != null) {
        return symbol.AddressOffset;
      }
      else {
        return symbol.Offset;
      }
    }
  
    public void Unary(MiddleOperator middleOperator, Symbol resultSymbol,
                              Symbol unarySymbol) {
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleOperator];

      // ++i; i = ~i; dec [bp + 6]; not [bp + 6]
      if ((resultSymbol == null) || resultSymbol.Equals(unarySymbol)) {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator,
                                      unarySymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(unarySymbol),
                        Offset(unarySymbol));
      }
      else { // t0 = -i; mov ax, [i], neg ax
        Track unaryTrack = LoadValueToRegister(unarySymbol);
        AddAssemblyCode(objectOperator, unaryTrack);
        m_trackMap.Add(resultSymbol, unaryTrack);
      }
    }

    public void IntegralMultiply(MiddleCode middleCode) {
      Symbol leftSymbol = (Symbol) middleCode[1];
      Register leftRegister = LeftMultiplyMap[leftSymbol.Type.SizeX()];
      Track leftTrack = LoadValueToRegister(leftSymbol, leftRegister);
      AddAssemblyCode(AssemblyOperator.empty, leftTrack);

      Pair<MiddleOperator,int> pair =
        new Pair<MiddleOperator,int>(middleCode.Operator,
                                     leftSymbol.Type.SizeX());
      Register clearRegister = ClearMultiplyMap[pair];
      Track clearTrack = new Track(leftSymbol, clearRegister);
      AddAssemblyCode(AssemblyOperator.xor, clearTrack, clearTrack);

      Symbol rightSymbol = (Symbol) middleCode[2];
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleCode.Operator];

      if (rightSymbol.IsTemporary() && (rightSymbol.AddressSymbol == null)) {
        Track rightTrack = LoadValueToRegister(rightSymbol);
        AddAssemblyCode(objectOperator, rightTrack);
      }
      else {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator,
                                      rightSymbol.Type.SizeX());
        AddAssemblyCode(sizeOperator, Base(rightSymbol),
                        Offset(rightSymbol));

        if (rightSymbol.Value != null) {
          SymbolTable.StaticSet.Add(ConstantExpression.Value(rightSymbol));
        }
      }

      Symbol resultSymbol = (Symbol) middleCode[0];
      Register resultRegister = ResultMultiplyMap[pair];
      Track resultTrack = new Track(resultSymbol, resultRegister);

      Assert.ErrorXXX(resultSymbol.IsTemporary() &&
                    (resultSymbol.AddressSymbol == null));
      m_trackMap.Add(resultSymbol, resultTrack);
      AddAssemblyCode(AssemblyOperator.empty, resultTrack);
    }

    public void Case(MiddleCode middleCode) {
      Symbol switchSymbol = (Symbol) middleCode[1];
      Track switchTrack;

      if (m_trackMap.ContainsKey(switchSymbol)) {
        switchTrack = m_trackMap[switchSymbol];
      }
      else {
        switchTrack = new Track(switchSymbol);
        m_trackMap.Add(switchSymbol, switchTrack);
        AddAssemblyCode(AssemblyOperator.mov, switchTrack,
                        Base(switchSymbol), Offset(switchSymbol));
      }

      Symbol caseSymbol = (Symbol) middleCode[2];
      BigInteger caseValue = (BigInteger) caseSymbol.Value; // cmp ax, 123
      AddAssemblyCode(AssemblyOperator.cmp, switchTrack, caseValue);
      // Note: no m_trackMap.Remove(symbol);
      int target = (int) middleCode[0];
      AddAssemblyCode(AssemblyOperator.je, null, null, target);
    }
    
    public void CaseEnd(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode[0];
      m_trackMap.Remove(symbol);
    }

    public void IntegralRelation(MiddleCode middleCode, int index) {
      IntegralRelationBinary((Symbol) middleCode[1],
                             (Symbol) middleCode[2]);
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleCode.Operator];
      int target = (int) middleCode[0];
      AddAssemblyCode(objectOperator, null, null, target);
    }

    public void IntegralIncrementDecrement(MiddleCode middleCode) {
      Unary(middleCode.Operator, (Symbol) middleCode[0],
                    (Symbol) middleCode[1]);
    }

    public void IntegralUnary(MiddleCode middleCode) {
      Unary(middleCode.Operator, (Symbol) middleCode[0],
                    (Symbol) middleCode[1]);
    }

    public void Address(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0],
             addressSymbol = (Symbol) middleCode[1];

      Track track = LoadAddressToRegister(addressSymbol);
      m_trackMap.Add(resultSymbol, track);
      m_trackMap.Remove(addressSymbol);
    }

    public void FloatingDereference(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0];
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);
    }

    public void IntegralDereference(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0];
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);
    }

    public Track LoadAddressToRegister(Symbol symbol,
                                       Register? register = null) {
      Symbol addressSymbol = symbol.AddressSymbol;
    
      if (addressSymbol != null) {
        Track addressTrack = LoadValueToRegister(addressSymbol);
        Assert.ErrorXXX((addressTrack.Register == null) ||
                        RegisterAllocator.PointerRegisterSetWithEllipse.
                        Contains(addressTrack.Register.Value));
        addressTrack.Pointer = true;

        if ((register != null) && (addressTrack.Register == null) &&
            !AssemblyCode.RegisterOverlap(register, addressTrack.Register)) {
          Track newAddressTrack = new Track(symbol, register);
          newAddressTrack.Pointer = true;
          AddAssemblyCode(AssemblyOperator.set_track_size,
                          newAddressTrack, addressTrack);
          AddAssemblyCode(AssemblyOperator.mov, newAddressTrack,
                          addressTrack);
          addressTrack = newAddressTrack;
        }
        else if (register != null) {
          addressTrack.Register = register;
        }

        return addressTrack;
      }
      else {
        Symbol pointerSymbol = new Symbol(new Type(symbol.Type));
        Track addressTrack = new Track(pointerSymbol, register);
        Assert.ErrorXXX((addressTrack.Register == null) ||
                        RegisterAllocator.PointerRegisterSetWithEllipse.
                        Contains(addressTrack.Register.Value));
        addressTrack.Pointer = true;

        if (symbol.IsAutoOrRegister()) {
          AddAssemblyCode(AssemblyOperator.mov, addressTrack,
                          BaseRegister(symbol));
          AddAssemblyCode(AssemblyOperator.add, addressTrack,
                          (BigInteger) symbol.Offset);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, addressTrack,
                          symbol.UniqueName);
        }

        return addressTrack;
      }
    }

    public void PushSymbol(Symbol symbol) {
      Assert.ErrorXXX((++m_floatStackSize) <= FloatingStackMaxSize);
      AssemblyOperator objectOperator = m_floatPushMap[symbol.Type.Sort];
      Track track;

      if (((symbol.Value is BigInteger) &&
          (((BigInteger) symbol.Value).IsZero)) ||
          ((symbol.Value is decimal) && (((decimal) symbol.Value) == 0))) {
        AddAssemblyCode(AssemblyOperator.fldz);
      }
      else if (((symbol.Value is BigInteger) &&
                (((BigInteger) symbol.Value).IsOne)) ||
               ((symbol.Value is decimal) &&
                (((decimal) symbol.Value) == 1))) {
        AddAssemblyCode(AssemblyOperator.fld1);
      }
      else if ((symbol.Value is BigInteger) || (symbol.Value is decimal)) {
        AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
        SymbolTable.StaticSet.Add(ConstantExpression.Value(symbol));
      }
      else if (symbol.Type.IsArrayFunctionStringStructOrUnion()) {
        if (symbol.IsAutoOrRegister()) {
          AddAssemblyCode(AssemblyOperator.mov, IntegralStorageName, 0,
                          BaseRegister(symbol));
          AssemblyOperator addObjectOp =
            AssemblyCode.OperatorToSize(AssemblyOperator.add,
                                        TypeSize.PointerSize);
          AddAssemblyCode(addObjectOp, IntegralStorageName, 0,
                          (BigInteger) symbol.Offset);
        }
        else {
          AssemblyOperator movObjectOp =
            AssemblyCode.OperatorToSize(AssemblyOperator.mov,
                                        TypeSize.PointerSize);
          AddAssemblyCode(movObjectOp, IntegralStorageName, 0,
                          symbol.UniqueName);
        }

        AddAssemblyCode(objectOperator, IntegralStorageName, 0);
      }
      else if (m_trackMap.TryGetValue(symbol, out track)) {
        m_trackMap.Remove(symbol);
        AddAssemblyCode(AssemblyOperator.mov, IntegralStorageName, 0,
                        track);
        AddAssemblyCode(objectOperator, IntegralStorageName, 0);
      }
      else {
        AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
      }
    }

    public enum TopOrPop {Top, Pop};

    public void PopEmpty() {
      AddAssemblyCode(AssemblyOperator.fistp_word,
                      AssemblyCodeGenerator.IntegralStorageName, 0);
    }
      
    public void TopPopSymbol(Symbol symbol, TopOrPop topOrPop) {
      Assert.ErrorXXX(symbol != null);
      AssemblyOperator objectOperator;

      if (topOrPop == TopOrPop.Pop) {
        objectOperator = m_floatPopMap[symbol.Type.Sort];
        Assert.ErrorXXX((--m_floatStackSize) >= 0);
      }
      else {
        objectOperator = m_floatTopMap[symbol.Type.Sort];
      }
    
      if (symbol.IsTemporary() && (symbol.AddressSymbol == null) &&
          (symbol.Offset == 0)) {
        AddAssemblyCode(objectOperator,
                        AssemblyCodeGenerator.IntegralStorageName, 0);
        Track track = new Track(symbol);
        AddAssemblyCode(AssemblyOperator.mov, track,
                        AssemblyCodeGenerator.IntegralStorageName, 0);
        m_trackMap.Add(symbol, track);
      }
      else {
        AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
      }
    }

    public void IntegralToIntegral(MiddleCode middleCode, int index) {
      Symbol toSymbol = (Symbol) middleCode[0],
             fromSymbol = (Symbol) middleCode[1];

      Type toType = toSymbol.Type, fromType = fromSymbol.Type;
      int toSize = toType.SizeX(), fromSize = fromType.SizeX();

      Track fromTrack = LoadValueToRegister(fromSymbol);
      AddAssemblyCode(AssemblyOperator.set_track_size, fromTrack, toSize);

      if (fromSize != toSize) {
        if (fromSize < toSize) {
          if (toSize == 8) {
            Track toTrack = new Track(toSymbol);
            AddAssemblyCode(AssemblyOperator.mov, toTrack,
                            TypeSize.GetMask(fromType.Sort));
            AddAssemblyCode(AssemblyOperator.and, fromTrack, toTrack);
          }
          else {
            AddAssemblyCode(AssemblyOperator.and, fromTrack,
                            TypeSize.GetMask(fromType.Sort));
          }

        }

        if (fromType.IsSigned() && toType.IsSigned()) {
          AddAssemblyCode(AssemblyOperator.set_track_size,
                          fromTrack, fromSize);
          AddAssemblyCode(AssemblyOperator.cmp, fromTrack, BigInteger.Zero);
          AddAssemblyCode(AssemblyOperator.jge, null, null, index + 1);
          AddAssemblyCode(AssemblyOperator.neg, fromTrack);
          AddAssemblyCode(AssemblyOperator.set_track_size, fromTrack, toSize);
          AddAssemblyCode(AssemblyOperator.neg, fromTrack);
        }
      }

      m_trackMap.Add(toSymbol, fromTrack);
    }

    public void IntegralToFloating(MiddleCode middleCode) {
      Symbol fromSymbol = (Symbol) middleCode[1];
      PushSymbol(fromSymbol);
    }
  
    public void FloatingToIntegral(MiddleCode middleCode) {
      Symbol toSymbol = (Symbol) middleCode[0];
      TopPopSymbol(toSymbol, TopOrPop.Pop);
    }

    public void IntegralParameter(MiddleCode middleCode) {
      Symbol fromSymbol = (Symbol) middleCode[1];

      Symbol toSymbol;
      if (fromSymbol.Type.IsArray()) {
        toSymbol = new Symbol(new Type(fromSymbol.Type.ArrayType));
      }
      else if (fromSymbol.Type.IsFunction()) {
        toSymbol = new Symbol(new Type(fromSymbol.Type));
      }
      else {
        toSymbol = new Symbol(fromSymbol.Type);
      }

      toSymbol.Offset = m_totalRecordSize +
                        ((int) middleCode[2]);

      if (fromSymbol.Value is BigInteger) {
        BigInteger fromValue = (BigInteger) fromSymbol.Value;

        if (toSymbol.Type.Size() == 8) {
          Track fromTrack = new Track(fromSymbol);
          AddAssemblyCode(AssemblyOperator.mov, fromTrack, fromValue);
          AddAssemblyCode(AssemblyOperator.mov, Base(toSymbol),
                          Offset(toSymbol), fromTrack);
        }
        else {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.mov,
                                        toSymbol.Type.Size());
          AddAssemblyCode(sizeOperator, Base(toSymbol),
                          Offset(toSymbol), fromValue);
        }
      }
      else if (fromSymbol.IsExternOrStatic() &&
               fromSymbol.Type.IsArrayFunctionOrString()) {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(AssemblyOperator.mov,
                                      TypeSize.PointerSize);
        AddAssemblyCode(sizeOperator, Base(toSymbol), Offset(toSymbol),
                        fromSymbol.UniqueName);
      }
      else if (fromSymbol.IsAutoOrRegister() &&
               fromSymbol.Type.IsArrayOrFunction()) {
        AddAssemblyCode(AssemblyOperator.mov, Base(toSymbol),
                        Offset(toSymbol), Base(fromSymbol));
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(AssemblyOperator.add,
                                      toSymbol.Type.Size());

        int offset = Offset(fromSymbol);
        if (offset > 0) {
          AddAssemblyCode(sizeOperator, Base(toSymbol), Offset(toSymbol),
                          (BigInteger) offset);
        }
      }
      else {
        Track fromTrack = LoadValueToRegister(fromSymbol);
        AddAssemblyCode(AssemblyOperator.mov, Base(toSymbol),
                        Offset(toSymbol), fromTrack);
        m_trackMap.Remove(fromSymbol);
      }
    }

    public void IntegralGetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode[0];
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister,
                                    returnSymbol.Type.Size());
      CheckRegister(returnSymbol, returnRegister);
      Track returnTrack = new Track(returnSymbol, returnRegister);
      m_trackMap.Add(returnSymbol, returnTrack);
      AddAssemblyCode(AssemblyOperator.empty, returnTrack);
    }

    public void IntegralSetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode[1];
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister,
                                    returnSymbol.Type.Size());
      LoadValueToRegister(returnSymbol, returnRegister);
      m_trackMap.Remove(returnSymbol);
    }

    public void FloatingBinary(MiddleCode middleCode) {
      Assert.ErrorXXX((--m_floatStackSize) >= 0);
      AddAssemblyCode(m_middleToFloatingMap[middleCode.Operator]);
    }

    public void FloatingUnary(MiddleCode middleCode) {
      AddAssemblyCode(m_middleToFloatingMap[middleCode.Operator]);
    }

    public void FloatingParameter(MiddleCode middleCode) {
      Symbol paramSymbol = (Symbol) middleCode[1];
      Symbol saveSymbol = new Symbol(paramSymbol.Type);
      saveSymbol.Offset = ((int) middleCode[2]) + m_totalRecordSize;
      TopPopSymbol(saveSymbol, TopOrPop.Pop);
    }

    public void StructUnionAssign(MiddleCode middleCode, int index) {
      Symbol targetSymbol = (Symbol) middleCode[0],
             sourceSymbol = (Symbol) middleCode[1];

      Track targetAddressTrack = LoadAddressToRegister(targetSymbol),
            sourceAddressTrack = LoadAddressToRegister(sourceSymbol);

      MemoryCopy(targetAddressTrack, sourceAddressTrack,
                         targetSymbol.Type.Size(), index);
    }

    public void StructUnionParameter(MiddleCode middleCode, int index) {
      Symbol sourceSymbol = (Symbol) middleCode[1];
      Symbol targetSymbol = new Symbol(Type.PointerTypeX);

      int paramOffset = ((int) middleCode[2]) +
                        m_totalRecordSize;
      targetSymbol.Offset = paramOffset;

      Track sourceAddressTrack = LoadAddressToRegister(sourceSymbol);
      Track targetAddressTrack = LoadAddressToRegister(targetSymbol);

      MemoryCopy(targetAddressTrack, sourceAddressTrack,
                         sourceSymbol.Type.Size(), index);    
    }

    public void StructUnionGetReturnValue(MiddleCode middleCode) {
      Symbol targetSymbol = (Symbol) middleCode[0];
      CheckRegister(targetSymbol, AssemblyCode.ReturnPointerRegister);
      Track targetAddressTrack =
        new Track(targetSymbol.AddressSymbol, 
                  AssemblyCode.ReturnPointerRegister);
      m_trackMap.Add(targetSymbol.AddressSymbol, targetAddressTrack);
    }

    public void StructUnionSetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode[1];
      LoadAddressToRegister(returnSymbol, AssemblyCode.ReturnPointerRegister);
    }

    private static int m_labelCount = 0;
    public void MemoryCopy(Track targetAddressTrack,
                           Track sourceAddressTrack, int size, int index) {
      Type countType = (size < 256) ? Type.UnsignedCharType
                                    : Type.UnsignedIntegerType;
      Track countTrack = new Track(countType),
            valueTrack = new Track(Type.UnsignedCharType);

      AddAssemblyCode(AssemblyOperator.mov, countTrack, (BigInteger) size);
      int labelIndex = m_labelCount++;
      AddAssemblyCode(AssemblyOperator.label, "x" + labelIndex);
      AddAssemblyCode(AssemblyOperator.mov, valueTrack,
                      sourceAddressTrack, 0);
      AddAssemblyCode(AssemblyOperator.mov, targetAddressTrack,
                      0, valueTrack);
      AddAssemblyCode(AssemblyOperator.inc, sourceAddressTrack);
      AddAssemblyCode(AssemblyOperator.inc, targetAddressTrack);
      AddAssemblyCode(AssemblyOperator.dec, countTrack);
      AddAssemblyCode(AssemblyOperator.cmp, countTrack, BigInteger.Zero);
      AddAssemblyCode(AssemblyOperator.jne, null, labelIndex);
    }

    public static void InitializerializationCodeList() {
      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                      "Initializerialize Stack Pointer");
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                 AssemblyCode.FrameRegister, LinkerWindows.StackTopName);

      if (Start.Windows) {
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                        "Initializerialize Heap Pointer");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                        null, 65534, (BigInteger) 65534);
      }

      if (Start.Linux) {
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                        "Initializerialize Heap Pointer");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_dword,
                        LinkerWindows.StackTopName, 65534,
                        LinkerWindows.StackTopName);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add_dword,
                        LinkerWindows.StackTopName, 65534,
                        (BigInteger) 65534);
      }

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                      "Initializerialize FPU Control Word, truncate mode " +
                      "=> set bit 10 and 11.");
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.fstcw,
                      AssemblyCode.FrameRegister, 0);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.or_word,
                      AssemblyCode.FrameRegister, 0, (BigInteger) 0x0C00);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.fldcw,
                      AssemblyCode.FrameRegister, 0);

      if (Start.Windows) {
        List<byte> byteList = new List<byte>();
        IDictionary<int, string> accessMap = new Dictionary<int, string>();
        IDictionary<int, string> callMap = new Dictionary<int, string>();
        ISet<int> returnSet = new HashSet<int>();
        AssemblyCodeGenerator.GenerateTargetWindows(assemblyCodeList,
                              byteList, accessMap, callMap, returnSet);
        StaticSymbol staticSymbol =
          new StaticSymbolWindows(AssemblyCodeGenerator.InitializerName,
                                  byteList, accessMap, callMap, returnSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }

      if (Start.Linux) {
        List<string> textList = new List<string>();
        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.TextList(assemblyCodeList, textList, externSet);
        //GenerateStaticInitializerLinux.TextList
        //                         (assemblyCodeList, textList, externSet);
        SymbolTable.StaticSet.Add(new StaticSymbolLinux
       (StaticSymbolLinux.TextOrData.Text,
        AssemblyCodeGenerator.InitializerName, textList, externSet));
      }
    }

    public static void ArgumentCodeList() {
      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();

      if (Start.Windows) {
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.si, Register.bp);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                        Register.bp, 0, AssemblyCodeGenerator.PathName);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                        Register.bp, (BigInteger) 2);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.ax, BigInteger.One);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bx, (BigInteger) 129);

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                        Register.bx, 0, (BigInteger) 32);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, assemblyCodeList.Count + 5);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                        Register.bx, 0, (BigInteger) 13);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, assemblyCodeList.Count + 13);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                        null, assemblyCodeList.Count - 5);

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp,
                        Register.ax, BigInteger.One);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, assemblyCodeList.Count + 2);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte,
                        Register.bx, 0, BigInteger.Zero);

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                        Register.bx, 0, (BigInteger) 32);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, assemblyCodeList.Count - 2);
    
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bp, 0, Register.bx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                        Register.bp, (BigInteger) 2);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.ax);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                        null, assemblyCodeList.Count - 15);

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte,
                        Register.bx, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                        Register.bp, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                        Register.bp, (BigInteger) 2);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bp, 6, Register.ax);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bp, 8, Register.si);

        List<byte> byteList = new List<byte>();
        IDictionary<int, string> accessMap = new Dictionary<int, string>();
        IDictionary<int, string> callMap = new Dictionary<int, string>();
        ISet<int> returnSet = new HashSet<int>();
        AssemblyCodeGenerator.
          GenerateTargetWindows(assemblyCodeList, byteList,
                                accessMap, callMap, returnSet);
        StaticSymbol staticSymbol =
          new StaticSymbolWindows(AssemblyCodeGenerator.ArgsName, byteList,
                                  accessMap, callMap, returnSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }

      if (Start.Linux) {
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                        "Initializerialize Command Line Arguments");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.pop, Register.rbx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.rax, Register.rbx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.rdx, Register.rbp);

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.label,
                        "$args$loop");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp,
                        Register.rbx, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, null, "$args$exit");

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.pop, Register.rsi);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.rbp, 0, Register.rsi);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add, Register.rbp,
                        (BigInteger) TypeSize.PointerSize);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.dec, Register.rbx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                        null, null, "$args$loop");

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.label,
                        "$args$exit");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_qword,
                        Register.rbp, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add, Register.rbp,
                        (BigInteger) TypeSize.PointerSize);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.rbp,
                        SymbolTable.FunctionHeaderSize, Register.eax);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.rbp,
                        SymbolTable.FunctionHeaderSize +
                        TypeSize.SignedIntegerSize, Register.rdx);

        List<string> textList = new List<string>();
        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.TextList(assemblyCodeList, textList, externSet);
        //GenerateStaticInitializerLinux.TextList(assemblyCodeList, textList,
        //                                        externSet);
        SymbolTable.StaticSet.
          Add(new StaticSymbolLinux(StaticSymbolLinux.TextOrData.Text,
                      AssemblyCodeGenerator.ArgsName, textList, externSet));
      }
    }

    private void JumpInfo() {
      IDictionary<int,int> middleToAssemblyMap = new Dictionary<int,int>();

      for (int assemblyIndex = 0;
           assemblyIndex < m_assemblyCodeList.Count; ++assemblyIndex) {
        AssemblyCode assemblyCode = m_assemblyCodeList[assemblyIndex];

        if (assemblyCode.Operator == AssemblyOperator.new_middle_code) {
          int middleIndex = (int) assemblyCode[0];
          middleToAssemblyMap.Add(middleIndex, assemblyIndex);
          assemblyCode.Operator = AssemblyOperator.empty;
        }
      }

      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        AssemblyCode assemblyCode = m_assemblyCodeList[line];

        if (assemblyCode.IsRelationNotRegister() ||
            assemblyCode.IsJumpNotRegister()) {
          if (!(assemblyCode[1] is int)) {
            int middleTarget = (int) assemblyCode[2];
            assemblyCode[1] = middleToAssemblyMap[middleTarget];
          }
        }
      }

      IDictionary<int,int> assemblyToByteMap = new Dictionary<int,int>();

      { int byteSize = 0, line = 0;
        foreach (AssemblyCode assemblyCode in m_assemblyCodeList) {
          assemblyToByteMap.Add(line++, byteSize);
  
          if (!(assemblyCode.IsRelationNotRegister() ||
                assemblyCode.IsJumpNotRegister())) {
            byteSize += assemblyCode.ByteList().Count;
          }
        }
        assemblyToByteMap.Add(m_assemblyCodeList.Count, byteSize);
      }

      while (true) {
        for (int line = 0; line < (m_assemblyCodeList.Count - 1); ++line) {
          AssemblyCode thisCode = m_assemblyCodeList[line],
                       nextCode = m_assemblyCodeList[line + 1];

          if (thisCode.IsRelationNotRegister() ||
              thisCode.IsJumpNotRegister()) {
            int assemblyTarget = (int) thisCode[1];
            int byteSource = assemblyToByteMap[line + 1],
                byteTarget = assemblyToByteMap[assemblyTarget];
            int byteDistance = byteTarget - byteSource;
            Assert.ErrorXXX(byteDistance != 0);
            thisCode[0] = byteDistance;
          }
        }

        bool update = false;
        { int byteSize = 0, line = 0;
          foreach (AssemblyCode objectCode in m_assemblyCodeList) {
            if (assemblyToByteMap[line] != byteSize) {
              assemblyToByteMap[line] = byteSize;
              update = true;
            }
            
            byteSize += objectCode.ByteList().Count;
            ++line;
          }
        }

        if (!update) {
          break;
        }
      }

      for (int line = 0; line < (m_assemblyCodeList.Count - 1); ++line) {
        AssemblyCode assemblyCode = m_assemblyCodeList[line];

        if (assemblyCode.Operator == AssemblyOperator.address_return) {
          int middleAddress = (int) ((BigInteger) assemblyCode[2]);
          int assemblyAddress = middleToAssemblyMap[middleAddress];
          int byteAddress = assemblyToByteMap[assemblyAddress];
          int nextAddress = assemblyToByteMap[line + 1];
          BigInteger byteReturn = byteAddress - nextAddress +
                                  TypeSize.PointerSize;
          assemblyCode[2] = byteReturn;
        }
      }
    }

    public static void TextList(IList<AssemblyCode> assemblyCodeList,
                                IList<string> textList,
                                ISet<string> externSet) {
      foreach (AssemblyCode assemblyCode in assemblyCodeList) {
        AssemblyOperator assemblyOperator = assemblyCode.Operator;
        object operand0 = assemblyCode[0],
               operand1 = assemblyCode[1],
               operand2 = assemblyCode[2];

        if (assemblyOperator == AssemblyOperator.define_value) {
          Sort sort = (Sort) operand0;

          if ((sort != Sort.String) && (operand1 is string)) {
            string name1 = (string) operand1;
               
            if (!name1.Contains(Symbol.SeparatorId)) {
              externSet.Add(name1);
            }
          }
        }
        else if ((assemblyOperator != AssemblyOperator.label) &&
                 (assemblyOperator != AssemblyOperator.comment)) {
          if (operand0 is string) {
            string name0 = (string) operand0;
               
            if (!name0.Contains(Symbol.SeparatorId)) {
              externSet.Add(name0);
            }
          }

          if (operand1 is string) {
            string name1 = (string) operand1;
               
            if (!name1.Contains(Symbol.SeparatorId)) {
              externSet.Add(name1);
            }
          }

          if (operand2 is string) {
            string name2 = (string) operand2;
               
            if (!name2.Contains(Symbol.SeparatorId)) {
              externSet.Add(name2);
            }
          }
        }

        string text = assemblyCode.ToString();
        if (text != null) {
          textList.Add(text);
        }
      }
    }

    private void TargetByteList(List<byte> byteList,
                       IDictionary<int,string> accessMap,
                       IDictionary<int,string> callMap, ISet<int> returnSet) {
      int lastSize = 0;
      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        AssemblyCode assemblyCode = m_assemblyCodeList[line];

        byteList.AddRange(assemblyCode.ByteList());
        int codeSize = byteList.Count - lastSize;
        lastSize = byteList.Count;
    
        AssemblyOperator objectOp = assemblyCode.Operator;
        object operand0 = assemblyCode[0],
               operand1 = assemblyCode[1],
               operand2 = assemblyCode[2];

        if ((assemblyCode.Operator != AssemblyOperator.label) &&
            (assemblyCode.Operator != AssemblyOperator.comment) &&
            (assemblyCode.Operator != AssemblyOperator.define_zero_sequence)){
           if (assemblyCode.Operator == AssemblyOperator.define_address) {
            string name = (string) assemblyCode[0];
            accessMap.Add(byteList.Count - TypeSize.PointerSize, name);
          }
          else if (assemblyCode.Operator == AssemblyOperator.define_value) {
            Sort sort = (Sort) assemblyCode[0];
            object value = assemblyCode[1];

            if (sort == Sort.Pointer) {
              if (value is string) {
                accessMap.Add(byteList.Count - TypeSize.PointerSize,
                              (string) value);
              }
              else if (value is StaticAddress) {
                StaticAddress staticAddress = (StaticAddress) value;
                accessMap.Add(byteList.Count - TypeSize.PointerSize,
                              staticAddress.UniqueName);
              }
            }
          }
          else if ((assemblyCode.Operator == AssemblyOperator.call) &&
                   (assemblyCode[0] is string)) {
            string calleeName = (string) assemblyCode[0];
            int address = byteList.Count - TypeSize.PointerSize;
            callMap.Add(address, calleeName);
          }
          else if (assemblyCode.Operator == AssemblyOperator.address_return) {
            int address = byteList.Count - TypeSize.PointerSize;
            returnSet.Add(address);
          }
          else if (operand0 is string) { // Add [g + 1], 2
            int size = 0;
            if (operand2 is BigInteger) {
               size = AssemblyCode.SizeOfValue((BigInteger) operand2,
                                               assemblyCode.Operator);
            }
          
            int address = byteList.Count - TypeSize.PointerSize - size;
            accessMap.Add(address, (string) operand0);
          }
          else if (operand1 is string) {
            if (operand2 is int) { // mov ax, [g + 1]
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) operand1);
            }
            else { // mov ax, g
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) operand1);
            }
          }
          else if (operand2 is string) { // Add [bp + 2], g
            int address = byteList.Count - TypeSize.PointerSize;
            accessMap.Add(address, (string) operand2);
          }
        }
      }
    }
  }
}


    /* e = x;     d => ax
       a = b / c; b => ax  
       d = e / f; d => ax
       f = b;     b => ?

       a -> b: yes
       b -> a: no
  
       a -> e: yes
       e -> a: no
  
       b -> d: no
       d -> b: yes
  
       b1: 3ax
       b2: 10, 11
       d1: 8
       d2: 9ax
  
       a: 6ax
       b: 3ax, 10, 11
       d: 8, 9ax
       e: 1, 2, 7ax

       1. mov ?, [x]
       2. mov [e], ?
  
       3. mov ax, [b]    ; b => ax
       4. mov ?, [c]
       5. div ?
       6. empty          ; a => ax
  
       7. empty          ; e => ax
       8. mov ?, [f]
       9. div ?          ; d => ax
  
       10. mov ?, [b]
       11. mov [f], ?

      // int 21       value
      // int ax       GetRegister
      // int [bx]     address
    
      // liveSet: stack  mov ax, [si + 2]; neg ax; int ax
      //          static mov ax, [20]; neg ax
      //        

      // !liveSet: stack  neg [si + 2]
      //           static neg [20]

     1 sbyte: al * 8 => ah:al
     2 bytes: ax * 16 => dx:ax
     4 bytes: eax * 32 => edx:eax

          // b = x;     b => ?
          // ...
          // d = e / f; e => ax
          // ...
          // a = b / c; b => ax

     1 sbyte: ax / 8, div: al, mod: ah
     2 bytes: dx:ax / 16, div: ax, mod: dx
     4 bytes: edx:eax / 32, div eax, mod edx */