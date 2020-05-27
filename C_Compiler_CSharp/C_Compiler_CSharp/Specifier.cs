using System;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class Specifier {
    private Storage? m_storage = null;
    private Type m_type = null;
    private static IDictionary<int, Sort> m_maskToSortMap =
      new Dictionary<int, Sort>();
  
    static Specifier() {
      m_maskToSortMap.Add((int) Mask.Void, Sort.Void);
      m_maskToSortMap.Add((int) Mask.Char, Sort.Signed_Char);
      m_maskToSortMap.Add((int) Mask.SignedChar, Sort.Signed_Char);
      m_maskToSortMap.Add((int) Mask.UnsignedChar, Sort.Unsigned_Char);
      m_maskToSortMap.Add((int) Mask.Short, Sort.Signed_Short_Int);
      m_maskToSortMap.Add((int) Mask.ShortInt, Sort.Signed_Short_Int);
      m_maskToSortMap.Add((int) Mask.SignedShort, Sort.Signed_Short_Int);
      m_maskToSortMap.Add((int) Mask.SignedShortInt, Sort.Signed_Short_Int);
      m_maskToSortMap.Add((int) Mask.UnsignedShort, Sort.Unsigned_Short_Int);
      m_maskToSortMap.Add((int) Mask.UnsignedShortInt,
                          Sort.Unsigned_Short_Int);
      m_maskToSortMap.Add((int) Mask.Int, Sort.Signed_Int);
      m_maskToSortMap.Add((int) Mask.Signed, Sort.Signed_Int);
      m_maskToSortMap.Add((int) Mask.SignedInt, Sort.Signed_Int);
      m_maskToSortMap.Add((int) Mask.Unsigned, Sort.Unsigned_Int);
      m_maskToSortMap.Add((int) Mask.UnsignedInt, Sort.Unsigned_Int);
      m_maskToSortMap.Add((int) Mask.Long, Sort.Signed_Long_Int);
      m_maskToSortMap.Add((int) Mask.LongInt, Sort.Signed_Long_Int);
      m_maskToSortMap.Add((int) Mask.SignedLong, Sort.Signed_Long_Int);
      m_maskToSortMap.Add((int) Mask.SignedLongInt, Sort.Signed_Long_Int);
      m_maskToSortMap.Add((int) Mask.UnsignedLong, Sort.Unsigned_Long_Int);
      m_maskToSortMap.Add((int) Mask.UnsignedLongInt, Sort.Unsigned_Long_Int);
      m_maskToSortMap.Add((int) Mask.Float, Sort.Float);
      m_maskToSortMap.Add((int) Mask.Double, Sort.Double);
      m_maskToSortMap.Add((int) Mask.LongDouble, Sort.Long_Double);
    }

    public Specifier(Storage? storage, Type type) {
      m_storage = storage;
      m_type = type;
    }

    public CCompiler.Storage? Storage {
      get { return m_storage; }
    }
 
    public Type Type {
      get { return m_type; }
    }

    public static Specifier SpecifierList(List<object> specifierList) {
      int totalMaskValue = 0;
      Type compoundType = null;
    
      foreach (object obj in specifierList) {
        if (obj is Mask) {
          int maskValue = (int) obj;

          if ((maskValue & totalMaskValue) != 0) {
            Assert.Error(MaskToString(maskValue),
                         Message.Keyword_defined_twice);
          }

          totalMaskValue |= maskValue;
        }
        else {
          if (compoundType != null) {
            Assert.Error(MaskToString(totalMaskValue),
                         Message.Invalid_specifier_sequence);
          }

          compoundType = (Type) obj;
        }
      }

      Storage? storage = null;
      { int totalStorageValue = totalMaskValue & ((int) Mask.StorageMask);

        if (totalStorageValue != 0) {
          if (!Enum.IsDefined(typeof(Mask), totalStorageValue)) {
            Assert.Error(MaskToString(totalStorageValue),
                         Message.Invalid_specifier_sequence);
          }

          storage = (Storage) totalStorageValue;
        }
      }

      if ((compoundType != null) && (compoundType.EnumerationItemSet != null)){
        if (storage != null) {
          foreach (Pair<Symbol,bool> pair in compoundType.EnumerationItemSet){
            Symbol enumSymbol = pair.First;          
          
            switch (enumSymbol.Storage = storage.Value) {
              case CCompiler.Storage.Static:
                SymbolTable.StaticSet.Add(ConstantExpression.
                                          Value(enumSymbol));
                break;

              case CCompiler.Storage.Extern: {
                  bool enumInit = pair.Second;
                  Assert.Error(!enumInit,
                               enumSymbol + " = " + enumSymbol.Value,
                    Message.Extern_enumeration_item_cannot_be_initialized);
                }
                break;

              case CCompiler.Storage.Auto:
              case CCompiler.Storage.Register:
                SymbolTable.CurrentTable.SetOffset(enumSymbol);
                break;
            }
          }
        }
        else {
          foreach (Pair<Symbol,bool> pair in compoundType.EnumerationItemSet) {
            Symbol enumSymbol = pair.First;          
          
            switch (SymbolTable.CurrentTable.Scope) {
              case Scope.Global:
                enumSymbol.Storage = CCompiler.Storage.Static;
                SymbolTable.StaticSet.Add(ConstantExpression.
                                          Value(enumSymbol));
                break;

              default:
                enumSymbol.Storage = CCompiler.Storage.Auto;
                SymbolTable.CurrentTable.SetOffset(enumSymbol);
                break;
            }
          }
        }
      }

      { bool isConstant = (totalMaskValue & ((int) Mask.Constant)) != 0;
        bool isVolatile = (totalMaskValue & ((int) Mask.Volatile)) != 0;
    
        if ((isConstant || isVolatile) && (compoundType != null) &&
             compoundType.IsStructOrUnion() /*&& compoundType.HasTag()*/) {
          compoundType = new Type(compoundType.Sort, compoundType.MemberMap);
        }

        Sort? sort = null;
        int sortMaskValue = totalMaskValue & ((int) Mask.SortMask);

        if (sortMaskValue != 0) {
          if (!m_maskToSortMap.ContainsKey(sortMaskValue)) {
            Assert.Error(MaskToString(sortMaskValue),
                         Message.Invalid_specifier_sequence);
          }

          sort = m_maskToSortMap[sortMaskValue];
        }

        Type type = null;
        if ((compoundType != null) && (sort == null)) {
          compoundType.IsConstant = (compoundType.IsConstant || isConstant);
          compoundType.Volatile = (compoundType.Volatile || isVolatile);
          type = compoundType;
        }
        else if ((compoundType == null) && (sort != null)) {
          type = new Type(sort.Value);
          type.IsConstant = isConstant;
          type.Volatile = isVolatile;
        }
        else if ((compoundType == null) && (sort == null)) {
          type = new Type(Sort.Signed_Int);
          type.IsConstant = isConstant;
          type.Volatile = isVolatile;
        }
        else {
          Assert.Error(MaskToString((int) sortMaskValue), Message.
                             Invalid_specifier_sequence_together_with_type);
        }

        return (new Specifier(storage, type));
      }
    }
 
    public static Type QualifierList(List<Mask> qualifierList) {
      int totalMaskValue = 0;
    
      foreach (Mask mask in qualifierList) {
        int maskValue = (int) mask;

        if ((maskValue & totalMaskValue) != 0) {
          Assert.Error(MaskToString(maskValue),
                       Message.Keyword_defined_twice);
        }

        totalMaskValue |= maskValue;
      }

      Type type = Type.VoidPointerType;
      type.IsConstant = (totalMaskValue & ((int) Mask.Constant)) != 0;
      type.IsVolatile = (totalMaskValue & ((int) Mask.Volatile)) != 0;
      return type;
    }
 
    private static string MaskToString(int totalMaskValue) {
      StringBuilder maskBuffer = new StringBuilder();

      for (int maskValue = 1; maskValue != 0; maskValue <<= 1) {
        if ((maskValue & totalMaskValue) != 0) {
          string maskName = Enum.GetName(typeof(Mask), maskValue).ToLower();
          maskBuffer.Append(((maskBuffer.Length > 0) ? " " : "") + maskName);
        }
      }

      return maskBuffer.ToString();
    }
  }
}