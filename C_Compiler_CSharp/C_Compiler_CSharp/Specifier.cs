using System;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class Specifier {
    private bool m_externalLinkage;
    private Storage? m_storage;
    private Type m_type;

    private static IDictionary<int, Sort> m_maskToSortMap =
      new Dictionary<int, Sort>() {  
        {(int) Mask.Void, Sort.Void},
        {(int) Mask.Char, Sort.Signed_Char},
        {(int) Mask.SignedChar, Sort.Signed_Char},
        {(int) Mask.UnsignedChar, Sort.Unsigned_Char},
        {(int) Mask.Short, Sort.Signed_Short_Int},
        {(int) Mask.ShortInt, Sort.Signed_Short_Int},
        {(int) Mask.SignedShort, Sort.Signed_Short_Int},
        {(int) Mask.SignedShortInt, Sort.Signed_Short_Int},
        {(int) Mask.UnsignedShort, Sort.Unsigned_Short_Int},
        {(int) Mask.UnsignedShortInt, Sort.Unsigned_Short_Int},
        {(int) Mask.Int, Sort.Signed_Int},
        {(int) Mask.Signed, Sort.Signed_Int},
        {(int) Mask.SignedInt, Sort.Signed_Int},
        {(int) Mask.Unsigned, Sort.Unsigned_Int},
        {(int) Mask.UnsignedInt, Sort.Unsigned_Int},
        {(int) Mask.Long, Sort.Signed_Long_Int},
        {(int) Mask.LongInt, Sort.Signed_Long_Int},
        {(int) Mask.SignedLong, Sort.Signed_Long_Int},
        {(int) Mask.SignedLongInt, Sort.Signed_Long_Int},
        {(int) Mask.UnsignedLong, Sort.Unsigned_Long_Int},
        {(int) Mask.UnsignedLongInt, Sort.Unsigned_Long_Int},
        {(int) Mask.Float, Sort.Float},
        {(int) Mask.Double, Sort.Double},
        {(int) Mask.LongDouble, Sort.Long_Double}
      };

    public Specifier(bool externalLinkage, Storage? storage, Type type) {
      m_externalLinkage = externalLinkage;
      m_storage = storage;
      m_type = type;
    }

    public bool ExternalLinkage {
      get { return m_externalLinkage; }
    }
 
    public CCompiler.Storage? StorageX {
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
      bool externalLinkage = (SymbolTable.CurrentTable.Scope == Scope.Global)
                             && (CCompiler_Main.Parser.CallDepth == 0) &&
                             ((storage == null) ||
                              (storage == CCompiler.Storage.Extern));

      if (CCompiler_Main.Parser.CallDepth > 0) {
        Assert.Error((storage == null) || (storage == Storage.Auto) || 
                     (storage == Storage.Register), storage, Message.
          Only_auto_or_register_storage_allowed_in_parameter_declaration);
      }
      else if ((SymbolTable.CurrentTable.Scope == Scope.Struct) ||
               (SymbolTable.CurrentTable.Scope == Scope.Union)) {
          Assert.Error((storage == null) || (storage == Storage.Auto) ||
                       (storage == Storage.Register), storage, Message.
            Only_auto_or_register_storage_allowed_for_struct_or_union_scope);  
      }
      else if (SymbolTable.CurrentTable.Scope == Scope.Global) {
          Assert.Error((storage == null) || (storage == Storage.Extern) ||
                       (storage == Storage.Static) ||
                      (storage == Storage.Typedef), storage, Message.
        Only_extern____static____or_typedef_storage_allowed_in_global_scope);
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
                  bool enumInitializer = pair.Second;
                  Assert.Error(!enumInitializer,
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
          compoundType.Constant = (compoundType.Constant || isConstant);
          compoundType.Volatile = (compoundType.Volatile || isVolatile);
          type = compoundType;
        }
        else if ((compoundType == null) && (sort != null)) {
          type = new Type(sort.Value);
          type.Constant = isConstant;
          type.Volatile = isVolatile;
        }
        else if ((compoundType == null) && (sort == null)) {
          type = new Type(Sort.Signed_Int);
          type.Constant = isConstant;
          type.Volatile = isVolatile;
        }
        else {
          Assert.Error(MaskToString((int) sortMaskValue), Message.
                             Invalid_specifier_sequence_together_with_type);
        }

        return (new Specifier(externalLinkage, storage, type));
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
      type.Constant = (totalMaskValue & ((int) Mask.Constant)) != 0;
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
