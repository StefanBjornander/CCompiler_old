using System;
using System.Linq;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCodeGenerator {
    public IDictionary<Symbol, Track> m_trackMap = new Dictionary<Symbol, Track>();
    public ISet<Track> m_trackSet = new HashSet<Track>(); // ListSetXXX
    private IDictionary<int, int> m_middleToAssemblyMap;
    private IDictionary<int, int> m_assemblyToByteMap = new Dictionary<int, int>();
    public List<AssemblyCode> m_assemblyCodeList;
    public const int FloatingStackMaxSize = 7;

    private int m_floatStackSize = 0, m_totalRecordSize = 0;
    private bool m_returnFloat = false;
    private Stack<int> m_recordSizeStack = new Stack<int>();
    private Stack<IDictionary<Symbol, Track>> m_trackMapStack = new Stack<IDictionary<Symbol, Track>>();
    private Stack<IDictionary<Track, int>> m_postMapStack = new Stack<IDictionary<Track, int>>();

    public static string IntegralStorageName = Symbol.SeparatorId + "IntegralStorage" + Symbol.NumberId;
    public static string PathName = Symbol.SeparatorId + "Path";
    public static string PathText = "";

    public AssemblyCodeGenerator(List<AssemblyCode> assemblyCodeList, IDictionary<int, int> middleToAssemblyMap) {
      m_assemblyCodeList = assemblyCodeList;
      m_middleToAssemblyMap = middleToAssemblyMap;
    }

    private void GenerateRegisterAllocation() {
      new RegisterAllocator(m_trackSet, m_assemblyCodeList);
    }

    public static void GenerateAssembly(List<MiddleCode> middleCodeList, List<AssemblyCode> assemblyCodeList,
                                        IDictionary<int, int> middleToAssemblyMap) {
      AssemblyCodeGenerator objectCodeGenerator = new AssemblyCodeGenerator(assemblyCodeList, middleToAssemblyMap);
      //Assert.Error(objectCodeGenerator.m_assemblyCodeList.Count == 0);
      objectCodeGenerator.GenerateAssemblyCodeList(middleCodeList);
      objectCodeGenerator.GenerateTrackSet();
      objectCodeGenerator.GenerateRegisterAllocation();

      /*      AssemblyCodeGenerator objectCodeGenerator = new AssemblyCodeGenerator(assemblyCodeList);
            objectCodeGenerator.GenerateJumpInfo();
            objectCodeGenerator.GenerateTargetByteList(byteList, accessMap, callMap, returnSet);
            return objectCodeGenerator.m_assemblyCodeList;
            /*
            new RegisterAllocator(m_trackSet, m_assemblyCodeList);

            if (Start.Windows) {
              GenerateJumpInfo();
              GenerateTargetByteList(functionStaticSymbol.ByteList, functionStaticSymbol.AccessMap,
                                     functionStaticSymbol.CallMap, functionStaticSymbol.ReturnSet);
            }
      

            if (Start.Linux) {
              List<string> assemblyTextList = new List<string>();
              foreach (ObjectCode objectCode in m_assemblyCodeList) {
                assemblyTextList.Add(objectCode.ToString());
                //Start.AssemblyStream.WriteLine(objectCode.ToString());
              }
            }*/
    }

    public static void GenerateTargetWindows(List<AssemblyCode> assemblyCodeList, IDictionary<int, int> middleToAssemblyMap,
                                             List<sbyte> byteList, IDictionary<int, string> accessMap,
                                             IDictionary<int, string> callMap, ISet<int> returnSet) {
      AssemblyCodeGenerator objectCodeGenerator = new AssemblyCodeGenerator(assemblyCodeList, middleToAssemblyMap);
      objectCodeGenerator.GenerateJumpInfo();
      objectCodeGenerator.GenerateTargetByteList(byteList, accessMap, callMap, returnSet);
    }

    public void AddAssemblyCode(AssemblyCode objectCode) {
      m_assemblyCodeList.Add(objectCode);
    }

    public void AddAssemblyCode(AssemblyOperator objectOp) {
      AddAssemblyCode(objectOp, null, null, null);
    }

    public void AddAssemblyCode(AssemblyOperator objectOp, object operand0) {
      AddAssemblyCode(objectOp, operand0, null, null);
    }

    public void AddAssemblyCode(AssemblyOperator objectOp, object operand0,
                              object operand1) {
      AddAssemblyCode(objectOp, operand0, operand1, null);
    }

    public void AddAssemblyCode(AssemblyOperator objectOp, object operand0,
                              object operand1, object operand2) {
      m_assemblyCodeList.Add(new AssemblyCode(objectOp, operand0, operand1, operand2));
    }

    public static void AddAssemblyCode(List<AssemblyCode> list, AssemblyOperator objectOp)
    {
      AddAssemblyCode(list, objectOp, null, null, null);
    }

    public static void AddAssemblyCode(List<AssemblyCode> list, AssemblyOperator objectOp, object operand0)
    {
      AddAssemblyCode(list, objectOp, operand0, null, null);
    }

    public static void AddAssemblyCode(List<AssemblyCode> list, AssemblyOperator objectOp,
                                object operand0, object operand1)
    {
      AddAssemblyCode(list, objectOp, operand0, operand1, null);
    }

    public static void AddAssemblyCode(List<AssemblyCode> list, AssemblyOperator objectOp, object operand0,
                                       object operand1, object operand2)
    {
      list.Add(new AssemblyCode(objectOp, operand0, operand1, operand2));
    }

    public void GenerateAssemblyCodeList(List<MiddleCode> middleCodeList)
    {
      /*if (m_assemblyCodeList == null) {
        m_assemblyCodeList = new List<AssemblyCode>();
      }*/

      for (int index = 0; index < middleCodeList.Count; ++index)
      {
        MiddleCode middleCode = middleCodeList[index];

        if (m_middleToAssemblyMap != null)
        {
          m_middleToAssemblyMap.Add(index, m_assemblyCodeList.Count);
        }

        if (SymbolTable.CurrentFunction != null)
        {
          string label = SymbolTable.CurrentFunction.UniqueName +
                 ((index > 0) ? (Symbol.SeparatorId + index) : "");
          AddAssemblyCode(AssemblyOperator.label, label, middleCode.ToString(), null);
        }

        switch (middleCode.Operator)
        {
          case MiddleOperator.CallHeader:
            GenerateCallHeader(middleCode);
            break;

          case MiddleOperator.Call:
            GenerateFunctionCall(middleCode, index);
            break;

          case MiddleOperator.PostCall:
            GenerateFunctionPostCall(middleCode);
            break;

          case MiddleOperator.Return:
            GenerateReturn(middleCode);
            break;

          case MiddleOperator.Exit:
            GenerateExit(middleCode);
            break;

          case MiddleOperator.Goto:
            GenerateGoto(middleCode);
            break;

          case MiddleOperator.AssignRegister:
            GenerateLoadToRegister(middleCode);
            break;

          /*case MiddleOperator.SaveFromRegister:
            GenerateSaveFromRegister(middleCode);
            break;*/

          case MiddleOperator.InspectRegister:
            GenerateInspectRegister(middleCode);
            break;

          /*case MiddleOperator.InspectFlagbyte:
            GenerateFlagbyte(middleCode);
            break;*/

          case MiddleOperator.JumpRegister:
            GenerateJumpRegister(middleCode);
            break;

          case MiddleOperator.CarryFlag:
            GenerateCarryFlag(middleCode);
            break;

          case MiddleOperator.Interrupt:
            GenerateInterrupt(middleCode);
            break;

          case MiddleOperator.Init:
            {
              Sort sort = (Sort)middleCode.GetOperand(0);
              object value = middleCode.GetOperand(1);
              GenerateInit(sort, value);
            }
            break;

          case MiddleOperator.InitZero:
            {
              int size = (int)middleCode.GetOperand(0);
              GenerateInitZero(size);
            }
            break;

          case MiddleOperator.Assign:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(1);

              if (symbol.Type.IsStructOrUnion())
              {
                GenerateStructUnionAssign(middleCode, index);
              }
              else
              {
                GenerateIntegralAssign(middleCode);
              }
            }
            break;

          case MiddleOperator.BitwiseAnd:
          case MiddleOperator.BitwiseIOr:
          case MiddleOperator.BitwiseXOr:
          case MiddleOperator.ShiftLeft:
          case MiddleOperator.ShiftRight:
            GenerateIntegralAdditionBitwiseShift(middleCode);
            break;

          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract:
            {
              Symbol resultSymbol = (Symbol)middleCode.GetOperand(1);

              if (resultSymbol.Type.IsFloating())
              {
                GenerateFloatingBinary(middleCode);
              }
              else
              {
                GenerateIntegralAdditionBitwiseShift(middleCode);
              }
            }
            break;

          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide:
          case MiddleOperator.SignedModulo:
          case MiddleOperator.UnsignedMultiply:
          case MiddleOperator.UnsignedDivide:
          case MiddleOperator.UnsignedModulo:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(1);

              if (symbol.Type.IsFloating())
              {
                GenerateFloatingBinary(middleCode);
              }
              else
              {
                GenerateIntegralMultiply(middleCode);
              }
            }
            break;

          case MiddleOperator.Carry:
          case MiddleOperator.NotCarry:
            GenerateCarryExpression(middleCode);
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
          case MiddleOperator.UnsignedGreaterThanEqual:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(1);

              if (symbol.Type.IsFloating())
              {
                GenerateFloatingRelation(middleCode, index);
              }
              else
              {
                GenerateIntegralRelation(middleCode, index);
              }
            }
            break;

          case MiddleOperator.Increment:
          case MiddleOperator.Decrement:
            GenerateIntegralIncrementDecrement(middleCode);
            break;

          case MiddleOperator.UnaryAdd:
          case MiddleOperator.UnarySubtract:
          case MiddleOperator.BitwiseNot:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(1);

              if (symbol.Type.IsFloating())
              {
                GenerateFloatingUnary(middleCode);
              }
              else
              {
                GenerateIntegralUnary(middleCode);
              }
            }
            break;

          case MiddleOperator.Address:
            GenerateAddress(middleCode);
            break;

          case MiddleOperator.Deref:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(1);

              if (symbol.Type.IsFloating())
              {
                GenerateFloatingDeref(middleCode);
              }
              else
              {
                GenerateIntegralDeref(middleCode);
              }
            }
            break;

          case MiddleOperator.DecreaseStack:
            Assert.Error((--m_floatStackSize) >= 0);
            break;

          /*case MiddleOperator.CheckTrackMapFloatStack:
            Assert.Error((m_trackMap.Count == 0) && (m_floatStackSize == 0));
            break;*/

          case MiddleOperator.PushZero:
            PushSymbol(new Symbol(Type.DoubleType, (decimal)0));
            break;

          case MiddleOperator.PushOne:
            PushSymbol(new Symbol(Type.DoubleType, (decimal)1));
            break;

          case MiddleOperator.PushFloat:
            PushSymbol((Symbol)middleCode.GetOperand(0));
            break;

          case MiddleOperator.TopFloat:
            TopPopSymbol((Symbol)middleCode.GetOperand(0), TopOrPop.Top);
            break;

          case MiddleOperator.PopFloat:
            TopPopSymbol((Symbol)middleCode.GetOperand(0), TopOrPop.Pop);
            break;

          case MiddleOperator.PopEmpty:
            PopEmpty();
            break;

          case MiddleOperator.IntegralToIntegral:
            GenerateIntegralToIntegral(middleCode, index);
            break;

          case MiddleOperator.IntegralToFloating:
            GenerateIntegralToFloating(middleCode);
            break;

          case MiddleOperator.FloatingToIntegral:
            GenerateFloatingToIntegral(middleCode);
            break;

          case MiddleOperator.Parameter:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(1);

              if (symbol.Type.IsFloating())
              {
                GenerateFloatingParameter(middleCode);
              }
              else if (symbol.Type.IsStructOrUnion())
              {
                GenerateStructUnionParameter(middleCode, index);
              }
              else
              {
                GenerateIntegralParameter(middleCode);
              }
            }
            break;

          case MiddleOperator.GetReturnValue:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(0);

              if (symbol.Type.IsStructOrUnion())
              {
                GenerateStructUnionGetReturnValue(middleCode);
              }
              else if (symbol.Type.IsFloating())
              {
                Assert.Error((++m_floatStackSize) <= FloatingStackMaxSize,
                             null, Message.Floating_stack_overflow);
              }
              else
              {
                GenerateIntegralGetReturnValue(middleCode);
              }
            }
            break;

          case MiddleOperator.SetReturnValue:
            {
              Symbol symbol = (Symbol)middleCode.GetOperand(1);

              if (symbol.Type.IsStructOrUnion())
              {
                GenerateStructUnionSetReturnValue(middleCode);
              }
              else if (symbol.Type.IsFloating())
              {
                Assert.Error((--m_floatStackSize) == 0);
              }
              else
              {
                GenerateIntegralSetReturnValue(middleCode);
              }
            }
            break;

          case MiddleOperator.Dot:
          case MiddleOperator.FunctionEnd:
          case MiddleOperator.Empty:
            break;

          case MiddleOperator.SystemInit:
            GenerateSystemInit(middleCode);
            break;

          case MiddleOperator.SystemParameter:
            GenerateSystemParameter(middleCode);
            break;

          case MiddleOperator.SystemCall:
            GenerateSystemCall(middleCode);
            break;

          default:
            Assert.Error(Enum.GetName(typeof(MiddleOperator), middleCode.Operator),
                         Message.Object_code_switch_default);
            break;
        }
      }

      //GenerateTrackSet();
    }

    private void GenerateTrackSet()
    {
      for (int index = 0; index < m_assemblyCodeList.Count; ++index)
      {
        AssemblyCode objectCode = m_assemblyCodeList[index];

        object operand0 = objectCode.GetOperand(0),
               operand1 = objectCode.GetOperand(1),
               operand2 = objectCode.GetOperand(2);

        if (objectCode.Operator == AssemblyOperator.set_track_size)
        {
          Track track = (Track) operand0;

          if (operand1 is int) {
            int size = (int)operand1;
            track.Size = size;
            objectCode.Operator = AssemblyOperator.empty;
          }
          else
          {
            int size = ((Track)operand1).Size;
            track.Size = size;
            objectCode.Operator = AssemblyOperator.empty;
          }
        }
        else
        {
          CheckTrack(operand0, 0, index);
          CheckTrack(operand1, 1, index);
          CheckTrack(operand2, 2, index);
        }
      }
    }

    private void CheckTrack(object operand, int position, int index)
    {
      if (operand is Track)
      {
        Track track = (Track)operand;

        if (!m_trackSet.Contains(track))
        {
          m_trackSet.Add(track);
        }

        track.AddCode(position, index);
      }
    }

    private Track SetPointer(Track track, Symbol symbol)
    {
      if (track.Pointer)
      {
        return track;
      }

      if (track.Register == null)
      {
        track.Pointer = true;
        return track;
      }

      if (!RegisterAllocator.m_pointerRegisterSetWithoutEllipse.Contains(track.Register.Value))
      {
        Track newTrack = new Track(symbol);
        m_trackSet.Add(newTrack);
        AddAssemblyCode(AssemblyOperator.mov, newTrack, track);

        if (m_trackMap.ContainsKey(symbol))
        {
          m_trackMap[symbol] = newTrack;
        }

        return newTrack;
      }
      else
      {
        return track;
      }
    }

    public Register BaseRegister(Symbol symbol)
    {
      Assert.Error((symbol == null) || symbol.IsAutoOrRegister());

      if (SymbolTable.CurrentFunction.Type.IsEllipse() && ((symbol == null) || !symbol.IsParameter()))
      {
        return AssemblyCode.EllipseRegister;
      }
      else
      {
        return AssemblyCode.FrameRegister;
      }
    }

    public void GenerateCallHeader(MiddleCode middleCode)
    {
      ISet<Symbol> integralSet = (ISet<Symbol>)middleCode.GetOperand(1);

      if (!integralSet.SequenceEqual(m_trackMap.Keys))
      {
        foreach (Symbol symbol in integralSet)
        {
          Console.Out.Write(symbol + " ");
        }
        Console.Out.WriteLine();
        foreach (Symbol symbol in m_trackMap.Keys)
        {
          Console.Out.Write(symbol + " ");
        }
        Console.Out.WriteLine();
      }

      Assert.Error(integralSet.SequenceEqual(m_trackMap.Keys), Message.Integral_set_does_not_equals_track_map_key_set);
      int stackSize = (int)middleCode.GetOperand(2);
      Assert.Error(stackSize == m_floatStackSize, Message.Stack_size);

      Register baseRegister = BaseRegister(null);
      int recordOffset = (int)middleCode.GetOperand(0), recordSize = 0;

      IDictionary<Track, int> postMap = new Dictionary<Track, int>();
      foreach (KeyValuePair<Symbol, Track> pair in m_trackMap)
      {
        Track track = pair.Value;
        AddAssemblyCode(AssemblyOperator.mov, baseRegister, recordOffset + recordSize, track);
        postMap[track] = recordOffset + recordSize;
        Symbol symbol = pair.Key;
        recordSize += symbol.Type.Size();
      }

      for (int count = 0; count < m_floatStackSize; ++count)
      {
        AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister, recordOffset + recordSize);
        recordSize += Type.QuarterWordSize;
      }

      m_recordSizeStack.Push(recordSize);
      m_totalRecordSize += recordSize;
      m_trackMapStack.Push(m_trackMap);
      m_postMapStack.Push(postMap);
      m_trackMap = new Dictionary<Symbol, Track>();
    }

    public void GenerateFunctionCall(MiddleCode middleCode, int index)
    {
      int recordSize = ((int)middleCode.GetOperand(1)) + m_totalRecordSize;
      Symbol calleeSymbol = (Symbol)middleCode.GetOperand(0);
      int extraSize = (int)middleCode.GetOperand(2);

      Type calleeType = calleeSymbol.Type.IsFunction() ? calleeSymbol.Type :
                        calleeSymbol.Type.PointerType;

      bool callerEllipse = SymbolTable.CurrentFunction.Type.IsEllipse(),
           calleeEllipse = calleeType.IsEllipse();
      Track jumpTrack = null;

      if (callerEllipse) {
        AddAssemblyCode(AssemblyOperator.address_return, AssemblyCode.EllipseRegister,
                      recordSize + SymbolTable.ReturnAddressOffset, index + 1);

        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                             recordSize + SymbolTable.RegularFrameOffset,
                             AssemblyCode.FrameRegister);
        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                             recordSize + SymbolTable.EllipseFrameOffset,
                             AssemblyCode.EllipseRegister);

        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister, AssemblyCode.EllipseRegister, null);

        if (!calleeSymbol.Type.IsFunction()) {
          jumpTrack = LoadValueToRegister(calleeSymbol);
        }

        AddAssemblyCode(AssemblyOperator.add, AssemblyCode.FrameRegister, recordSize, null);

        if (calleeEllipse) {
          AddAssemblyCode(AssemblyOperator.add, AssemblyCode.EllipseRegister,
                                         recordSize + extraSize, null);
        }
      }
      else
      {
        AddAssemblyCode(AssemblyOperator.address_return, AssemblyCode.FrameRegister,
                      recordSize + SymbolTable.ReturnAddressOffset, index + 1);

        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister,
                                       recordSize + SymbolTable.RegularFrameOffset,
                                       AssemblyCode.FrameRegister);

        if (!calleeSymbol.Type.IsFunction()) {
          jumpTrack = LoadValueToRegister(calleeSymbol);
        }

        AddAssemblyCode(AssemblyOperator.add, AssemblyCode.FrameRegister, recordSize, null);

        if (calleeEllipse) {
          AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                                         AssemblyCode.FrameRegister);
          Assert.Error(extraSize >= 0);
          if (extraSize > 0) {
            AddAssemblyCode(AssemblyOperator.add, AssemblyCode.EllipseRegister, extraSize);
          }
        }
      }

      if (calleeSymbol.Type.IsFunction()) {
        AddAssemblyCode(AssemblyOperator.call, calleeSymbol.UniqueName, null, null);
        m_returnFloat = calleeSymbol.Type.ReturnType.IsFloating();
      }
      else {
        //Track track = LoadValueToRegister(calleeSymbol);
        AddAssemblyCode(AssemblyOperator.long_jmp, jumpTrack, null, null);
        m_returnFloat = calleeSymbol.Type.PointerType.ReturnType.IsFloating();
      }
    }

    public void GenerateFunctionPostCall(MiddleCode middleCode) {
      Register baseRegister = BaseRegister(null);
      m_trackMap = m_trackMapStack.Pop();
      IDictionary<Track, int> postMap = m_postMapStack.Pop();

      foreach (KeyValuePair<Track, int> pair in postMap)
      {
        Track track = pair.Key;
        int offset = pair.Value;
        AddAssemblyCode(AssemblyOperator.mov, track, baseRegister, offset);
      }

      /*foreach (KeyValuePair<Symbol,Track> pair in m_trackMap) {
        Symbol symbol = pair.Key;
        Track track = pair.Value;
        AddAssemblyCode(ObjectOperator.mov, track, baseRegister, symbol.Offset);
      }*/

      if (m_floatStackSize > 0)
      {
        int recordOffset = (int)middleCode.GetOperand(2);
        int recordSize = m_recordSizeStack.Pop();

        if (m_returnFloat)
        {
          AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister, recordOffset + recordSize);
        }

        int currentOffset = recordOffset + recordSize;
        for (int count = 0; count < m_floatStackSize; ++count)
        {
          currentOffset -= Type.QuarterWordSize;
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister, currentOffset);
        }

        if (m_returnFloat)
        {
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister, recordOffset + recordSize);
        }

        m_totalRecordSize -= recordSize;
      }
      else
      {
        m_totalRecordSize -= m_recordSizeStack.Pop();
      }
    }

    public Track LoadValueToRegister(Symbol symbol)
    {
      return LoadValueToRegister(symbol, null);
    }

    public Track LoadValueToRegister(Symbol symbol, Register? register)
    {
      if (register != null)
      {
        CheckRegister(symbol, register);
      }

      Track track;
      if (m_trackMap.TryGetValue(symbol, out track))
      {
        m_trackMap.Remove(symbol);

        if ((register != null) && (track.Register != null) &&
            !AssemblyCode.RegisterOverlap(register, track.Register))
        {
          Track newTrack = new Track(symbol, register);
          m_trackSet.Add(newTrack);
          AddAssemblyCode(AssemblyOperator.set_track_size, newTrack, track);
          AddAssemblyCode(AssemblyOperator.mov, newTrack, track);
          track = newTrack;
        }
        else if (register != null)
        {
          track.Register = register;
        }

        foreach (Track twinTrack in track.TwinTrackSet)
        {
          Assert.Error((twinTrack.Register == null) || twinTrack.Register.Equals(register));
          twinTrack.Register = register;
        }

        return track;
      }
      else
      {
        track = new Track(symbol, register);
        m_trackSet.Add(track);

        if ((symbol.Value is long) || (symbol.IsStaticOrExtern() && symbol.Type.IsFunctionArrayStringStructOrUnion()))
        {
          AddAssemblyCode(AssemblyOperator.mov, track, ValueOrAddress(symbol));
        }
        else if (symbol.Value is StaticAddress)
        {
          StaticAddress staticAddress = (StaticAddress)symbol.Value;
          AddAssemblyCode(AssemblyOperator.mov, track, staticAddress.UniqueName);

          if (staticAddress.Offset > 0)
          {
            AddAssemblyCode(AssemblyOperator.add, track, staticAddress.Offset);
          }
        }
        else if (symbol.Type.IsArray())
        {
          AddAssemblyCode(AssemblyOperator.mov, track, Base(symbol));

          int symbolOffset = Offset(symbol);
          if (symbolOffset != 0)
          {
            AddAssemblyCode(AssemblyOperator.add, track, symbolOffset);
          }
        }
        else
        {
          AddAssemblyCode(AssemblyOperator.mov, track, Base(symbol), Offset(symbol));
        }

        return track;
      }
    }

    public void SaveValueFromRegister(Track track, Symbol symbol)
    {
      AddAssemblyCode(AssemblyOperator.mov, Base(symbol), Offset(symbol), track);
    }

    public bool CheckRegister(Symbol symbol, Register? register)
    {
      foreach (KeyValuePair<Symbol, Track> entry in m_trackMap)
      {
        Symbol oldSymbol = entry.Key;
        Track oldTrack = entry.Value;

        if (!oldSymbol.Equals(symbol) &&
            AssemblyCode.RegisterOverlap(register, oldTrack.Register))
        {
          Track previousTrack = oldTrack.PreviousTrack;
          Assert.Error(previousTrack != null);

          Assert.Error(oldTrack.FirstLine != -1);
          Assert.Error((m_assemblyCodeList[oldTrack.FirstLine].Operator == AssemblyOperator.empty) &&
                       (m_assemblyCodeList[oldTrack.FirstLine + 1].Operator == AssemblyOperator.empty));

          m_assemblyCodeList[oldTrack.FirstLine] = new AssemblyCode(AssemblyOperator.set_track_size, oldTrack, previousTrack, null);
          m_assemblyCodeList[oldTrack.FirstLine + 1] = new AssemblyCode(AssemblyOperator.mov, oldTrack, previousTrack, null);

          //AddAssemblyCode(ObjectOperator.set_track_size, newTrack, oldTrack);
          //AddAssemblyCode(ObjectOperator.mov, newTrack, oldTrack);
          //m_trackMap[oldSymbol] = previousTrack;
          oldTrack.Register = null;
          oldTrack.PreviousTrack = null;
          return true;
        }
      }

      return false;
    }

    public void GenerateReturn(MiddleCode middleCode)
    {
      Assert.Error(m_floatStackSize == 0);
      Track track = new Track(Type.UnsignedIntegerType, null);
      AddAssemblyCode(AssemblyOperator.mov, track,
                    AssemblyCode.FrameRegister, SymbolTable.ReturnAddressOffset);
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                    AssemblyCode.FrameRegister, SymbolTable.EllipseFrameOffset);
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister,
                    AssemblyCode.FrameRegister, SymbolTable.RegularFrameOffset);
      AddAssemblyCode(AssemblyOperator.jmp, track);
    }

    public void GenerateExit(MiddleCode middleCode)
    {
      Symbol exitSymbol = (Symbol)middleCode.GetOperand(0);

      if (exitSymbol == null)
      {
        AddAssemblyCode(AssemblyOperator.mov, Register.al, 0x00);
      }
      else
      {
        LoadValueToRegister(exitSymbol, Register.al);
      }

      AddAssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C);
      AddAssemblyCode(AssemblyOperator.interrupt, 33);
    }

    public void GenerateGoto(MiddleCode middleCode)
    {
      AddAssemblyCode(AssemblyOperator.long_jmp, null, null, middleCode.GetOperand(0));
    }

    public void GenerateLoadToRegister(MiddleCode middleCode)
    {
      Register register = (Register)middleCode.GetOperand(0);
      Symbol symbol = (Symbol)middleCode.GetOperand(1);
      LoadValueToRegister(symbol, register);
    }

    /*public void GenerateSaveFromRegister(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode.GetOperand(0);
      Register register = (Register) middleCode.GetOperand(1);
      AddAssemblyCode(ObjectOperator.mov, Base(symbol), Offset(symbol), register);
    }*/

    public void GenerateInspectRegister(MiddleCode middleCode)
    {
      Symbol symbol = (Symbol)middleCode.GetOperand(0);
      Register register = (Register)middleCode.GetOperand(1);
      Track track = new Track(symbol, register);
      m_trackSet.Add(track);
      m_trackMap.Add(symbol, track);
    }

    public void GenerateFlagbyte(MiddleCode middleCode)
    {
      AddAssemblyCode(AssemblyOperator.lahf);
      Symbol symbol = (Symbol)middleCode.GetOperand(0);
      Assert.Error(symbol.Type.Size() == 1);
      Track track = new Track(symbol, Register.ah);
      m_trackSet.Add(track);
      m_trackMap.Add(symbol, track);
    }

    public void GenerateSystemInit(MiddleCode middleCode)
    {
      String name = (String)middleCode.GetOperand(0);
      SystemCode.GenerateInit(name, this);
    }

    public void GenerateSystemParameter(MiddleCode middleCode)
    {
      String name = (String)middleCode.GetOperand(0);
      int index = (int)middleCode.GetOperand(1);
      Symbol argSymbol = (Symbol)middleCode.GetOperand(2);
      SystemCode.GenerateParameter(name, index, argSymbol, this);
    }

    public void GenerateSystemCall(MiddleCode middleCode)
    {
      String name = (String)middleCode.GetOperand(0);
      Symbol returnSymbol = (Symbol)middleCode.GetOperand(1);
      SystemCode.GenerateCall(name, returnSymbol, this);
    }

    /*    public void GenerateFlagbyte(MiddleCode middleCode) {
          AddAssemblyCode(ObjectOperator.lahf);
          Symbol symbol = (Symbol) middleCode.GetOperand(0);
          Assert.Error(symbol.Type.Size() == 1);
    //      AddAssemblyCode(ObjectOperator.mov, Base(symbol), Offset(symbol), Register.ah);
        }*/

    /*public void GenerateSaveFromFlagbyte(MiddleCode middleCode) {
      AddAssemblyCode(ObjectOperator.sahf);
      AddAssemblyCode(ObjectOperator.mov, Register.al, Register.ah);
    }*/

    public void GenerateJumpRegister(MiddleCode middleCode)
    {
      Register jumpRegister = (Register)middleCode.GetOperand(0);
      AddAssemblyCode(AssemblyOperator.jmp, jumpRegister);
    }

    public void GenerateCarryFlag(MiddleCode middleCode)
    {
      AddAssemblyCode(AssemblyOperator.jc, null, null, middleCode.GetOperand(0));
    }

    public void GenerateInterrupt(MiddleCode middleCode)
    {
      int intValue = int.Parse(middleCode.GetOperand(0).ToString());
      AddAssemblyCode(AssemblyOperator.interrupt, intValue);
      m_trackMap.Clear();
    }

    public void GenerateCarryExpression(MiddleCode middleCode)
    {
      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, middleCode.GetOperand(0));
    }

    /*private static void MergeTracks(Track oldTrack, Track newTrack) {
    }*/

    private void GenerateInit(Sort sort, object value)
    {
      if (value is StaticAddress)
      {
        StaticAddress staticAddress = (StaticAddress)value;
        AddAssemblyCode(AssemblyOperator.define_address, staticAddress.UniqueName,
                        staticAddress.Offset); // dw name + offset
      }
      else
      {
        AddAssemblyCode(AssemblyOperator.define_value, sort, value);
        /*        IDictionary<Sort,ObjectOperator> sortToOperatorMap = new Dictionary<Sort,ObjectOperator>();
                sortToOperatorMap[Sort.Signed_Char] = ObjectOperator.define_char;
                sortToOperatorMap[Sort.Unsigned_Char] = ObjectOperator.define_char;
                sortToOperatorMap[Sort.Signed_Short_Int] = ObjectOperator.define_short;
                sortToOperatorMap[Sort.Unsigned_Short_Int] = ObjectOperator.define_short;
                sortToOperatorMap[Sort.Signed_Int] = ObjectOperator.define_int;
                sortToOperatorMap[Sort.Unsigned_Int] = ObjectOperator.define_int;
                sortToOperatorMap[Sort.Signed_Long_Int] = ObjectOperator.define_long;
                sortToOperatorMap[Sort.Unsigned_Long_Int] = ObjectOperator.define_long;
                sortToOperatorMap[Sort.Float] = ObjectOperator.define_float;
                sortToOperatorMap[Sort.Double] = ObjectOperator.define_double;
                sortToOperatorMap[Sort.Long_Double] = ObjectOperator.define_long_double;
                sortToOperatorMap[Sort.String] = ObjectOperator.define_string;
                ObjectOperator objectOperator = sortToOperatorMap[symbol.Type.Sort];
                AddAssemblyCode(objectOperator, symbol.Value);*/
      }
    }

    private void GenerateInitZero(int size)
    {
      AddAssemblyCode(AssemblyOperator.define_zero_sequence, size);
    }

    public void GenerateIntegralAssign(MiddleCode middleCode)
    {
      Symbol resultSymbol = (Symbol)middleCode.GetOperand(0),
             assignSymbol = (Symbol)middleCode.GetOperand(1);

      if (resultSymbol.Temporary && (resultSymbol.AddressSymbol == null))
      {
        Track resultTrack;

        if (m_trackMap.TryGetValue(resultSymbol, out resultTrack))
        {
          m_trackMap.Remove(resultSymbol);
          Track twinTrack = new Track(resultSymbol, resultTrack.Register);
          //resultTrack.TwinTrackSet.UnionWith(twinTrack.TwinTrackSet);

          twinTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);
          foreach (Track tTrack in resultTrack.TwinTrackSet)
          {
            tTrack.TwinTrackSet.Add(twinTrack);
          }

          resultTrack.TwinTrackSet.Add(twinTrack);
          twinTrack.TwinTrackSet.Add(resultTrack);

          Assert.Error(resultTrack.Register == twinTrack.Register);
          resultTrack = twinTrack;
        }
        else
        {
          resultTrack = new Track(resultSymbol);
          m_trackSet.Add(resultTrack);
        }

        m_trackMap.Add(resultSymbol, resultTrack);

        Track assignTrack;
        if (m_trackMap.TryGetValue(assignSymbol, out assignTrack))
        {
          m_trackMap.Remove(assignSymbol);
          m_trackMap[resultSymbol] = assignTrack;
          assignTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);

          /*foreach (Track tTrack in assignTrack.TwinTrackSet) {
            tTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);
          }

          foreach (Track tTrack in resultTrack.TwinTrackSet) {
            tTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);
          }

          resultTrack.TwinTrackSet.UnionWith(assignTrack.TwinTrackSet);
          assignTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);*/

          /*          resultTrack.TwinTrackSet.UnionWith(assignTrack.TwinTrackSet);
                    assignTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);
                    resultTrack.TwinTrackSet.Add(assignTrack);
                    assignTrack.TwinTrackSet.Add(resultTrack);*/
        }
        else if ((assignSymbol.Value is long) || assignSymbol.Type.IsArrayFunctionOrString())
        {
          AddAssemblyCode(AssemblyOperator.mov, resultTrack, ValueOrAddress(assignSymbol));
        }
        else
        {
          AddAssemblyCode(AssemblyOperator.mov, resultTrack, Base(assignSymbol), Offset(assignSymbol));
        }
      }
      else
      {
        Track assignTrack;

        if ((assignSymbol.Value is long) ||
            (assignSymbol.IsStaticOrExtern() && assignSymbol.Type.IsArrayFunctionOrString()))
        {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.mov, assignSymbol.Type.Size());
          AddAssemblyCode(sizeOperator, Base(resultSymbol), Offset(resultSymbol), ValueOrAddress(assignSymbol));
        }
        else if (m_trackMap.TryGetValue(assignSymbol, out assignTrack))
        {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), assignTrack);
          m_trackMap.Remove(assignSymbol);
        }
        else if (assignSymbol.Type.IsArray())
        {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), Base(assignSymbol));
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.add, Type.PointerSize);
          AddAssemblyCode(sizeOperator, Base(resultSymbol), Offset(resultSymbol), Offset(assignSymbol));
        }
        else
        {
          object b = Base(resultSymbol);
          assignTrack = LoadValueToRegister(assignSymbol);
          AddAssemblyCode(AssemblyOperator.mov, b, Offset(resultSymbol), assignTrack);
        }
      }
    }

    public static IDictionary<MiddleOperator, AssemblyOperator>
                           m_middleToIntegralBinaryTargetMap =
                           new Dictionary<MiddleOperator, AssemblyOperator>();

    public static IDictionary<int, Register> LeftMultiplyMap = new Dictionary<int, Register>();

    public static IDictionary<Pair<MiddleOperator, int>, Register>
                    ResultMultiplyMap = new Dictionary<Pair<MiddleOperator, int>, Register>();

    public static IDictionary<Pair<MiddleOperator, int>, Register>
                    ClearMultiplyMap = new Dictionary<Pair<MiddleOperator, int>, Register>();

    public static IDictionary<MiddleOperator, AssemblyOperator>
                    m_middleToIntegralUnaryTargetMap = new Dictionary<MiddleOperator, AssemblyOperator>();

    public static IDictionary<Sort, AssemblyOperator> m_floatPushMap = new Dictionary<Sort, AssemblyOperator>();

    public static IDictionary<Sort, AssemblyOperator> m_floatTopMap = new Dictionary<Sort, AssemblyOperator>(),
                                                  m_floatPopMap = new Dictionary<Sort, AssemblyOperator>();

    public static IDictionary<Pair<int, int>, int>
                    m_maskMap = new Dictionary<Pair<int, int>, int>();

    public static IDictionary<MiddleOperator, AssemblyOperator>
                    m_middleToFloatingBinaryTargetMap = new Dictionary<MiddleOperator, AssemblyOperator>();

    public static IDictionary<MiddleOperator, AssemblyOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator, AssemblyOperator>();

    public static IDictionary<MiddleOperator, AssemblyOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator, AssemblyOperator>();

    public static IDictionary<AssemblyOperator, AssemblyOperator>
                    m_inverseMap = new Dictionary<AssemblyOperator, AssemblyOperator>();

    static AssemblyCodeGenerator()
    {
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Assign, AssemblyOperator.mov);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Parameter, AssemblyOperator.mov);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Compare, AssemblyOperator.cmp);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BinaryAdd, AssemblyOperator.add);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BinarySubtract, AssemblyOperator.sub);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BitwiseAnd, AssemblyOperator.and);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BitwiseIOr, AssemblyOperator.or);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BitwiseXOr, AssemblyOperator.xor);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.ShiftLeft, AssemblyOperator.shl);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.ShiftRight, AssemblyOperator.shr);

      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Equal, AssemblyOperator.jne);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.NotEqual, AssemblyOperator.je);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Carry, AssemblyOperator.jc);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.NotCarry, AssemblyOperator.jnc);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedLessThan, AssemblyOperator.jge);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, AssemblyOperator.jg);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedGreaterThan, AssemblyOperator.jle);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.jl);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedLessThan, AssemblyOperator.jae);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedLessThanEqual, AssemblyOperator.ja);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedGreaterThan, AssemblyOperator.jbe);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedGreaterThanEqual, AssemblyOperator.jb);

      LeftMultiplyMap.Add(Type.ByteSize, Register.al);
      LeftMultiplyMap.Add(Type.WordSize, Register.ax);
      LeftMultiplyMap.Add(Type.DoubleWordSize, Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.WordSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.DoubleWordSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.WordSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.DoubleWordSize),
                            Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.WordSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.DoubleWordSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.WordSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.DoubleWordSize),
                            Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.ByteSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.WordSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.DoubleWordSize),
                            Register.edx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.ByteSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.WordSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.DoubleWordSize),
                            Register.edx);

      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.ByteSize),
                            Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.WordSize),
                            Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.DoubleWordSize),
                            Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.ByteSize),
                            Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.WordSize),
                            Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.DoubleWordSize),
                            Register.edx);

      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.ByteSize),
                            Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.WordSize),
                            Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.DoubleWordSize),
                            Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.ByteSize),
                            Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.WordSize),
                            Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.DoubleWordSize),
                            Register.edx);

      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.ByteSize),
                            Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.WordSize),
                            Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.DoubleWordSize),
                            Register.edx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.ByteSize),
                            Register.ah);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.WordSize),
                            Register.dx);
      ClearMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.DoubleWordSize),
                            Register.edx);

      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd, AssemblyOperator.empty);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot, AssemblyOperator.not);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, AssemblyOperator.neg);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Increment, AssemblyOperator.inc);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Decrement, AssemblyOperator.dec);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedMultiply, AssemblyOperator.imul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedDivide, AssemblyOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedModulo, AssemblyOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedMultiply, AssemblyOperator.mul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedDivide, AssemblyOperator.div);
      m_middleToIntegralUnaryTargetMap.
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

      m_maskMap.Add(new Pair<int, int>(1, 2), 0x00FF);
      m_maskMap.Add(new Pair<int, int>(1, 4), 0x000000FF);
      m_maskMap.Add(new Pair<int, int>(2, 4), 0x0000FFFF);

      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinaryAdd, AssemblyOperator.fadd);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinarySubtract, AssemblyOperator.fsub);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedMultiply, AssemblyOperator.fmul);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedDivide, AssemblyOperator.fdiv);

      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, AssemblyOperator.jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, AssemblyOperator.je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Carry, AssemblyOperator.jc);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotCarry, AssemblyOperator.jnc);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, AssemblyOperator.jbe);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, AssemblyOperator.jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, AssemblyOperator.jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.ja);

      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd, AssemblyOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot, AssemblyOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, AssemblyOperator.fchs);

      m_inverseMap.Add(AssemblyOperator.je, AssemblyOperator.jne);
      m_inverseMap.Add(AssemblyOperator.jne, AssemblyOperator.je);
      m_inverseMap.Add(AssemblyOperator.jl, AssemblyOperator.jge);
      m_inverseMap.Add(AssemblyOperator.jle, AssemblyOperator.jg);
      m_inverseMap.Add(AssemblyOperator.jg, AssemblyOperator.jle);
      m_inverseMap.Add(AssemblyOperator.jge, AssemblyOperator.jl);
      m_inverseMap.Add(AssemblyOperator.ja, AssemblyOperator.jbe);
      m_inverseMap.Add(AssemblyOperator.jae, AssemblyOperator.jb);
      m_inverseMap.Add(AssemblyOperator.jb, AssemblyOperator.jae);
      m_inverseMap.Add(AssemblyOperator.jbe, AssemblyOperator.ja);
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();

    static ObjectCodeGenerator() {
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, ObjectOperator.jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, ObjectOperator.je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, ObjectOperator.jbe);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, ObjectOperator.jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, ObjectOperator.jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, ObjectOperator.ja);
    }

    public void GenerateFloatingRelation(MiddleCode middleCode, int index) {
      int target = (int) middleCode.Operand(0);
      AddAssemblyCode(ObjectOperator.fcompp);
      AddAssemblyCode(ObjectOperator.fstsw, Register.ax);
      AddAssemblyCode(ObjectOperator.sahf);
      ObjectOperator objectOperator =
        m_middleToFloatingRelationTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, index + 1);
      AddAssemblyCode(ObjectOperator.long_jmp, null, null, target);
    }

    public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.fchs);
    }*/

    public void GenerateFloatingRelation(MiddleCode middleCode, int index)
    {
      Assert.Error((m_floatStackSize -= 2) >= 0);
      int target = (int)middleCode.GetOperand(0);
      AddAssemblyCode(AssemblyOperator.fcompp);
      AddAssemblyCode(AssemblyOperator.fstsw, Register.ax);
      AddAssemblyCode(AssemblyOperator.sahf);
      AssemblyOperator objectOperator =
        m_middleToFloatingRelationTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, index + 1);
      AddAssemblyCode(AssemblyOperator.long_jmp, null, null, target);
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();

    public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, ObjectOperator.jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, ObjectOperator.je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, ObjectOperator.jbe);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, ObjectOperator.jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, ObjectOperator.jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, ObjectOperator.ja);
    }

    static ObjectCodeGenerator() {
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.fchs);
    }

    public void GenerateIntegralAdditionBitwiseShiftx(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode.GetOperand(0),
             leftSymbol = (Symbol) middleCode.GetOperand(1),
             rightSymbol = (Symbol) middleCode.GetOperand(2);
    }*/

    public void GenerateIntegralAdditionBitwiseShift(MiddleCode middleCode)
    {
      Symbol resultSymbol = (Symbol)middleCode.GetOperand(0),
             leftSymbol = (Symbol)middleCode.GetOperand(1),
             rightSymbol = (Symbol)middleCode.GetOperand(2);

      if (resultSymbol.Equals(leftSymbol) && !resultSymbol.Temporary)
      {
        GenerateCompoundIntegralBinary(middleCode.Operator, leftSymbol, rightSymbol);
      }
      else
      {
        GenerateSimpleIntegralBinary(middleCode.Operator, resultSymbol, leftSymbol, rightSymbol);
      }
    }

    public void GenerateCompoundIntegralBinary(MiddleOperator middleOperator,
                                               Symbol leftSymbol, Symbol rightSymbol)
    {
      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleOperator];
      Assert.Error(!m_trackMap.ContainsKey(leftSymbol));

      if ((rightSymbol.Value is long) ||
          (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString()))
      {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator, leftSymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(leftSymbol), Offset(leftSymbol), ValueOrAddress(rightSymbol));
      }
      else if (MiddleCode.IsShift(middleOperator))
      {
        Track rightTrack = LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
        AddAssemblyCode(AssemblyOperator.mov, Base(leftSymbol), Offset(leftSymbol), rightTrack);
      }
      else
      {
        Track rightTrack = LoadValueToRegister(rightSymbol);
        AddAssemblyCode(AssemblyOperator.mov, Base(leftSymbol), Offset(leftSymbol), rightTrack);
      }

    }

    public void GenerateSimpleIntegralBinary(MiddleOperator middleOperator, Symbol resultSymbol,
                                             Symbol leftSymbol, Symbol rightSymbol)
    {
      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleOperator];
      Track leftTrack = LoadValueToRegister(leftSymbol), rightTrack;

      if (m_trackMap.TryGetValue(rightSymbol, out rightTrack))
      {
        AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        m_trackMap.Remove(rightSymbol);
      }
      else if ((rightSymbol.Value is long) || rightSymbol.Type.IsArrayFunctionOrString())
      {
        AddAssemblyCode(objectOperator, leftTrack, ValueOrAddress(rightSymbol));
      }
      else if (MiddleCode.IsShift(middleOperator))
      {
        rightTrack = LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
        AddAssemblyCode(objectOperator, leftTrack, rightTrack);
      }
      else
      {
        AddAssemblyCode(objectOperator, leftTrack, Base(rightSymbol), Offset(rightSymbol));
      }

      if (resultSymbol.Temporary && (resultSymbol.AddressSymbol == null))
      {
        m_trackMap[resultSymbol] = leftTrack;
      }
      else
      {
        AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), leftTrack);
      }
    }

    public void GenerateIntegralRelationBinary(Symbol leftSymbol, Symbol rightSymbol)
    {
      Track leftTrack = null, rightTrack = null;
      m_trackMap.TryGetValue(leftSymbol, out leftTrack);
      m_trackMap.TryGetValue(rightSymbol, out rightTrack);

      if ((leftTrack == null) && (rightTrack == null))
      {
        if ((leftSymbol.AddressSymbol != null) ||
            (leftSymbol.IsStaticOrExtern() && !leftSymbol.Type.IsArrayFunctionOrString()) ||
            (leftSymbol.IsAutoOrRegister() && !leftSymbol.Type.IsArray()))
        {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.cmp, leftSymbol.Type.Size());

          if ((rightSymbol.Value is long) ||
              (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString()))
          {
            AddAssemblyCode(sizeOperator, Base(leftSymbol), Offset(leftSymbol), ValueOrAddress(rightSymbol));
            return;
          }
        }

        if (rightSymbol.IsAutoOrRegister() && rightSymbol.Type.IsArray())
        {
          rightTrack = LoadValueToRegister(rightSymbol);
        }
        else
        {
          leftTrack = LoadValueToRegister(leftSymbol);
        }
      }

      if (leftTrack != null)
      {
        if (rightSymbol.Type.IsArray())
        {
          rightTrack = LoadValueToRegister(rightSymbol);
        }

        if ((rightSymbol.Value is long) ||
            (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString()))
        {
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, ValueOrAddress(rightSymbol)); // cmp ax, 123
        }
        else if (rightTrack != null)
        {
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack); // cmp ax, bx
          m_trackMap.Remove(rightSymbol);
        }
        else
        {
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, Base(rightSymbol), Offset(rightSymbol)); // cmp ax, [bp + 2]
        }

        m_trackMap.Remove(leftSymbol);
      }
      else
      { // rightTrack != null
        Assert.Error(!(leftSymbol.Value is long));

        if ((leftSymbol.IsStaticOrExtern() && leftSymbol.Type.IsArrayFunctionOrString()) ||
             (leftSymbol.IsAutoOrRegister() && leftSymbol.Type.IsArray()))
        {
          leftTrack = LoadValueToRegister(leftSymbol);
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack);
        }
        else
        {
          AddAssemblyCode(AssemblyOperator.cmp, Base(leftSymbol), Offset(leftSymbol), rightTrack);
        }

        m_trackMap.Remove(rightSymbol);
      }
    }

    /*public static IDictionary<int,Register> LeftMultiplyMap = new Dictionary<int,Register>();

    static ObjectCodeGenerator() {
      LeftMultiplyMap.Add(Type.ByteSize, Register.al);
      LeftMultiplyMap.Add(Type.IntegerSize, Register.ax);
      LeftMultiplyMap.Add(Type.LongSize, Register.eax);
    }
  
    public static IDictionary<Pair<MiddleOperator,int>,Register>
                    ResultMultiplyMap = new Dictionary<Pair<MiddleOperator,int>,Register>();

    static ObjectCodeGenerator() {
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply,Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.LongSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply,Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.LongSize),
                            Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.LongSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.ByteSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.LongSize),
                            Register.eax);
    
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.ByteSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.IntegerSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.LongSize),
                            Register.edx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.ByteSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.IntegerSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.LongSize),
                            Register.edx);
    }
  
    public static IDictionary<Pair<MiddleOperator,int>,Register>
                    ExtraMultiplyMap = new Dictionary<Pair<MiddleOperator,int>,Register>();
  
    static ObjectCodeGenerator() {
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.ByteSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply,Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.ByteSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply,Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.LongSize),
                            Register.edx);
    
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.ByteSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.ByteSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.LongSize),
                            Register.edx);

      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.ByteSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.ByteSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.LongSize),
                            Register.edx);
    }*/

    private object ValueOrAddress(Symbol symbol)
    {
      if (symbol.Value is long)
      {
        return ((int)((long)symbol.Value));
      }
      else if (symbol.IsAutoOrRegister())
      {
        Track track = new Track(symbol);
        m_trackSet.Add(track);
        AddAssemblyCode(AssemblyOperator.mov, track, Base(symbol));
        AddAssemblyCode(AssemblyOperator.add, track, Offset(symbol));
        return track;
      }
      else
      {
        return symbol.UniqueName;
      }
    }

    private object Base(Symbol symbol)
    {
      if (symbol.Value is StaticAddress)
      {
        StaticAddress staticAddress = (StaticAddress)symbol.Value;
        return staticAddress.UniqueName;
      }
      else if (!symbol.Switch && (symbol.AddressSymbol != null))
      {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        addressTrack = SetPointer(addressTrack, symbol.AddressSymbol);
        m_trackMap.Remove(symbol.AddressSymbol);
        return addressTrack;
      }
      else if (symbol.IsStaticOrExtern())
      {
        return symbol.UniqueName;
      }
      else
      { //resultSymbol.IsAutoOrRegister()
        return BaseRegister(symbol);
      }
    }

    private int Offset(Symbol symbol)
    {
      if (symbol.Value is StaticAddress)
      {
        StaticAddress staticAddress = (StaticAddress)symbol.Value;
        return staticAddress.Offset;
      }
      else if (!symbol.Switch && (symbol.AddressSymbol != null))
      {
        return symbol.AddressOffset;
      }
      else
      {
        return symbol.Offset;
      }
      /*else if (symbol.IsStaticOrExtern()) {
        return symbol.Offset;
      }
      else { //resultSymbol.IsAutoOrRegister()
        return symbol.Offset;
      }*/
    }

    public void GenerateUnary(MiddleOperator middleOperator, Symbol resultSymbol,
                              Symbol unarySymbol)
    {
      AssemblyOperator objectOperator =
        m_middleToIntegralUnaryTargetMap[middleOperator];

      if ((resultSymbol == null) || resultSymbol.Equals(unarySymbol))
      { // ++i; i = ~i; dec [bp + 6]; not [bp + 6]
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator, unarySymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(unarySymbol), Offset(unarySymbol));
      }
      else
      {
        Track unaryTrack = LoadValueToRegister(unarySymbol); // t0 = -i; mov ax, [i], neg ax
        AddAssemblyCode(objectOperator, unaryTrack);
        m_trackMap[resultSymbol] = unaryTrack;
      }
    }

    public void GenerateIntegralMultiply(MiddleCode middleCode)
    {
      Symbol leftSymbol = (Symbol) middleCode.GetOperand(1);
      Register leftRegister = LeftMultiplyMap[leftSymbol.Type.Size()];
      Track leftTrack = LoadValueToRegister(leftSymbol, leftRegister);
      AddAssemblyCode(AssemblyOperator.empty, leftTrack);

      Pair<MiddleOperator, int> pair = new Pair<MiddleOperator, int>(middleCode.Operator, leftSymbol.Type.Size());
      Register clearRegister = ClearMultiplyMap[pair];
      Track clearTrack = new Track(leftSymbol, clearRegister);
      m_trackSet.Add(clearTrack);
      AddAssemblyCode(AssemblyOperator.xor, clearTrack, clearTrack);

      Symbol rightSymbol = (Symbol)middleCode.GetOperand(2);
      AssemblyOperator objectOperator =
        m_middleToIntegralUnaryTargetMap[middleCode.Operator];

      if (rightSymbol.Temporary && (rightSymbol.AddressSymbol == null))
      {
        Track rightTrack = LoadValueToRegister(rightSymbol);
        AddAssemblyCode(objectOperator, rightTrack);
      }
      else
      {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator, rightSymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(rightSymbol), Offset(rightSymbol));
      }

      Symbol resultSymbol = (Symbol)middleCode.GetOperand(0);
      Register resultRegister = ResultMultiplyMap[pair];
      Track resultTrack = new Track(resultSymbol, resultRegister);
      m_trackSet.Add(resultTrack);
      AddAssemblyCode(AssemblyOperator.empty, resultTrack);

      if (resultSymbol.Temporary)
      {
        if (resultSymbol.AddressSymbol != null)
        {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), resultRegister);
        }
        else
        {
          m_trackMap[resultSymbol] = resultTrack;
        }

        AddAssemblyCode(AssemblyOperator.empty, resultTrack);

        Track nextTrack = new Track(resultSymbol, resultRegister);
        nextTrack.PreviousTrack = resultTrack;
        nextTrack.FirstLine = m_assemblyCodeList.Count;
        AddAssemblyCode(AssemblyOperator.empty, nextTrack);
        AddAssemblyCode(AssemblyOperator.empty);
        m_trackMap[resultSymbol] = nextTrack;
      }
      else
      {
        AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), resultRegister);
      }

      /*      if (resultSymbol.Temporary && (resultSymbol.AddressSymbol == null)) {
              m_trackMap[resultSymbol] = resultTrack;
            }
            else if (resultSymbol.Temporary) {
              AddAssemblyCode(ObjectOperator.empty, resultTrack);
              AddAssemblyCode(ObjectOperator.empty, resultTrack);
            }
            else {
              AddAssemblyCode(ObjectOperator.mov, Base(resultSymbol), Offset(resultSymbol), resultRegister);
            }*/
    }

    public void GenerateIntegralRelation(MiddleCode middleCode, int index)
    {
      GenerateIntegralRelationBinary((Symbol)middleCode.GetOperand(1), (Symbol)middleCode.GetOperand(2));
      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, index + 1);
      int target = (int)middleCode.GetOperand(0);
      AddAssemblyCode(AssemblyOperator.long_jmp, null, null, target);
    }

    public void GenerateIntegralIncrementDecrement(MiddleCode middleCode)
    {
      GenerateUnary(middleCode.Operator, (Symbol)middleCode.GetOperand(0),
                    (Symbol)middleCode.GetOperand(1));
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToIntegralUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.not);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.neg);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Increment, ObjectOperator.inc);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Decrement, ObjectOperator.dec);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedMultiply, ObjectOperator.imul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedDivide, ObjectOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedModulo, ObjectOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedMultiply, ObjectOperator.mul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedDivide, ObjectOperator.div);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedModulo, ObjectOperator.div);
    }*/

    public void GenerateIntegralUnary(MiddleCode middleCode)
    {
      GenerateUnary(middleCode.Operator, (Symbol)middleCode.GetOperand(0),
                    (Symbol)middleCode.GetOperand(1));
    }

    public void GenerateAddress(MiddleCode middleCode)
    {
      Symbol resultSymbol = (Symbol)middleCode.GetOperand(0),
             addressSymbol = (Symbol)middleCode.GetOperand(1);

      Track track = LoadAddressToRegister(addressSymbol);
      m_trackMap[resultSymbol] = track;
      m_trackMap.Remove(addressSymbol);
    }

    public void GenerateFloatingDeref(MiddleCode middleCode)
    {
      Symbol resultSymbol = (Symbol)middleCode.GetOperand(0);
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);

      if (resultSymbol.Switch)
      {
        Track resultTrack = new Track(resultSymbol);
        m_trackSet.Add(resultTrack);
        AddAssemblyCode(AssemblyOperator.mov, resultTrack, addressTrack, resultSymbol.AddressOffset);
        AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), resultTrack);
        m_trackMap.Remove(resultSymbol.AddressSymbol);
        //m_trackMap.Remove(resultSymbol);
      }
    }

    public void GenerateIntegralDeref(MiddleCode middleCode)
    {
      Symbol resultSymbol = (Symbol)middleCode.GetOperand(0);
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);

      if (resultSymbol.Switch)
      {
        Track resultTrack = new Track(resultSymbol);
        m_trackSet.Add(resultTrack);
        AddAssemblyCode(AssemblyOperator.mov, resultTrack, addressTrack, resultSymbol.AddressOffset);
        AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), resultTrack);
        m_trackMap.Remove(resultSymbol.AddressSymbol);
        //m_trackMap.Remove(resultSymbol);
      }
    }

    public Track LoadAddressToRegister(Symbol symbol)
    {
      return LoadAddressToRegister(symbol, null);
    }

    public Track LoadAddressToRegister(Symbol symbol, Register? register)
    {
      Symbol addressSymbol = symbol.AddressSymbol;

      if (addressSymbol != null)
      {
        Track addressTrack = LoadValueToRegister(addressSymbol);
        addressTrack = SetPointer(addressTrack, addressSymbol);

        if ((register != null) && (addressTrack.Register == null) &&
            !AssemblyCode.RegisterOverlap(register, addressTrack.Register))
        {
          Track newAddressTrack = new Track(symbol, register);
          m_trackSet.Add(newAddressTrack);
          newAddressTrack.Pointer = true;
          AddAssemblyCode(AssemblyOperator.set_track_size, newAddressTrack, addressTrack);
          AddAssemblyCode(AssemblyOperator.mov, newAddressTrack, addressTrack);
          addressTrack = newAddressTrack;
        }
        else if (register != null)
        {
          addressTrack.Register = register;
        }

        return addressTrack;
      }
      else
      {
        Symbol pointerSymbol = new Symbol(new Type(symbol.Type));
        Track addressTrack = new Track(pointerSymbol, register);
        m_trackSet.Add(addressTrack);
        addressTrack = SetPointer(addressTrack, pointerSymbol);

        if (symbol.IsAutoOrRegister())
        {
          AddAssemblyCode(AssemblyOperator.mov, addressTrack, BaseRegister(symbol));
          AddAssemblyCode(AssemblyOperator.add, addressTrack, symbol.Offset);
        }
        else
        {
          AddAssemblyCode(AssemblyOperator.mov, addressTrack, symbol.UniqueName);
        }

        return addressTrack;
      }
    }

    /*public static IDictionary<Sort,ObjectOperator> m_floatPushMap = new Dictionary<Sort,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_floatPushMap.Add(Sort.Signed_Int, ObjectOperator.fild_word);
      m_floatPushMap.Add(Sort.Unsigned_Int, ObjectOperator.fild_word);
      m_floatPushMap.Add(Sort.Signed_Long_Int, ObjectOperator.fild_dword);
      m_floatPushMap.Add(Sort.Unsigned_Long_Int, ObjectOperator.fild_dword);
      m_floatPushMap.Add(Sort.Float, ObjectOperator.fld_dword);
      m_floatPushMap.Add(Sort.Double, ObjectOperator.fld_qword);
      m_floatPushMap.Add(Sort.Long_Double, ObjectOperator.fld_qword);
    }*/

    public void PushSymbol(Symbol symbol)
    {
      Assert.Error((++m_floatStackSize) <= FloatingStackMaxSize, Message.Floating_stack_overflow);
      AssemblyOperator objectOperator = m_floatPushMap[symbol.Type.Sort];
      Track track;

      if (((symbol.Value is long) && (((long)symbol.Value) == 0)) ||
          ((symbol.Value is decimal) && (((decimal)symbol.Value) == 0)))
      {
        AddAssemblyCode(AssemblyOperator.fldz);
      }
      else if (((symbol.Value is long) && (((long)symbol.Value) == 1)) ||
               ((symbol.Value is decimal) && (((decimal)symbol.Value) == 1)))
      {
        AddAssemblyCode(AssemblyOperator.fld1);
      }
      else if ((symbol.Value is long) || (symbol.Value is decimal))
      {
        AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
      }
      else if (symbol.Type.IsFunctionArrayStringStructOrUnion())
      {
        if (symbol.IsAutoOrRegister())
        {
          AddAssemblyCode(AssemblyOperator.mov, IntegralStorageName, 0, BaseRegister(symbol));
          AssemblyOperator addObjectOp = AssemblyCode.OperatorToSize(AssemblyOperator.add, Type.PointerSize);
          AddAssemblyCode(addObjectOp, IntegralStorageName, 0, symbol.Offset);
        }
        else
        {
          AssemblyOperator movObjectOp = AssemblyCode.OperatorToSize(AssemblyOperator.mov, Type.PointerSize);
          AddAssemblyCode(movObjectOp, IntegralStorageName, 0, symbol.UniqueName);
        }

        AddAssemblyCode(objectOperator, IntegralStorageName, 0);
      }
      else if (m_trackMap.TryGetValue(symbol, out track))
      {
        m_trackMap.Remove(symbol);
        AddAssemblyCode(AssemblyOperator.mov, IntegralStorageName, 0, track);
        AddAssemblyCode(objectOperator, IntegralStorageName, 0);
      }
      else
      {
        AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
      }
    }

    /*public static IDictionary<Sort,ObjectOperator> m_floatTopMap = new Dictionary<Sort,ObjectOperator>(),
                                                  m_floatPopMap = new Dictionary<Sort,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_floatTopMap.Add(Sort.Signed_Int, ObjectOperator.fist_word);
      m_floatTopMap.Add(Sort.Unsigned_Int, ObjectOperator.fist_word);
      m_floatTopMap.Add(Sort.Pointer, ObjectOperator.fist_word);
      m_floatTopMap.Add(Sort.Signed_Long_Int, ObjectOperator.fist_dword);
      m_floatTopMap.Add(Sort.Unsigned_Long_Int, ObjectOperator.fist_dword);
      m_floatTopMap.Add(Sort.Float, ObjectOperator.fst_dword);
      m_floatTopMap.Add(Sort.Double, ObjectOperator.fst_qword);
      m_floatTopMap.Add(Sort.Long_Double, ObjectOperator.fst_qword);
  
      m_floatPopMap.Add(Sort.Signed_Int, ObjectOperator.fistp_word);
      m_floatPopMap.Add(Sort.Unsigned_Int, ObjectOperator.fistp_word);
      m_floatPopMap.Add(Sort.Pointer, ObjectOperator.fistp_word);
      m_floatPopMap.Add(Sort.Signed_Long_Int, ObjectOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Unsigned_Long_Int, ObjectOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Float, ObjectOperator.fstp_dword);
      m_floatPopMap.Add(Sort.Double, ObjectOperator.fstp_qword);
      m_floatPopMap.Add(Sort.Long_Double, ObjectOperator.fstp_qword);    
    }*/

    public enum TopOrPop { Top, Pop };

    public void PopEmpty()
    {
      AddAssemblyCode(AssemblyOperator.fistp_word, AssemblyCodeGenerator.IntegralStorageName, 0);
    }

    public void TopPopSymbol(Symbol symbol, TopOrPop topOrPop)
    {
      Assert.Error(symbol != null);
      AssemblyOperator objectOperator;

      if (topOrPop == TopOrPop.Pop)
      {
        objectOperator = m_floatPopMap[symbol.Type.Sort];
        Assert.Error((--m_floatStackSize) >= 0);
      }
      else
      {
        objectOperator = m_floatTopMap[symbol.Type.Sort];
      }

      if (symbol.Temporary && (symbol.AddressSymbol == null) && (symbol.Offset == 0))
      {
        AddAssemblyCode(objectOperator, AssemblyCodeGenerator.IntegralStorageName, 0);
        Track track = new Track(symbol);
        m_trackSet.Add(track);
        AddAssemblyCode(AssemblyOperator.mov, track, AssemblyCodeGenerator.IntegralStorageName, 0);
        m_trackMap[symbol] = track;
      }
      else
      {
        AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
      }
    }

    /*public static IDictionary<Pair<int,int>,int>
                    m_maskMap = new Dictionary<Pair<int,int>,int>();

    static ObjectCodeGenerator() {
      m_maskMap.Add(new Pair<int,int>(1,2), 0x00FF);
      m_maskMap.Add(new Pair<int,int>(1,4), 0x000000FF);
      m_maskMap.Add(new Pair<int,int>(2,4), 0x0000FFFF);  
    }*/

    public void GenerateIntegralToIntegral(MiddleCode middleCode, int index)
    {
      Symbol toSymbol = (Symbol)middleCode.GetOperand(0),
             fromSymbol = (Symbol)middleCode.GetOperand(1);

      Type toType = toSymbol.Type, fromType = fromSymbol.Type;
      int toSize = toType.Size(), fromSize = fromType.Size();

      Track track = LoadValueToRegister(fromSymbol);
      AddAssemblyCode(AssemblyOperator.set_track_size, track, toSize);

      if (fromSize != toSize)
      {
        if (fromSize < toSize)
        {
          int mask = m_maskMap[new Pair<int, int>(fromSize, toSize)];
          AddAssemblyCode(AssemblyOperator.and, track, mask, null);
        }

        if (fromType.IsSigned() && toType.IsSigned())
        {
          AddAssemblyCode(AssemblyOperator.set_track_size, track, fromSize);
          AddAssemblyCode(AssemblyOperator.cmp, track, 0, null);
          AddAssemblyCode(AssemblyOperator.jge, null, null, index + 1);
          AddAssemblyCode(AssemblyOperator.neg, track);
          AddAssemblyCode(AssemblyOperator.set_track_size, track, toSize);
          AddAssemblyCode(AssemblyOperator.neg, track);
        }
      }

      m_trackMap[toSymbol] = track;
    }

    public void GenerateIntegralToFloating(MiddleCode middleCode)
    {
      Symbol fromSymbol = (Symbol)middleCode.GetOperand(1);
      PushSymbol(fromSymbol);
    }

    public void GenerateFloatingToIntegral(MiddleCode middleCode)
    {
      Symbol toSymbol = (Symbol)middleCode.GetOperand(0);
      TopPopSymbol(toSymbol, TopOrPop.Pop);
    }

    public void GenerateIntegralParameter(MiddleCode middleCode)
    {
      Symbol fromSymbol = (Symbol)middleCode.GetOperand(1);

      Symbol toSymbol;
      if (fromSymbol.Type.IsArray())
      {
        toSymbol = new Symbol(new Type(fromSymbol.Type.ArrayType));
      }
      else if (fromSymbol.Type.IsFunction())
      {
        toSymbol = new Symbol(new Type(fromSymbol.Type));
      }
      else
      {
        toSymbol = new Symbol(fromSymbol.Type);
      }

      toSymbol.Offset = m_totalRecordSize + ((int)middleCode.GetOperand(2));

      if (fromSymbol.Value is long)
      {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(AssemblyOperator.mov, toSymbol.Type.Size());
        int fromValue = (int)((long)fromSymbol.Value);
        AddAssemblyCode(sizeOperator, Base(toSymbol), Offset(toSymbol), fromValue);
      }
      else if (fromSymbol.IsStaticOrExtern() && fromSymbol.Type.IsArrayFunctionOrString())
      {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(AssemblyOperator.mov, toSymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(toSymbol), Offset(toSymbol), fromSymbol.UniqueName);
      }
      else if (fromSymbol.IsAutoOrRegister() && fromSymbol.Type.IsArray())
      {
        AddAssemblyCode(AssemblyOperator.mov, Base(toSymbol), Offset(toSymbol), Base(fromSymbol));
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(AssemblyOperator.add, toSymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(toSymbol), Offset(toSymbol), Offset(fromSymbol));
      }
      else
      {
        Track fromTrack = LoadValueToRegister(fromSymbol);
        AddAssemblyCode(AssemblyOperator.mov, Base(toSymbol), Offset(toSymbol), fromTrack);
        m_trackMap.Remove(fromSymbol);
      }
    }

    public void GenerateIntegralGetReturnValue(MiddleCode middleCode)
    {
      Symbol returnSymbol = (Symbol)middleCode.GetOperand(0);
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, returnSymbol.Type.Size());
      CheckRegister(returnSymbol, returnRegister);
      Track returnTrack = new Track(returnSymbol, returnRegister);
      m_trackSet.Add(returnTrack);
      AddAssemblyCode(AssemblyOperator.empty, returnTrack);

      Track nextTrack = new Track(returnSymbol, returnRegister);
      nextTrack.PreviousTrack = returnTrack;
      nextTrack.FirstLine = m_assemblyCodeList.Count;
      AddAssemblyCode(AssemblyOperator.empty, nextTrack);
      AddAssemblyCode(AssemblyOperator.empty);
      m_trackMap[returnSymbol] = nextTrack;
    }

    public void GenerateIntegralSetReturnValue(MiddleCode middleCode)
    {
      Symbol returnSymbol = (Symbol)middleCode.GetOperand(1);
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, returnSymbol.Type.Size());
      LoadValueToRegister(returnSymbol, returnRegister);
      m_trackMap.Remove(returnSymbol);
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingBinaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinaryAdd, ObjectOperator.fadd);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinarySubtract, ObjectOperator.fsub);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedMultiply, ObjectOperator.fmul);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedDivide, ObjectOperator.fdiv);
    }*/

    public void GenerateFloatingBinary(MiddleCode middleCode)
    {
      Assert.Error((--m_floatStackSize) >= 0);
      AddAssemblyCode(m_middleToFloatingBinaryTargetMap[middleCode.Operator]);
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();

    static ObjectCodeGenerator() {
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, ObjectOperator.jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, ObjectOperator.je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, ObjectOperator.jbe);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, ObjectOperator.jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, ObjectOperator.jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, ObjectOperator.ja);
    }

    public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.fchs);
    }*/

    public void GenerateFloatingUnary(MiddleCode middleCode)
    {
      AddAssemblyCode(m_middleToFloatingUnaryTargetMap[middleCode.Operator]);
    }

    public void GenerateFloatingParameter(MiddleCode middleCode)
    {
      Symbol paramSymbol = (Symbol)middleCode.GetOperand(1);
      Symbol saveSymbol = new Symbol(paramSymbol.Type);
      saveSymbol.Offset = ((int)middleCode.GetOperand(2)) + m_totalRecordSize;
      TopPopSymbol(saveSymbol, TopOrPop.Pop);
    }

    public void GenerateStructUnionAssign(MiddleCode middleCode, int index)
    {
      Symbol targetSymbol = (Symbol)middleCode.GetOperand(0),
             sourceSymbol = (Symbol)middleCode.GetOperand(1);

      Track targetAddressTrack = LoadAddressToRegister(targetSymbol),
            sourceAddressTrack = LoadAddressToRegister(sourceSymbol);

      GenerateMemoryCopy(targetAddressTrack, sourceAddressTrack,
                         targetSymbol.Type.Size(), index);
    }

    public void GenerateStructUnionParameter(MiddleCode middleCode, int index)
    {
      Symbol sourceSymbol = (Symbol)middleCode.GetOperand(1);
      Symbol targetSymbol = new Symbol(Type.PointerTypeX);

      int paramOffset = ((int)middleCode.GetOperand(2)) + m_totalRecordSize;
      targetSymbol.Offset = paramOffset;

      Track sourceAddressTrack = LoadAddressToRegister(sourceSymbol);
      Track targetAddressTrack = LoadAddressToRegister(targetSymbol);

      GenerateMemoryCopy(targetAddressTrack, sourceAddressTrack,
                         sourceSymbol.Type.Size(), index);
    }

    public void GenerateStructUnionGetReturnValue(MiddleCode middleCode)
    {
      Symbol targetSymbol = (Symbol)middleCode.GetOperand(0);
      CheckRegister(targetSymbol, AssemblyCode.ReturnPointerRegister);
      Track targetAddressTrack =
        new Track(targetSymbol.AddressSymbol, AssemblyCode.ReturnPointerRegister);
      m_trackMap[targetSymbol.AddressSymbol] = targetAddressTrack;
    }

    public void GenerateStructUnionSetReturnValue(MiddleCode middleCode)
    {
      Symbol returnSymbol = (Symbol)middleCode.GetOperand(1);
      LoadAddressToRegister(returnSymbol, AssemblyCode.ReturnPointerRegister);
    }

    public void GenerateMemoryCopy(Track targetAddressTrack,
                                   Track sourceAddressTrack, int size, int index)
    {
      Type countType = (size < 256) ? Type.UnsignedCharType : Type.UnsignedIntegerType;
      Track countTrack = new Track(countType, null),
            valueTrack = new Track(Type.UnsignedCharType, null);

      AddAssemblyCode(AssemblyOperator.mov, countTrack, size);
      int labelIndex = m_assemblyCodeList.Count;
      AddAssemblyCode(AssemblyOperator.mov, valueTrack, sourceAddressTrack, 0);
      AddAssemblyCode(AssemblyOperator.mov, targetAddressTrack, 0, valueTrack);
      AddAssemblyCode(AssemblyOperator.inc, sourceAddressTrack);
      AddAssemblyCode(AssemblyOperator.inc, targetAddressTrack);
      AddAssemblyCode(AssemblyOperator.dec, countTrack);
      AddAssemblyCode(AssemblyOperator.cmp, countTrack, 0, null);
      AddAssemblyCode(AssemblyOperator.jne, null, labelIndex);
    }

    public static List<AssemblyCode> InitializationCodeList() {
      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      //AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment, "");
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment, "Initialize Stack Pointer", null, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, AssemblyCode.FrameRegister, LinkerWindows.StackTopName, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment, "Initialize Heap Pointer", null, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word, null, 65534, 65534);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment, "Initialize FPU Control Word, truncate mode => set bit 10 and 11.", null, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.fstcw, AssemblyCode.FrameRegister, 0, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.or_word, AssemblyCode.FrameRegister, 0, 0x0C00);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.fldcw, AssemblyCode.FrameRegister, 0, null);
      return assemblyCodeList;
    }

    public static List<AssemblyCode> ArgumentCodeList()
    {
      /*List<sbyte> byteList = new List<sbyte>();
      IDictionary<int,string> accessMap = new ListMap<int,string>();

      GenerateStaticInitializerWindows.GenerateByteList(Type.StringType, AssemblyCodeGenerator.PathText, byteList, accessMap);
      StaticSymbol staticSymbol = new StaticSymbol(AssemblyCodeGenerator.PathName, byteList, accessMap);
      SymbolTable.StaticSet.Add(staticSymbol);*/

      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.si, Register.bp, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word, Register.bp, 0, AssemblyCodeGenerator.PathName);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.add, Register.bp, 2, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.ax, 1, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.bx, 129, null);

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte, Register.bx, 0, 32);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.je, null, assemblyCodeList.Count + 5);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte, Register.bx, 0, 13);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.je, null, assemblyCodeList.Count + 17);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx, null, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.long_jmp, null, assemblyCodeList.Count - 6);

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp, Register.ax, 1, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.je, null, assemblyCodeList.Count + 2);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte, Register.bx, 0, 0);

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx, null, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte, Register.bx, 0, 32);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.je, null, assemblyCodeList.Count - 3);

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.bp, 0, Register.bx);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.add, Register.bp, 2, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.ax, null, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.long_jmp, null, assemblyCodeList.Count - 19);

      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte, Register.bx, 0, 0);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word, Register.bp, 0, 0);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.add, Register.bp, 2, null);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.bp, 6, Register.ax);
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.bp, 8, Register.si);
      return assemblyCodeList;
    }

    private static int ByteListSize(AssemblyCode objectCode)
    {
      if (objectCode.Operator == AssemblyOperator.empty)
      {
        return 0;
      }
      else if (objectCode.IsCallRegular() || (objectCode.Operator == AssemblyOperator.long_jmp))
      {
        return ((objectCode.GetOperand(0) is Track) ||
                (objectCode.GetOperand(0) is Register))
                ? AssemblyCode.ShortJumpSize : AssemblyCode.LongJumpSize;
      }
      else if (objectCode.IsRelation() || (objectCode.Operator == AssemblyOperator.short_jmp))
      {
        return AssemblyCode.RelationSize;
      }
      else
      {
        object x = objectCode.ByteList();

        if (x == null) {
          int i = 1;
        }

        return objectCode.ByteList().Count;
      }
    }

    /*public static IDictionary<ObjectOperator,ObjectOperator>
                    m_inverseMap = new Dictionary<ObjectOperator,ObjectOperator>();

    static ObjectCodeGenerator() {
      m_inverseMap.Add(ObjectOperator.je, ObjectOperator.jne);
      m_inverseMap.Add(ObjectOperator.jne, ObjectOperator.je);
      m_inverseMap.Add(ObjectOperator.jl, ObjectOperator.jge);
      m_inverseMap.Add(ObjectOperator.jle, ObjectOperator.jg);
      m_inverseMap.Add(ObjectOperator.jg, ObjectOperator.jle);
      m_inverseMap.Add(ObjectOperator.jge, ObjectOperator.jl);
      m_inverseMap.Add(ObjectOperator.ja, ObjectOperator.jbe);
      m_inverseMap.Add(ObjectOperator.jae, ObjectOperator.jb);
      m_inverseMap.Add(ObjectOperator.jb, ObjectOperator.jae);
      m_inverseMap.Add(ObjectOperator.jbe, ObjectOperator.ja);
    }*/

    private void GenerateJumpInfo()
    {
      foreach (AssemblyCode objectCode in m_assemblyCodeList)
      {
        if ((objectCode.IsRelation() || objectCode.IsJump()) &&
            (objectCode.GetOperand(2) is int))
        {
          int middleTarget = (int)objectCode.GetOperand(2);
          objectCode.SetOperand(1, m_middleToAssemblyMap[middleTarget]);
        }
      }

      while (true)
      {
        int byteSize = 0;
        m_assemblyToByteMap.Clear();
        for (int line = 0; line < m_assemblyCodeList.Count; ++line)
        {
          m_assemblyToByteMap.Add(line, byteSize);
          byteSize += ByteListSize(m_assemblyCodeList[line]);
        }
        m_assemblyToByteMap.Add(m_assemblyCodeList.Count, byteSize);

        bool update = false;
        for (int line = 0; line < (m_assemblyCodeList.Count - 1); ++line)
        {
          AssemblyCode thisCode = m_assemblyCodeList[line],
                     nextCode = m_assemblyCodeList[line + 1];

          if (thisCode.IsRelation() && nextCode.IsJump() &&
              (thisCode.GetOperand(1) is int) &&
              (nextCode.GetOperand(1) is int))
          {
            int thisTarget = (int)thisCode.GetOperand(1);
            Assert.Error(thisTarget == (line + 2), Message.This_target);

            int nextTarget = (int)nextCode.GetOperand(1);
            int toByteAddress = m_assemblyToByteMap[nextTarget];
            int forwardDistance = toByteAddress - m_assemblyToByteMap[line + 2];
            int backwardDistance = toByteAddress - m_assemblyToByteMap[line + 1];

            if ((backwardDistance >= -128) && (forwardDistance <= 127))
            {
              thisCode.Operator = m_inverseMap[thisCode.Operator];
              thisCode.SetOperand(1, nextTarget);
              thisCode.SetOperand(2, nextCode.GetOperand(2));
              nextCode.Operator = AssemblyOperator.empty;
              update = true;
              break;
            }
          }
          else if ((thisCode.IsRelation() || thisCode.IsJump()) &&
                   (thisCode.GetOperand(1) is int))
          {
            int thisTarget = (int)thisCode.GetOperand(1);

            int fromByteAddress = m_assemblyToByteMap[line + 1],
                toByteAddress = m_assemblyToByteMap[thisTarget];
            int byteDistance = toByteAddress - fromByteAddress;

            if (byteDistance == 0)
            {
              thisCode.Operator = AssemblyOperator.empty;
              update = true;
              break;
            }
            else if ((thisCode.Operator == AssemblyOperator.long_jmp) &&
                     (byteDistance >= -129) && (byteDistance <= 127))
            {
              thisCode.Operator = AssemblyOperator.short_jmp;
              update = true;
              break;
            }
          }
        }

        if (!update)
        {
          break;
        }
      }

      for (int line = 0; line < m_assemblyCodeList.Count; ++line)
      {
        AssemblyCode objectCode = m_assemblyCodeList[line];

        if ((objectCode.IsRelation() || objectCode.IsJump()) &&
            (objectCode.GetOperand(1) is int))
        {
          int objectTarget = (int)objectCode.GetOperand(1);
          int fromByteAddress = m_assemblyToByteMap[line + 1],
              toByteAddress = m_assemblyToByteMap[objectTarget];
          objectCode.SetOperand(0, toByteAddress - fromByteAddress);
        }
        else if (objectCode.Operator == AssemblyOperator.address_return)
        {
          int middleAddress = (int)objectCode.GetOperand(2);
          int objectAddress = m_middleToAssemblyMap[middleAddress];
          int byteAddress = m_assemblyToByteMap[objectAddress];
          int nextAddress = m_assemblyToByteMap[line + 1];
          int returnAddress = byteAddress - nextAddress + Type.PointerSize;
          objectCode.SetOperand(2, returnAddress);
        }
      }
    }

    private static int OperandSize(AssemblyOperator objectOp, object operand)
    {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);

      if (name.Contains("mov_"))
      {
        return AssemblyCode.OperatorSize(objectOp);
      }
      else if (name.Contains("cmp_"))
      {
        return Math.Max(1, ValueSize(operand));
      }
      else
      {
        return ValueSize(operand);
      }
    }

    private static int ValueSize(object operand)
    {
      return ValueSize(operand, false);
    }

    private static int ValueSize(object operand, bool isCompare)
    {
      if (operand is int)
      {
        if (isCompare && (((int)operand) == 0))
        {
          return 1;
        }

        return AssemblyCode.ValueSize((int)operand);
      }

      return 0;
    }

    private void GenerateTargetByteList(List<sbyte> byteList, IDictionary<int, string> accessMap,
                                        IDictionary<int, string> callMap, ISet<int> returnSet) {
      int lastSize = 0;
      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        AssemblyCode objectCode = m_assemblyCodeList[line];

        byteList.AddRange(objectCode.ByteList());
        int codeSize = byteList.Count - lastSize;
        lastSize = byteList.Count;

        AssemblyOperator objectOp = objectCode.Operator;

        if (!((objectOp == AssemblyOperator.empty) ||
              (objectOp == AssemblyOperator.label) ||
              (objectOp == AssemblyOperator.comment))) {
          object operand0 = objectCode.GetOperand(0),
                 operand1 = objectCode.GetOperand(1),
                 operand2 = objectCode.GetOperand(2);

          if ((objectCode.Operator == AssemblyOperator.call) &&
              (objectCode.GetOperand(0) is string)) {
            string calleeName = (string)objectCode.GetOperand(0);
            int address = byteList.Count - Type.PointerSize;
            callMap.Add(address, calleeName);
          }
          else if (objectCode.Operator == AssemblyOperator.address_return)
          {
            int address = byteList.Count - Type.PointerSize;
            returnSet.Add(address);
          }
          else if (operand0 is string)
          { // Add [g + 1], 2
            string name = (string)operand0;
            string nameX = Enum.GetName(typeof(AssemblyOperator), objectOp);
            bool isCompare = nameX.Contains("cmp");
            int size = (nameX.Contains("mov") && (operand2 is int)) ? Type.PointerSize : ValueSize(operand2, isCompare);
            int address = byteList.Count - Type.PointerSize - size;
            accessMap[address] = name;
            //WriteName(name);
          }
          else if (operand1 is string)
          {
            if (operand2 is int)
            { // mov ax, [g + 1]
              int size = OperandSize(objectOp, operand2);
              int address = byteList.Count /*- size*/ - Type.PointerSize;
              string name = (string)operand1;
              accessMap[address] = name;
              //WriteName(name);
            }
            else
            {
              int address = byteList.Count - Type.PointerSize; // mov ax, g
              string name = (string)operand1;
              accessMap[address] = name;
              //WriteName(name);
            }
          }
          else if (operand2 is string)
          { // Add [bp + 2], g
            string name = (string)operand2;
            int address = byteList.Count - Type.PointerSize;
            accessMap[address] = name;
            //WriteName(name);
          }
        }
      }
    }

    /*    public static void WriteName(string name) {
    //      if ((SymbolTable.CurrentFunction != null) &&
    //          SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
            if (name.StartsWith("0double")) {
              int i = 1;
            }

            name = name.Replace("\n", "\\n");
        //    Console.Out.WriteLine(name);
    //      }
        }*/
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