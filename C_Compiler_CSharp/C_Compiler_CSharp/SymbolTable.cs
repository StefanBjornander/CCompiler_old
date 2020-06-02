using System;
using System.Collections.Generic;

namespace CCompiler {
  public class SymbolTable {
    public static int ReturnAddressOffset;
    public static int RegularFrameOffset;
    public static int EllipseFrameOffset;
    public static int FunctionHeaderSize;

    private Scope m_scope;
    private SymbolTable m_parentTable;
    private int m_offset;

    private IDictionary<string,Symbol> m_entryMap = new ListMap<string,Symbol>();
    private IDictionary<string,Type> m_tagMap = new Dictionary<string,Type>();

    public static SymbolTable CurrentTable = null;
    public static ISet<StaticSymbol> StaticSet = new HashSet<StaticSymbol>();
    public static Symbol CurrentFunction = null;

    static SymbolTable() {
      ReturnAddressOffset = 0;
      RegularFrameOffset = TypeSize.PointerSize;
      EllipseFrameOffset = 2 * TypeSize.PointerSize;
      FunctionHeaderSize = 3 * TypeSize.PointerSize;
    }

    public SymbolTable(SymbolTable parentTable, Scope scope) {
      m_parentTable = parentTable;
    
      switch (m_scope = scope) {
        case Scope.Global:
          StaticSet = new HashSet<StaticSymbol>();
          break;

        case Scope.Struct:
        case Scope.Union:
          m_offset = 0;
          break;

        case Scope.Function:
          m_offset = FunctionHeaderSize;
          break;

        case Scope.Block:
          m_offset = m_parentTable.m_offset;
          break;
      }
    }

    public Scope Scope {
      get { return m_scope; }
    }

    public SymbolTable ParentTable {
      get { return m_parentTable; }
    }

    public IDictionary<string,Symbol> EntryMap {
      get { return m_entryMap; }
    }

    public int CurrentOffset {
      get { return m_offset; }
    }

    public void AddSymbol(Symbol newSymbol) {
      string name = newSymbol.Name;

      if (name != null) {
        Symbol oldSymbol;

        if (m_entryMap.TryGetValue(name, out oldSymbol)) {
          Assert.Error(oldSymbol.IsExtern() || newSymbol.IsExtern(),
                        name, Message.Name_already_defined);
          Assert.Error(oldSymbol.Type.Equals(newSymbol.Type),
                        name, Message.Different_types_in_redeclaration);
        }

        m_entryMap[name] = newSymbol;
      }
    
      if (newSymbol.IsAutoOrRegister()) {
        if (m_scope == Scope.Union) {
          newSymbol.Offset = 0;
        }
        else if (!newSymbol.Type.EnumeratorItem) {
          newSymbol.Offset = m_offset;
          m_offset += newSymbol.Type.Size();
        }
      }
    }

    public void SetOffset(Symbol symbol) {
      symbol.Offset = m_offset;
      m_offset += symbol.Type.Size();
    }

    public Symbol LookupSymbol(string name) {
      Symbol symbol;

      if (m_entryMap.TryGetValue(name, out symbol)) {
        return symbol;
      }
      else if (m_parentTable != null) {
        return m_parentTable.LookupSymbol(name);
      }

      return null;
    }

    public void AddTag(string name, Type newType) {
      if (m_tagMap.ContainsKey(name)) {
        Type oldType = m_tagMap[name];
        Assert.Error(!oldType.IsEnumerator() &&
                     (oldType.Sort == newType.Sort), name,
                     Message.Name_already_defined);

        if (oldType.MemberMap == null) {
          oldType.MemberMap = newType.MemberMap;
        }
        else {
          Assert.Error(newType.MemberMap == null, name,
                       Message.Name_already_defined);
        }
      }
      else {
        m_tagMap.Add(name, newType);
      }
    }

    public Type LookupTag(string name, Sort sort) {
      Type type;

      if (m_tagMap.TryGetValue(name, out type) && (type.Sort == sort)) {
        return type;
      }
      else if (m_parentTable != null) {
        return m_parentTable.LookupTag(name, sort);
      }

      return null;
    }
  }
}