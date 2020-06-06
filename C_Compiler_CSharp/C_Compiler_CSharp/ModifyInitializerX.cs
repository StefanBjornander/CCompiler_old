using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  class ModifyInitializerX {
    private static IDictionary<Type,int> m_typeToDimensionMap = new Dictionary<Type,int>();
    private static IDictionary<int,int> m_dimensionToSizeMap = new Dictionary<int,int>();

    private static int GenerateDimensionMaps(Type type) {
      if (type.IsArray()) {
        int dimension = GenerateDimensionMaps(type.ArrayType) + 1;
        m_typeToDimensionMap[type] = dimension;
        m_dimensionToSizeMap[dimension] = type.ArraySize;
        return dimension;
      }
      else {
        m_typeToDimensionMap[type] = 0;
        m_dimensionToSizeMap[0] = 0;
        return 0;
      }
    }

    public static object DoInit(Type type, object init) {
      if (type.IsArray() && (init is List<object>)) {
        List<object> list = (List<object>) init;
        int maxDimension = GenerateDimensionMaps(type);
        IDictionary<object, int> initToDimensionMap = new Dictionary<object, int>();
        GenerateInitToDimensionlMap(list, initToDimensionMap);

        // int a[2][2][2] = {1,2,3,4,5,6,7,8};
        // {{1,2],{3,4},{5,6},{7,8}}
        // {{{1,2],{3,4}},{{5,6},{7,8}}}

        //List<object> list = (List<object>) init;
        for (int dimension = 1; dimension < maxDimension; ++dimension) {
          List<object> totalList = new List<object>(), currentList = new List<object>();
          int arraySize = m_dimensionToSizeMap[dimension];
          Assert.ErrorA(arraySize > 0);

          foreach (object member in list) {
            if (initToDimensionMap[member] < dimension) {
              currentList.Add(member);
            }
            else {
              if (currentList.Count > 0) {
                initToDimensionMap[currentList] = dimension;
                totalList.Add(currentList);
              }

              totalList.Add(member);
              currentList = new List<object>();
            }

            if (currentList.Count == arraySize) {
              initToDimensionMap[currentList] = dimension;
              totalList.Add(currentList);
              currentList = new List<object>();
            }
          }

          if (currentList.Count > 0) {
            initToDimensionMap[currentList] = dimension;
            totalList.Add(currentList);
          }

          list = totalList;
        }

        //PrintList(Console.Out, list);
        //Console.Out.WriteLine();
        return list;
      }

      return init;
    }

    private static int GenerateInitToDimensionlMap(object init, IDictionary<object,int> totalMap) {
      if (init is Expression) {
        Symbol symbol = ((Expression) init).Symbol;
        int dimension = m_typeToDimensionMap[symbol.Type];
        totalMap[init] = dimension;
        return dimension;
      }
      else {
        List<object> list = (List<object>) init;
        int maxDimension = 0;

        foreach (object member in list) {
          int dimension = GenerateInitToDimensionlMap(member, totalMap);
          maxDimension = Math.Max(maxDimension, dimension);
        }

        totalMap[list] = maxDimension + 1;
        return maxDimension + 1;
      }
    }

    public static void PrintList(TextWriter textWriter, object init) {
      if (init is Expression) {
        textWriter.Write(SimpleName((((Expression) init)).Symbol.UniqueName));
      }
      else {
        textWriter.Write("[");

        List<object> list = (List<object>) init;
        bool first = true;

        foreach (object member in list) {
          textWriter.Write(first ? "" : ",");
          PrintList(textWriter, member);
          first = false;
        }

        textWriter.Write("]");
      }
    }

    private static string SimpleName(string name) {
      int index = name.LastIndexOf(Symbol.SeparatorId);
      return ((index != -1) ? name.Substring(index + 1) : name).Replace(Symbol.NumberId, "");
    }
  }
}