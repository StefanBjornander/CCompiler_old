        if (rightTrack != null) {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), rightTrack);
          m_trackMap.Remove(rightSymbol);
        }
        else if (rightSymbol.Value is BigInteger) {
          BigInteger bigValue = (BigInteger) rightSymbol.Value;

          if ((-2147483648 <= bigValue) && (bigValue <= 2147483647)) {
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), rightSymbol.Value,
                            typeSize);
          }
          else {
            rightTrack = new Track(rightSymbol);
            AddAssemblyCode(AssemblyOperator.mov, rightTrack,
                            rightSymbol.Value);
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), rightTrack);
          }
        }
        else if (rightSymbol.Type.IsArrayFunctionOrString() ||
                 (rightSymbol.Value is StaticAddress)) {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), Base(rightSymbol),
                          typeSize);

          int offset = Offset(rightSymbol);
          if (offset != 0) {
            AddAssemblyCode(AssemblyOperator.add, Base(resultSymbol),
                            Offset(resultSymbol), (BigInteger) offset,
                            typeSize);
          }
        }
        else {
          rightTrack = LoadValueToRegister(rightSymbol);
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), rightTrack);
        }
      }
