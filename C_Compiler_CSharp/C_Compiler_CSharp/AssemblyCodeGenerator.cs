using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCodeGenerator {
    public IDictionary<Symbol,Track> m_trackMap =
      new Dictionary<Symbol,Track>();
    public List<AssemblyCode> m_assemblyCodeList;

    private int m_floatStackSize = 0;
    public const int FloatingStackMaxSize = 7;

    //public static string IntegralStorageName =
      //Symbol.SeparatorId + "IntegralStorage" + Symbol.NumberId;
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
      objectCodeGenerator.WindowsJumpInfo();
      objectCodeGenerator.WindowsByteList
                          (byteList, accessMap, callMap, returnSet);
    }

    public AssemblyCode AddAssemblyCode(AssemblyOperator objectOp,
                          object operand0 = null, object operand1 = null,
                          object operand2 = null, int size = 0) {
                                    /*if ((SymbolTable.CurrentFunction != null) &&
                                        SymbolTable.CurrentFunction.Name.Equals("mainc") &&
                                        (m_assemblyCodeList.Count == 32)) {
                                      int i = 1;
                                    }*/

      AssemblyCode assemblyCode =
        new AssemblyCode(objectOp, operand0, operand1, operand2, size);
      m_assemblyCodeList.Add(assemblyCode);
      return assemblyCode;
    }

    public static AssemblyCode AddAssemblyCode(List<AssemblyCode> list,
                    AssemblyOperator objectOp, object operand0 = null,
                    object operand1 = null, object operand2 = null,
                    int size = 0) {
      AssemblyCode assemblyCode =
        new AssemblyCode(objectOp, operand0, operand1, operand2, size);
      list.Add(assemblyCode);
      return assemblyCode;
    }

    public void AssemblyCodeList(List<MiddleCode> middleCodeList){
      for (int middleIndex = 0; middleIndex < middleCodeList.Count;
           ++middleIndex) {
        MiddleCode middleCode = middleCodeList[middleIndex];
        AddAssemblyCode(AssemblyOperator.new_middle_code, middleIndex);

        if (SymbolTable.CurrentFunction != null) {
          if ((middleCode.Operator != MiddleOperator.Initializer) &&
              (middleCode.Operator != MiddleOperator.InitializerZero)) {
            string labelText = SymbolTable.CurrentFunction.UniqueName;
            
            if (middleIndex > 0) {
              labelText += Symbol.SeparatorId + middleIndex;
            }

            AddAssemblyCode(AssemblyOperator.label, labelText);
          }
        }

        AddAssemblyCode(AssemblyOperator.comment, middleCode.ToString());

        /*if (middleCode.ToString().Contains("parameter s, offset 113")) {
          int i = 1;
        }

        if (middleCode.ToString().Contains("if arr >= Array_1000# goto 5")) {
          int i = 1;
        }*/

        switch (middleCode.Operator) {
          case MiddleOperator.PreCall:
            FunctionPreCall(middleCode);
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
            JumpToRegister(middleCode);
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
            IntegralRelation(middleCode);
            break;
          
          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract: {
              Symbol resultSymbol = (Symbol) middleCode[1];

              if (resultSymbol.Type.IsFloating()) {
                FloatingBinary(middleCode);
              }
              else {
                IntegralBinary(middleCode);
              }
            }
            break;

          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide:
          case MiddleOperator.SignedModulo:
          case MiddleOperator.UnsignedMultiply:
          case MiddleOperator.UnsignedDivide:
          case MiddleOperator.UnsignedModulo: {
              Symbol resultSymbol = (Symbol) middleCode[0];

              if (resultSymbol.Type.IsFloating()) {
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
              Symbol leftSymbol = (Symbol) middleCode[1];

              if (leftSymbol.Type.IsFloating()) {
                FloatingRelation(middleCode);
              }
              else {
                IntegralRelation(middleCode);
              }
            }
            break;
        
          case MiddleOperator.Case:
            Case(middleCode);
            break;

          case MiddleOperator.CaseEnd:
            CaseEnd(middleCode);
            break;

          case MiddleOperator.UnaryAdd:
          case MiddleOperator.UnarySubtract:
          case MiddleOperator.BitwiseNot: {
              Symbol resultSymbol = (Symbol) middleCode[0];

              if (resultSymbol.Type.IsFloating()) {
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
              Symbol paramSymbol = (Symbol) middleCode[1];

              if (paramSymbol.Type.IsFloating()) {
                FloatingParameter(middleCode);
              }
              else if (paramSymbol.Type.IsStructOrUnion()) {
                StructUnionParameter(middleCode, middleIndex);
              }
              else {
                IntegralParameter(middleCode);
              }
            }
            break;
          
          case MiddleOperator.GetReturnValue: {
              Symbol returnSymbol = (Symbol) middleCode[0];

              if (returnSymbol.Type.IsStructOrUnion()) {
                StructUnionGetReturnValue(middleCode);
              }
              else if (returnSymbol.Type.IsFloating()) {
                Assert.Error((++m_floatStackSize) <= FloatingStackMaxSize,
                             null, Message.Floating_stack_overflow);
              }
              else {
                IntegralGetReturnValue(middleCode);
              }
            }
            break;
          
          case MiddleOperator.SetReturnValue: {
              Symbol returnSymbol = (Symbol) middleCode[1];

              if (returnSymbol.Type.IsStructOrUnion()) {
                StructUnionSetReturnValue(middleCode);
              }
              else if (returnSymbol.Type.IsFloating()) {
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

    // Track Set Generation

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
            track.CurrentSize = (int) operand1;
          }
          else {
            track.CurrentSize = ((Track) operand1).CurrentSize;
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

    // Function Calls -----------------------------------------------------------------------

    public Register BaseRegister(Symbol symbol) {
      Assert.ErrorXXX((symbol == null) || symbol.IsAutoOrRegister());
    
      if (SymbolTable.CurrentFunction.Type.IsEllipse() &&
          (symbol != null) && !symbol.IsParameter()) {
        return AssemblyCode.EllipseRegister;
      }
      else {
        return AssemblyCode.FrameRegister;
      }
    }

    private int m_totalExtraSize = 0;
    private Stack<int> m_recordSizeStack = new Stack<int>();
    private Stack<IDictionary<Symbol, Track>> m_trackMapStack =
      new Stack<IDictionary<Symbol, Track>>();
    private Stack<IDictionary<Track,int>> m_registerMapStack =
      new Stack<IDictionary<Track,int>>();

    public void FunctionPreCall(MiddleCode middleCode) {
      Register baseRegister = BaseRegister(null);
      int recordSize = (int) middleCode[0], extraSize = 0;

      IDictionary<Track,int> registerMap = new Dictionary<Track,int>();
      foreach (KeyValuePair<Symbol, Track> pair in m_trackMap) {
        Track track = pair.Value;
        AddAssemblyCode(AssemblyOperator.mov, baseRegister,
                        recordSize + extraSize, track);
        registerMap.Add(track, recordSize + extraSize);
        Symbol symbol = pair.Key;
        extraSize += symbol.Type.Size();
      }

      for (int count = 0; count < m_floatStackSize; ++count) {
        AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister,
                        recordSize + extraSize);
        extraSize += 8;
      }

      m_recordSizeStack.Push(extraSize);
      m_totalExtraSize += extraSize;
      m_trackMapStack.Push(m_trackMap);
      m_registerMapStack.Push(registerMap);
      m_trackMap = new Dictionary<Symbol, Track>();
    }

    private bool m_returnFloating = false;

    public void FunctionCall(MiddleCode middleCode, int index) {
      int recordSize = ((int) middleCode[1]) +
                       m_totalExtraSize;
      Symbol calleeSymbol = (Symbol) middleCode[0];
      int extraSize = (int) middleCode[2];

      Type calleeType = calleeSymbol.Type.IsFunction()
                      ? calleeSymbol.Type : calleeSymbol.Type.PointerType;

      bool callerEllipse = SymbolTable.CurrentFunction.Type.IsEllipse(),
           calleeEllipse = calleeType.IsEllipse();

      Register frameRegister = callerEllipse ? AssemblyCode.EllipseRegister
                                             : AssemblyCode.FrameRegister;               

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

      if (callerEllipse) { // mov bp, di
        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister,
                        AssemblyCode.EllipseRegister);
      }
      else if (calleeEllipse) {
        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                        AssemblyCode.FrameRegister);
      }

      if (calleeEllipse && (extraSize > 0)) {
        AddAssemblyCode(AssemblyOperator.add, AssemblyCode.EllipseRegister,
                        (BigInteger) extraSize);
      }

      if (calleeSymbol.Type.IsFunction()) {
        AddAssemblyCode(AssemblyOperator.call, calleeSymbol.UniqueName);
        m_returnFloating = calleeSymbol.Type.ReturnType.IsFloating();
      }
      else {
        Track jumpTrack = LoadValueToRegister(calleeSymbol);
        AddAssemblyCode(AssemblyOperator.jmp, jumpTrack);
        m_returnFloating =
          calleeSymbol.Type.PointerType.ReturnType.IsFloating();
      }            
    }
  
    public void FunctionPostCall(MiddleCode middleCode) {
      Register baseRegister = BaseRegister(null);
      m_trackMap = m_trackMapStack.Pop();
      IDictionary<Track,int> postMap = m_registerMapStack.Pop();

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

        m_totalExtraSize -= recordSize;
      }
      else {
        m_totalExtraSize -= m_recordSizeStack.Pop();
      }
    }
	
    // -----------------------------------------------------------------------

    public Track LoadValueToRegister(Symbol symbol,
                                     Register? register = null) {
      if (register != null) {
        CheckRegister(symbol, register.Value);
      }

      Track track;
      if (m_trackMap.TryGetValue(symbol, out track)) {
        m_trackMap.Remove(symbol);

        if ((register != null) && (track.Register != null) &&
            !AssemblyCode.RegisterOverlap(register, track.Register)) {
          Track newTrack = new Track(symbol, register.Value);
          AddAssemblyCode(AssemblyOperator.set_track_size,
                          newTrack, track);
          AddAssemblyCode(AssemblyOperator.mov, newTrack, track);
          return newTrack;
        }
        else {
          if (register != null) {
            track.Register = register;
          }

          return track;
        }
      }
      else {
        track = new Track(symbol, register);
        //Assert.ErrorXXX(!(symbol.Type.IsFunction()));

        if (symbol.Value is BigInteger)  {
          AddAssemblyCode(AssemblyOperator.mov, track, symbol.Value);
        }
        else if (symbol.Type.IsArrayFunctionOrString() ||
                 (symbol.Value is StaticAddress)) {
          AddAssemblyCode(AssemblyOperator.mov, track,
                          Base(symbol));

          int offset = Offset(symbol);
          if (offset != 0) {
            AddAssemblyCode(AssemblyOperator.add, track,
                            (BigInteger) offset);
          }
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, track,
                          Base(symbol), Offset(symbol));
        }
        
        return track;
      }
    }

    public void CheckRegister(Symbol symbol, Register register) {
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

    public Track LoadAddressToRegister(Symbol symbol,
                                       Register? register = null) {
      if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        addressTrack.Pointer = true;
        return addressTrack;
      }
      else {
        Symbol addressSymbol = new Symbol(new Type(symbol.Type));
        Track addressTrack = new Track(addressSymbol, register);
        Assert.ErrorXXX((addressTrack.Register == null) ||
                        RegisterAllocator.PointerRegisterSetWithEllipse.
                        Contains(addressTrack.Register.Value));
        addressTrack.Pointer = true;
        Assert.ErrorXXX(!(symbol.Value is BigInteger));

        AddAssemblyCode(AssemblyOperator.mov, addressTrack, Base(symbol));

        int offset = Offset(symbol);
        if (offset != 0) {
          AddAssemblyCode(AssemblyOperator.add, addressTrack,
                          (BigInteger) offset);
        }

        return addressTrack;
      }
    }

    // Return, Exit, and Goto --------------------------------------------------------------------------

    public void Return(MiddleCode middleCode) {
      Assert.ErrorXXX(m_floatStackSize == 0);
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

    public void IntegralGetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol)middleCode[0];
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister,
                                    returnSymbol.Type.Size());
      CheckRegister(returnSymbol, returnRegister);
      Track returnTrack = new Track(returnSymbol, returnRegister);
      m_trackMap.Add(returnSymbol, returnTrack);
      AddAssemblyCode(AssemblyOperator.empty, returnTrack);
    }

    public void IntegralSetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol)middleCode[1];
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister,
                                    returnSymbol.Type.Size());
      LoadValueToRegister(returnSymbol, returnRegister);
      m_trackMap.Remove(returnSymbol);
    }

    public void Exit(MiddleCode middleCode) {
      Symbol exitSymbol = (Symbol) middleCode[0];

      if (Start.Linux) {
        if (exitSymbol != null) {
          LoadValueToRegister(exitSymbol, Register.rdi);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, Register.rdi,
                          BigInteger.Zero);
        }

        AddAssemblyCode(AssemblyOperator.mov, Register.rax,
                        (BigInteger)60); // 0x3C
        AddAssemblyCode(AssemblyOperator.syscall);
      }

      if (Start.Windows) {        
        if (exitSymbol != null) {
          LoadValueToRegister(exitSymbol, Register.al);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, Register.al,
                          BigInteger.Zero);
        }

        AddAssemblyCode(AssemblyOperator.mov, Register.ah,
                        (BigInteger) 76); // 0x4C
        AddAssemblyCode(AssemblyOperator.interrupt, (BigInteger) 33); // 0x21
      }
    }

    public void Goto(MiddleCode middleCode) {
      int jumpTarget = (int) middleCode[0];
      AddAssemblyCode(AssemblyOperator.jmp, null, null, jumpTarget);
    }
    
    // Load and Inspect Registers --------------------------------------------------------------------------
    
    private ISet<Track> m_syscallSet = new HashSet<Track>();

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
  
    public void CarryExpression(MiddleCode middleCode) {
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleCode.Operator];
      int jumpTarget = (int) middleCode[0];
      AddAssemblyCode(objectOperator, null, null, jumpTarget);
    }

    public void JumpToRegister(MiddleCode middleCode) {
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

    // Initialization

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
      Assert.ErrorXXX(size > 0);
      AddAssemblyCode(AssemblyOperator.define_zero_sequence, size);
    }

    // Base and Offset

    private object Base(Symbol symbol) {
      Assert.ErrorXXX(!(symbol.Value is BigInteger));

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
      Assert.ErrorXXX(!(symbol.Value is BigInteger));

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

    // Integral Assignment and Parameters -----------------------------------------------------------------------

    public void IntegralAssign(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0],
             assignSymbol = (Symbol) middleCode[1];
      IntegralAssign(resultSymbol, assignSymbol);
    }

    public void IntegralParameter(MiddleCode middleCode) {
      Type toType = (Type) middleCode[0];
      Symbol toSymbol = new Symbol(null, false, Storage.Auto, toType);
      Symbol fromSymbol = (Symbol) middleCode[1];
      int parameterOffset = (int) middleCode[2];
      toSymbol.Offset = m_totalExtraSize + parameterOffset;
      IntegralAssign(toSymbol, fromSymbol);
    }

    public void IntegralAssign(Symbol resultSymbol, Symbol assignSymbol) {
      Track resultTrack = null, assignTrack = null;
      m_trackMap.TryGetValue(resultSymbol, out resultTrack);
      m_trackMap.TryGetValue(assignSymbol, out assignTrack);
      int typeSize = assignSymbol.Type.SizeArray();

      if (resultSymbol.IsTemporary()) {
        Assert.ErrorXXX(resultSymbol.AddressSymbol == null);

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
          
          if (assignSymbol.Value is BigInteger) {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            assignSymbol.Value);
          }
          else if (assignSymbol.Type.IsArrayFunctionOrString() ||
                   (assignSymbol.Value is StaticAddress)) {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            Base(assignSymbol));

            int offset = Offset(assignSymbol);
            if (offset != 0) {
              AddAssemblyCode(AssemblyOperator.add, resultTrack,
                              (BigInteger) offset);
            }
          }
          else {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            Base(assignSymbol), Offset(assignSymbol));
          }
        }
      }
      else {
        IntegralBinary(MiddleOperator.Assign, resultSymbol,
                       resultSymbol, assignSymbol);
      }
    }

    // Integral Unary

    public static IDictionary<MiddleOperator,AssemblyOperator>
      m_middleToIntegralMap =
      new Dictionary<MiddleOperator,AssemblyOperator>() {
        {MiddleOperator.BitwiseNot, AssemblyOperator.not},
        {MiddleOperator.UnarySubtract, AssemblyOperator.neg},
        {MiddleOperator.SignedMultiply, AssemblyOperator.imul},
        {MiddleOperator.SignedDivide, AssemblyOperator.idiv},
        {MiddleOperator.SignedModulo, AssemblyOperator.idiv},
        {MiddleOperator.UnsignedMultiply, AssemblyOperator.mul},
        {MiddleOperator.UnsignedDivide, AssemblyOperator.div},
        {MiddleOperator.UnsignedModulo, AssemblyOperator.div},
        {MiddleOperator.Assign, AssemblyOperator.mov},
        {MiddleOperator.BinaryAdd, AssemblyOperator.add},
        {MiddleOperator.BinarySubtract, AssemblyOperator.sub},
        {MiddleOperator.BitwiseAnd, AssemblyOperator.and},
        {MiddleOperator.BitwiseOr, AssemblyOperator.or},
        {MiddleOperator.BitwiseXOr, AssemblyOperator.xor},
        {MiddleOperator.ShiftLeft, AssemblyOperator.shl},
        {MiddleOperator.ShiftRight, AssemblyOperator.shr},
        {MiddleOperator.Equal, AssemblyOperator.je},
        {MiddleOperator.NotEqual, AssemblyOperator.jne},
        {MiddleOperator.Carry, AssemblyOperator.jc},
        {MiddleOperator.NotCarry, AssemblyOperator.jnc},
        {MiddleOperator.Compare, AssemblyOperator.cmp},
        {MiddleOperator.SignedLessThan, AssemblyOperator.jl},
        {MiddleOperator.SignedLessThanEqual,AssemblyOperator.jle},
        {MiddleOperator.SignedGreaterThan, AssemblyOperator.jg},
        {MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.jge},
        {MiddleOperator.UnsignedLessThan, AssemblyOperator.jb},
        {MiddleOperator.UnsignedLessThanEqual, AssemblyOperator.jbe},
        {MiddleOperator.UnsignedGreaterThan, AssemblyOperator.ja},
        {MiddleOperator.UnsignedGreaterThanEqual, AssemblyOperator.jae}};

    public void IntegralUnary(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol)middleCode[0],
             unarySymbol = (Symbol)middleCode[1];
      IntegralUnary(middleCode.Operator, resultSymbol, unarySymbol);
    }

    public void IntegralUnary(MiddleOperator middleOperator,
                              Symbol resultSymbol, Symbol unarySymbol) {
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleOperator];
      int typeSize = unarySymbol.Type.SizeArray();

      Track unaryTrack = null;
      m_trackMap.TryGetValue(unarySymbol, out unaryTrack);

      if (unarySymbol.Value is BigInteger) {
        SymbolTable.StaticSet.Add(ConstantExpression.Value(unarySymbol));
        AddAssemblyCode(objectOperator, unarySymbol.UniqueName, 0, null, typeSize);
      }
      else if (resultSymbol == unarySymbol) {
        Assert.ErrorXXX(unaryTrack == null);
        if (middleOperator != MiddleOperator.UnaryAdd) {
          AddAssemblyCode(objectOperator, Base(unarySymbol),
                          Offset(unarySymbol), null, typeSize);
        }
      }
      else {
        unaryTrack = LoadValueToRegister(unarySymbol);

        if (middleOperator != MiddleOperator.UnaryAdd) {
          AddAssemblyCode(objectOperator, unaryTrack);
        }

        if (resultSymbol.IsTemporary()) {
          Assert.ErrorXXX(resultSymbol.AddressSymbol == null);
          m_trackMap.Add(resultSymbol, unaryTrack);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), unaryTrack);
        }

        m_trackMap.Remove(unarySymbol);
      }
    }

    // Integral Multiplication, Division, and Modulo

    public static IDictionary<int,Register> m_leftRegisterMap =
      new Dictionary<int,Register>() {{1, Register.al}, {2, Register.ax},
                                      {4, Register.eax}, {8, Register.rax}};

    public static IDictionary<int,Register> m_zeroRegisterMap =
      new Dictionary<int,Register>() {{1, Register.ah}, {2, Register.dx},
                                      {4, Register.edx}, {8, Register.rdx}};

    public static IDictionary<int,Register> m_productQuintentRegisterMap =
      new Dictionary<int,Register>() {{1, Register.al}, {2, Register.ax},
                                      {4, Register.eax}, {8, Register.rax}};

    public static IDictionary<int,Register> m_remainderRegisterMap =
      new Dictionary<int,Register>() {{1, Register.ah}, {2, Register.dx},
                                      {4, Register.edx}, {8, Register.rdx}};

    public void IntegralMultiply(MiddleCode middleCode) {
      Symbol leftSymbol = (Symbol) middleCode[1];
      int typeSize = leftSymbol.Type.SizeArray();
      Register leftRegister = m_leftRegisterMap[typeSize];
      Track leftTrack = LoadValueToRegister(leftSymbol, leftRegister);

      Register zeroRegister = m_zeroRegisterMap[typeSize];
      Track zeroTrack = new Track(leftSymbol, zeroRegister);
      AddAssemblyCode(AssemblyOperator.xor, zeroTrack, zeroTrack);

      Symbol rightSymbol = (Symbol) middleCode[2];
      IntegralUnary(middleCode.Operator, rightSymbol, rightSymbol);

      Register resultRegister, discardRegister;

      if ((middleCode.Operator == MiddleOperator.SignedModulo) ||
          (middleCode.Operator == MiddleOperator.UnsignedModulo)) {
        resultRegister = m_remainderRegisterMap[typeSize];
        discardRegister = m_productQuintentRegisterMap[typeSize];
      }
      else {
        resultRegister = m_productQuintentRegisterMap[typeSize];
        discardRegister = m_remainderRegisterMap[typeSize];
      }

      Symbol resultSymbol = (Symbol) middleCode[0];
      Track resultTrack = new Track(resultSymbol, resultRegister);

      if (resultSymbol.IsTemporary()) {
        Assert.ErrorXXX(resultSymbol.AddressSymbol == null);
        m_trackMap.Add(resultSymbol, resultTrack);
        AddAssemblyCode(AssemblyOperator.empty, resultTrack);
      }
      else {
        AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                        Offset(resultSymbol), resultTrack);
      }

      Track discaredTrack = new Track(resultSymbol, discardRegister);
      AddAssemblyCode(AssemblyOperator.empty, discaredTrack);
    }

    // Case

    public void Case(MiddleCode middleCode) {
      Symbol switchSymbol = (Symbol) middleCode[1];
      Track switchTrack = LoadValueToRegister(switchSymbol);
      Symbol caseSymbol = (Symbol) middleCode[2];
      BigInteger caseValue = (BigInteger) caseSymbol.Value; // cmp ax, 123
      AddAssemblyCode(AssemblyOperator.cmp, switchTrack, caseValue);
      int target = (int) middleCode[0];
      AddAssemblyCode(AssemblyOperator.je, null, null, target);
      m_trackMap.Add(switchSymbol, switchTrack);
    }
    
    public void CaseEnd(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode[0];
      m_trackMap.Remove(symbol);
    }

    // Integral Relation

    public void IntegralBinary(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol)middleCode[0],
             leftSymbol = (Symbol)middleCode[1],
             rightSymbol = (Symbol)middleCode[2];
      IntegralBinary(middleCode.Operator, resultSymbol,
                     leftSymbol, rightSymbol);
    }

    public void IntegralRelation(MiddleCode middleCode) {
      Symbol leftSymbol = (Symbol) middleCode[1],
             rightSymbol = (Symbol) middleCode[2];
      IntegralBinary(MiddleOperator.Compare, null, leftSymbol, rightSymbol);
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleCode.Operator];
      int target = (int) middleCode[0];
      AddAssemblyCode(objectOperator, null, null, target);
    }

    public void IntegralBinary(MiddleOperator middleOperator,
                               Symbol resultSymbol,  Symbol leftSymbol,
                                Symbol rightSymbol) {
      Track leftTrack = null, rightTrack = null;
      m_trackMap.TryGetValue(leftSymbol, out leftTrack);
      m_trackMap.TryGetValue(rightSymbol, out rightTrack);

      if ((leftTrack == null) &&
          (middleOperator != MiddleOperator.Assign) &&
          (((resultSymbol != null) && (resultSymbol != leftSymbol)) ||
            (leftSymbol.Type.IsArrayFunctionOrString() ||
            (leftSymbol.Value is StaticAddress)))) {
        leftTrack = LoadValueToRegister(leftSymbol);
      }

      if ((rightTrack == null) &&
          (middleOperator != MiddleOperator.BinaryAdd) &&
          (middleOperator != MiddleOperator.BinarySubtract) &&
          (middleOperator != MiddleOperator.Assign) &&
          ((rightSymbol.Type.IsArrayFunctionOrString() &&
           (rightSymbol.Offset != 0)) ||
           ((rightSymbol.Value is StaticAddress) &&
            (((StaticAddress) rightSymbol.Value).Offset != 0)))) {
        rightTrack = LoadValueToRegister(rightSymbol);
      }

/*      if ((leftTrack == null) &&
          (middleOperator != MiddleOperator.Assign) &&
          (((resultSymbol != null) && (resultSymbol != leftSymbol)) ||
            ((leftSymbol.Type.IsArrayFunctionOrString() &&
              leftSymbol.Offset != 0) ||
            ((leftSymbol.Value is StaticAddress) &&
             (((StaticAddress) leftSymbol.Value).Offset != 0))))) {
        leftTrack = LoadValueToRegister(leftSymbol);
      }*/

      if ((rightTrack == null) && (rightSymbol.Value is BigInteger) &&
          ((middleOperator != MiddleOperator.Assign) ||
           (leftTrack == null))) {
        BigInteger bigValue = (BigInteger) rightSymbol.Value;
        if (!((-2147483648 <= bigValue) && (bigValue <= 2147483647))) {
          rightTrack = LoadValueToRegister(rightSymbol);
        }
      }

      if (MiddleCode.IsShift(middleOperator)) {
        rightTrack =
          LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
      }

      int typeSize = leftSymbol.Type.Size();
      AssemblyOperator objectOperator = m_middleToIntegralMap[middleOperator];
      Assert.ErrorXXX(!(leftSymbol.Value is BigInteger));

      if (leftTrack != null) {
        if (rightTrack != null) {
          AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        }
        else if (rightSymbol.Value is BigInteger) {
          AddAssemblyCode(objectOperator, leftTrack, rightSymbol.Value);
        }
        else if (rightSymbol.Type.IsArrayFunctionOrString() ||
                 (rightSymbol.Value is StaticAddress)) {
          AddAssemblyCode(objectOperator, leftTrack, Base(rightSymbol));

          int rightOffset = Offset(rightSymbol);
          if (rightOffset != 0) {
            if (middleOperator == MiddleOperator.Assign) {
              AddAssemblyCode(AssemblyOperator.add, leftTrack,
                              (BigInteger) rightOffset);
            }
            else {
              AddAssemblyCode(objectOperator, leftTrack,
                              (BigInteger) rightOffset);
            }
          }
        }
        else {
          if (middleOperator == MiddleOperator.Assign) {
            leftTrack = new Track(resultSymbol);
          }

          AddAssemblyCode(objectOperator, leftTrack,
                          Base(rightSymbol), Offset(rightSymbol));
        }

        if (resultSymbol != null) {
          if (resultSymbol.IsTemporary()) {
            Assert.ErrorXXX(resultSymbol.AddressSymbol == null);
            m_trackMap.Add(resultSymbol, leftTrack);
          }
          else {
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), leftTrack);
          }
        }
      }
      /*else if ((leftSymbol.Type.IsArrayFunctionOrString() ||
               (leftSymbol.Value is StaticAddress))) {
        Assert.ErrorXXX(objectOperator == AssemblyOperator.cmp);

        if (rightTrack != null) {
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName, rightTrack);
        }
        else if (rightSymbol.Value is BigInteger) {
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName,
                          rightSymbol.Value, null, typeSize);
        }
        else if (rightSymbol.Type.IsArrayFunctionOrString() ||
                 (rightSymbol.Value is StaticAddress)) {
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName,
                          rightSymbol.UniqueName, null, typeSize);
        }
        else {
          rightTrack = LoadValueToRegister(rightSymbol);
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName,
                          Offset(leftSymbol), rightTrack);
        }
      }*/
      else {
        if (rightTrack != null) {
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), rightTrack);
        }
        else if (rightSymbol.Value is BigInteger) {
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), rightSymbol.Value, typeSize);
        }
        else if (rightSymbol.Type.IsArrayFunctionOrString() ||
                 (rightSymbol.Value is StaticAddress)) {
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), Base(rightSymbol), typeSize);

          int rightOffset = Offset(rightSymbol);
          if (rightOffset != 0) {
            if (middleOperator == MiddleOperator.Assign) {
              AddAssemblyCode(AssemblyOperator.add, Base(leftSymbol),
                              Offset(leftSymbol), (BigInteger) rightOffset,
                              typeSize);
            }
            else {
              AddAssemblyCode(objectOperator, Base(leftSymbol),
                              Offset(leftSymbol), (BigInteger) rightOffset,
                              typeSize);
            }
          }
        }
        else {
          rightTrack = LoadValueToRegister(rightSymbol);
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), rightTrack);
        }
      }

      m_trackMap.Remove(leftSymbol);
      m_trackMap.Remove(rightSymbol);
    }

    // Address

    public void Address(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0],
             addressSymbol = (Symbol) middleCode[1];
      Track track = LoadAddressToRegister(addressSymbol);
      m_trackMap.Add(resultSymbol, track);
      m_trackMap.Remove(addressSymbol);
    }

    public void IntegralDereference(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0];
      Assert.ErrorXXX(resultSymbol.AddressSymbol != null);
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);
    }

    public void FloatingDereference(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0];
      Assert.ErrorXXX(resultSymbol.AddressSymbol != null);
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);
    }

    // Floating Binary

    public static IDictionary<MiddleOperator, AssemblyOperator>
      m_middleToFloatingMap =
        new Dictionary<MiddleOperator, AssemblyOperator>() {
          {MiddleOperator.UnarySubtract, AssemblyOperator.fchs},
          {MiddleOperator.BinaryAdd, AssemblyOperator.fadd},
          {MiddleOperator.BinarySubtract, AssemblyOperator.fsub},
          {MiddleOperator.SignedMultiply, AssemblyOperator.fmul},
          {MiddleOperator.SignedDivide, AssemblyOperator.fdiv},
          {MiddleOperator.Equal, AssemblyOperator.je},
          {MiddleOperator.NotEqual, AssemblyOperator.jne},
          {MiddleOperator.SignedLessThan, AssemblyOperator.ja},
          {MiddleOperator.SignedLessThanEqual, AssemblyOperator.jae},
          {MiddleOperator.SignedGreaterThan, AssemblyOperator.jb},
          {MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.jbe}
        };

    public void FloatingBinary(MiddleCode middleCode) {
      Assert.ErrorXXX((--m_floatStackSize) >= 0);
      AddAssemblyCode(m_middleToFloatingMap[middleCode.Operator]);
    }

    public void FloatingUnary(MiddleCode middleCode) {
      AddAssemblyCode(m_middleToFloatingMap[middleCode.Operator]);
    }

    public void FloatingParameter(MiddleCode middleCode) {
      Type paramType = (Type) middleCode[0];
      Symbol paramSymbol = new Symbol(paramType);
      int paramOffset = (int) middleCode[2];
      paramSymbol.Offset = m_totalExtraSize + paramOffset;
      TopPopSymbol(paramSymbol, TopOrPop.Pop);
    }

    // Floating Relation

    public void FloatingRelation(MiddleCode middleCode) {
      Assert.ErrorXXX((m_floatStackSize -= 2) >= 0);
      int target = (int)middleCode[0];
      AddAssemblyCode(AssemblyOperator.fcompp);
      AddAssemblyCode(AssemblyOperator.fstsw, Register.ax);
      AddAssemblyCode(AssemblyOperator.sahf);
      AssemblyOperator objectOperator =
        m_middleToFloatingMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, target);
    }

    // Floating Push and Pop

    public static IDictionary<Sort, AssemblyOperator> m_floatPushMap =
      new Dictionary<Sort, AssemblyOperator>() {
        {Sort.Signed_Int, AssemblyOperator.fild_word},
        {Sort.Unsigned_Int, AssemblyOperator.fild_word},
        {Sort.Signed_Long_Int, AssemblyOperator.fild_dword},
        {Sort.Unsigned_Long_Int, AssemblyOperator.fild_dword},
        {Sort.Float, AssemblyOperator.fld_dword},
        {Sort.Double, AssemblyOperator.fld_qword},
        {Sort.Long_Double, AssemblyOperator.fld_qword}
      };

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
        SymbolTable.StaticSet.Add(ConstantExpression.Value(symbol));
        AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
      }
      else if (m_trackMap.TryGetValue(symbol, out track)) {
        m_trackMap.Remove(symbol);
        string containerName = AddStaticContainer(symbol.Type);
        AddAssemblyCode(AssemblyOperator.mov, containerName, 0, track);
        AddAssemblyCode(objectOperator, containerName, 0);
      }
      else if (symbol.Type.IsArrayFunctionOrString()) {
        string containerName = AddStaticContainer(symbol.Type);
        AddAssemblyCode(AssemblyOperator.mov, containerName, 0,
                        Base(symbol), TypeSize.PointerSize);

        int offset = Offset(symbol);
        if (offset != 0) {
          AddAssemblyCode(AssemblyOperator.add, containerName, 0,
                          (BigInteger) offset, TypeSize.PointerSize);
        }

        AddAssemblyCode(objectOperator, containerName, 0);
      }
      else {
        AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
      }
    }

    private static string AddStaticContainer(Type type) {
      string containerName = "container" + type.Size() +
                             "bytes" + Symbol.NumberId;
      SymbolTable.StaticSet.Add(ConstantExpression.Value(containerName,
                                                         type, null));
      return containerName;
    }

    public static IDictionary<Sort, AssemblyOperator> m_floatPopMap =
      new Dictionary<Sort, AssemblyOperator>() {
        {Sort.Signed_Int, AssemblyOperator.fistp_word},
        {Sort.Unsigned_Int, AssemblyOperator.fistp_word},
        {Sort.Pointer, AssemblyOperator.fistp_word},
        {Sort.Signed_Long_Int, AssemblyOperator.fistp_dword},
        {Sort.Unsigned_Long_Int, AssemblyOperator.fistp_dword},
        {Sort.Float, AssemblyOperator.fstp_dword},
        {Sort.Double, AssemblyOperator.fstp_qword},
        {Sort.Long_Double, AssemblyOperator.fstp_qword}
      };

    public static IDictionary<Sort, AssemblyOperator> m_floatTopMap =
      new Dictionary<Sort, AssemblyOperator>() {
        {Sort.Signed_Int, AssemblyOperator.fist_word},
        {Sort.Unsigned_Int, AssemblyOperator.fist_word},
        {Sort.Pointer, AssemblyOperator.fist_word},
        {Sort.Signed_Long_Int, AssemblyOperator.fist_dword},
        {Sort.Unsigned_Long_Int, AssemblyOperator.fist_dword},
        {Sort.Float, AssemblyOperator.fst_dword},
        {Sort.Double, AssemblyOperator.fst_qword},
        {Sort.Long_Double, AssemblyOperator.fst_qword}
      };

    public enum TopOrPop {Top, Pop};

    public void PopEmpty() {
      string containerName = AddStaticContainer(Type.LongDoubleType);
      AddAssemblyCode(AssemblyOperator.fistp_word, containerName, 0);
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
        string containerName = AddStaticContainer(symbol.Type);
        AddAssemblyCode(objectOperator, containerName, 0);
        Track track = new Track(symbol);
        AddAssemblyCode(AssemblyOperator.mov, track, containerName, 0);
        m_trackMap.Add(symbol, track);
      }
      else {
        AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
      }
    }

    // Type Conversion

    public void IntegralToIntegral(MiddleCode middleCode, int index) {
      Symbol toSymbol = (Symbol) middleCode[0],
             fromSymbol = (Symbol) middleCode[1];

      Type toType = toSymbol.Type, fromType = fromSymbol.Type;
      int toSize = toType.SizeArray(), fromSize = fromType.SizeArray();

      Track fromTrack = LoadValueToRegister(fromSymbol);

      //Assert.ErrorXXX(fromSize != toSize);

      if (fromSize != toSize) {
        AddAssemblyCode(AssemblyOperator.set_track_size, fromTrack, toSize);

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

    // Struct and Union

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

      int paramOffset = (int) middleCode[2];
      targetSymbol.Offset = m_totalExtraSize + paramOffset;

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

    private void MemoryCopy(Track targetAddressTrack,
                            Track sourceAddressTrack, int size, int index) {
      Type countType = (size < 256) ? Type.UnsignedCharType
                                    : Type.UnsignedIntegerType;
      Track countTrack = new Track(countType),
            valueTrack = new Track(Type.UnsignedCharType);

      AddAssemblyCode(AssemblyOperator.mov, countTrack, (BigInteger) size);
      int labelIndex = m_labelCount++;
      string labelText = AssemblyCode.MakeMemoryLabel(labelIndex);
      AddAssemblyCode(AssemblyOperator.label, labelText);
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

    // Initialization Code

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

      if (Start.Linux) {
        List<string> textList = new List<string>();
        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList, externSet);
        SymbolTable.StaticSet.Add(new StaticSymbolLinux
          (StaticSymbolLinux.TextOrData.Text,
           AssemblyCodeGenerator.InitializerName, textList, externSet));
      }

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
    }

    // Command Line Arguments

    public static void ArgumentCodeList() {
      if (Start.Linux) {
        ArgumentCodeListLinux();
      }
      else {
        ArgumentCodeListWindows();
      }
    }

    public static void ArgumentCodeListLinux() {
      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                      "Initializerialize Command Line Arguments");
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.pop, Register.rbx);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                      Register.rax, Register.rbx);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                      Register.rdx, Register.rbp);

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.label, "$args$loop");
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
      AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList,
                              externSet);
      SymbolTable.StaticSet.
      Add(new StaticSymbolLinux(StaticSymbolLinux.TextOrData.Text,
                  AssemblyCodeGenerator.ArgsName, textList, externSet));
    }

    public static void ArgumentCodeListWindows() {
      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();

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
        
    // Jump Info

    /*private void WindowsJumpInfo() {
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
    }*/

    private void WindowsJumpInfo() {
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

      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {        
        AssemblyCode assemblyCode = m_assemblyCodeList[line];

        if (assemblyCode.Operator == AssemblyOperator.address_return) {
          int middleAddress = (int) ((BigInteger) assemblyCode[2]);
          int assemblyAddress = middleToAssemblyMap[middleAddress];
          int byteAddress = assemblyToByteMap[assemblyAddress];
          assemblyCode[2] = (BigInteger) byteAddress;
        }
      }
    }
    
/*
      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
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
*/
    // Text List
    
    public static void LinuxTextList(IList<AssemblyCode> assemblyCodeList,
                                     IList<string> textList,
                                     ISet<string> externSet) {
//      foreach (AssemblyCode assemblyCode in assemblyCodeList) {
      for (int index = 0; index < assemblyCodeList.Count; ++index) {
        AssemblyCode assemblyCode = assemblyCodeList[index];
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

    // Target Byte List

    private void WindowsByteList(List<byte> byteList,
                                 IDictionary<int,string> accessMap,
                                 IDictionary<int,string> callMap,
                                 ISet<int> returnSet) {
      foreach (AssemblyCode assemblyCode in m_assemblyCodeList) {
        byteList.AddRange(assemblyCode.ByteList());

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
          else if (assemblyCode[0] is string) { // Add [g + 1], 2
            if (assemblyCode[2] is BigInteger) {
              int size = AssemblyCode.SizeOfValue((BigInteger)assemblyCode[2],
                                                  assemblyCode.Operator);
              int address = byteList.Count - TypeSize.PointerSize - size;
              accessMap.Add(address, (string) assemblyCode[0]);
            }
            else {
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) assemblyCode[0]);
            }
          }
          else if (assemblyCode[1] is string) { // mov ax, [g + 1]; mov ax, g
            int address = byteList.Count - TypeSize.PointerSize;
            accessMap.Add(address, (string) assemblyCode[1]);
          }
          else if (assemblyCode[2] is string) { // Add [bp + 2], g
            int address = byteList.Count - TypeSize.PointerSize;
            accessMap.Add(address, (string) assemblyCode[2]);
          }
        }
      }
    }
  }
}

/*
          else if (assemblyCode[1] is string) {
            if (assemblyCode[2] is int) { // mov ax, [g + 1]
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) assemblyCode[1]);
            }
            else { // mov ax, g
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) assemblyCode[1]);
            }
          }
*/

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