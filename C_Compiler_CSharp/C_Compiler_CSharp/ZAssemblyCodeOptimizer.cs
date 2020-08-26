using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCodeOptimizerX {
    public List<AssemblyCode> m_assemblyCodeList;

    public AssemblyCodeOptimizerX(List<AssemblyCode> assemblyCodeList) {
      m_assemblyCodeList = assemblyCodeList;
    }

    public void Optimize() {
      foreach (AssemblyCode assemblyCode in m_assemblyCodeList) {
        AssemblyOperator operatorX = assemblyCode.Operator;
        object operand0 = assemblyCode[0],
               operand1 = assemblyCode[1],
               operand2 = assemblyCode[2];

        switch (operatorX) {
          case AssemblyOperator.add:
          case AssemblyOperator.sub:
            if ((operand0 is Register) && (operand1 is int) && (operand2 == null)) {
              int value = (int) operand1;

              if (((operatorX == AssemblyOperator.add) && (value == 1)) ||
                  ((operatorX == AssemblyOperator.sub) && (value == -1))) {
                assemblyCode.Operator = AssemblyOperator.inc;
                assemblyCode[1] = null;
              }
              else if (((operatorX == AssemblyOperator.add) && (value == -1)) ||
                       ((operatorX == AssemblyOperator.sub) && (value == 1))) {
                assemblyCode.Operator = AssemblyOperator.dec;
                assemblyCode[1] = null;
              }
            }
            else if ((operand0 is Register) && (operand1 is int) && (operand2 is int)) {
              int value = (int) operand2;

              if (((operatorX == AssemblyOperator.add) && (value == 1)) ||
                  ((operatorX == AssemblyOperator.sub) && (value == -1))) {
                assemblyCode.Operator = AssemblyOperator.inc;
                assemblyCode[2] = null;
              }
              else if (((operatorX == AssemblyOperator.add) && (value == -1)) ||
                       ((operatorX == AssemblyOperator.sub) && (value == 1))) {
                assemblyCode.Operator = AssemblyOperator.dec;
                assemblyCode[2] = null;
              }
            }
            break;

          case AssemblyOperator.jmp:
          case AssemblyOperator.je:
          case AssemblyOperator.jne:
          case AssemblyOperator.jc:
          case AssemblyOperator.jnc:
          case AssemblyOperator.jl:
          case AssemblyOperator.jle:
          case AssemblyOperator.jg:
          case AssemblyOperator.jge:
          case AssemblyOperator.jb:
          case AssemblyOperator.jbe:
          case AssemblyOperator.ja:
          case AssemblyOperator.jae: {
              if (operand0 is int) {
                int address = (int) operand0;

                if (address == 0) {
                  assemblyCode.Operator = AssemblyOperator.empty;
                  assemblyCode[0] = null;
                }
              }
            }
            break;
        }
      }
    }
  }
}