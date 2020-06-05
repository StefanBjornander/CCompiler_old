using System;
using System.Text;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class GenerateStaticInitializerLinux {
    /*public static void TextList(IList<AssemblyCode> assemblyCodeList, IList<string> textList,
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
    }*/
  }
}