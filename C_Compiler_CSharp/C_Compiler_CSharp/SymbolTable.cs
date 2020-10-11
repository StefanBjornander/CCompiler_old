using System;
using System.Collections.Generic;

namespace CCompiler {
  public class SymbolTable {
    private Scope m_scope;

    public static int ReturnAddressOffset = 0;
    public static int RegularFrameOffset = TypeSize.PointerSize;
    public static int EllipseFrameOffset = 2 * TypeSize.PointerSize;
    public static int FunctionHeaderSize = 3 * TypeSize.PointerSize;

    private SymbolTable m_parentTable;
    private int m_currentOffset;

    private IDictionary<string,Symbol> m_entryMap = new ListMap<string,Symbol>();
    private IDictionary<string,Type> m_tagMap = new Dictionary<string,Type>();

    public static SymbolTable CurrentTable = null;
    public static Symbol CurrentFunction = null;
    public static ISet<StaticSymbol> StaticSet = new HashSet<StaticSymbol>();

    public SymbolTable(SymbolTable parentTable, Scope scope) {
      m_parentTable = parentTable;
    
      switch (m_scope = scope) {
        case Scope.Global:
          StaticSet = new HashSet<StaticSymbol>();
          break;

        case Scope.Struct:
        case Scope.Union:
          m_currentOffset = 0;
          break;

        case Scope.Function:
          m_currentOffset = FunctionHeaderSize;
          break;

        case Scope.Block:
          m_currentOffset = m_parentTable.m_currentOffset;
          break;
      }
    }

    public Scope Scope {
      get { return m_scope; }
      set { m_scope = value; }
    }

    public SymbolTable ParentTable {
      get { return m_parentTable; }
    }

    public IDictionary<string,Symbol> EntryMap {
      get { return m_entryMap; }
    }

    public int CurrentOffset {
      get { return m_currentOffset; }
    }

    public void AddSymbol(Symbol newSymbol) {
      string name = newSymbol.Name;

      if (name != null) {
        Symbol oldSymbol;

        if (m_entryMap.TryGetValue(name, out oldSymbol)) {
          if (oldSymbol.Type.IsFunction() && newSymbol.Type.IsFunction()) {
            Assert.Error(!oldSymbol.FunctionDefinition ||
                         !newSymbol.FunctionDefinition, name,
                         Message.Name_already_defined);
          }
          else {
            Assert.Error(oldSymbol.IsExtern() || newSymbol.IsExtern(),
                         name, Message.Name_already_defined);
          }

          Assert.Error(oldSymbol.Type.Equals(newSymbol.Type),
                       name, Message.Different_types_in_redeclaration);
        }

        m_entryMap[name] = newSymbol;
      }

      if (!newSymbol.Type.IsFunction()) {    
        if (newSymbol.IsAutoOrRegister()) {
          if (m_scope == Scope.Union) {
            newSymbol.Offset = 0;
          }
          else if (!newSymbol.Type.EnumeratorItem) {
            newSymbol.Offset = m_currentOffset;
            m_currentOffset += newSymbol.Type.Size();
          }
        }
      }
    }

    public void SetOffset(Symbol symbol) {
      symbol.Offset = m_currentOffset;
      m_currentOffset += symbol.Type.Size();
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