using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCompiler {
  public abstract class Linker {
    public abstract void Add(StaticSymbol staticSymbol);
    public abstract void Generate();
  }
}
