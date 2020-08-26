using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class GenerateStaticInitializerWindowsX {
    /*public static void ByteListX(IList<AssemblyCode> assemblyCodeList, List<byte> byteList,
                                        IDictionary<int,string> accessMap) {
      foreach (AssemblyCode assemblyCode in assemblyCodeList) {
        switch (assemblyCode.Operator) {
          case AssemblyOperator.define_address: {
              string name = (string) assemblyCode[0];
              accessMap.Add(byteList.Count, name);
              int offset = (int) assemblyCode[1];
              AssemblyCode.LoadByteList(byteList, byteList.Count, TypeSize.PointerSize, (BigInteger) offset);
            }
            break;

          case AssemblyOperator.define_zero_sequence: {
              int size = (int) assemblyCode[0];
              byteList.AddRange(new byte[size]);
            }
            break;

          case AssemblyOperator.define_value: {
              Sort sort = (Sort) assemblyCode[0];
              object value = assemblyCode[1];

              if (sort == Sort.Pointer) {
                if (value is string) {
                  accessMap.Add(byteList.Count, (string) value);
                  AssemblyCode.LoadByteList(byteList, byteList.Count, TypeSize.PointerSize, (BigInteger) 0);
                }
                else if (value is StaticAddress) {
                  StaticAddress staticAddress = (StaticAddress) value;
                  accessMap.Add(byteList.Count, staticAddress.UniqueName);
                  int offset = staticAddress.Offset;
                  AssemblyCode.LoadByteList(byteList, byteList.Count, TypeSize.PointerSize, (BigInteger) offset);
                }
                else {
                  AssemblyCode.LoadByteList(byteList, byteList.Count, TypeSize.PointerSize, (BigInteger) value);
                }
              }
              else if (sort == Sort.Float) {
                float floatValue = (float) ((decimal) assemblyCode[0]);
                byteList.AddRange(BitConverter.GetBytes(floatValue));
              }
              else if ((sort == Sort.Double) || (sort == Sort.Long_Double)) {
                double doubleValue = (double) ((decimal) value);
                byteList.AddRange(BitConverter.GetBytes(doubleValue));
              }
              else if (sort == Sort.String) {
                string text = (string) value;

                foreach (char c in text) {
                  byteList.Add((byte) c);
                }

                byteList.Add((byte) 0);
              }
              else {
                AssemblyCode.LoadByteList(byteList, byteList.Count, Type.Size(sort), (BigInteger) value);
              }
              break;
          }
        }
      }
    }*/
  }
}