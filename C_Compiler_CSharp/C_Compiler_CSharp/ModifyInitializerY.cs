using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  class ModifyInitializer {
    public static object DoInit(Type type, object init) {
      if (type.IsArray() && (init is List<object>)) {
        List<object> list = (List<object>) init;
        IDictionary<Type,int> typeToDimensionMap = new Dictionary<Type,int>();
        int maxDimension = GenerateTypeToDimensionMap(type, typeToDimensionMap);
        IDictionary<int,int> dimensionToSizeMap = new Dictionary<int,int>();
        GenerateDimensionToSizeMap(type, typeToDimensionMap, dimensionToSizeMap);
        IDictionary<object,int> initToDimensionMap = new Dictionary<object,int>();
        GenerateInitToDimensionlMap(list, typeToDimensionMap, initToDimensionMap);

        // int a[2][2][2] = {1,2,3,4,5,6,7,8};
        // {{1,2],{3,4},{5,6},{7,8}}
        // {{{1,2],{3,4}},{{5,6},{7,8}}}

        //List<object> list = (List<object>) init;
        for (int dimension = 1; dimension < maxDimension; ++dimension) {
          List<object> totalList = new List<object>(), currentList = new List<object>();
          int arraySize = dimensionToSizeMap[dimension];
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

    private static int GenerateTypeToDimensionMap(Type type, IDictionary<Type,int> typeToDimensionMap) {
      if (type.IsArray()) {
        int dimension = GenerateTypeToDimensionMap(type.ArrayType, typeToDimensionMap) + 1;
        typeToDimensionMap[type] = dimension;
        return dimension;
      }
      else {
        typeToDimensionMap[type] = 0;
        return 0;
      }
    }

    private static void GenerateDimensionToSizeMap(Type type, IDictionary<Type,int> typeToDimensionMap,
                                                   IDictionary<int,int> dimensionToSizeMap) {
      if (type.IsArray()) {
        int dimension = typeToDimensionMap[type];
        dimensionToSizeMap[dimension] = type.ArraySize;
        GenerateDimensionToSizeMap(type.ArrayType, typeToDimensionMap, dimensionToSizeMap);
      }
    }

    private static int GenerateInitToDimensionlMap(object init, IDictionary<Type,int> typeToDimensionMap,
                                                   IDictionary<object, int> totalMap)
    {
      if (init is Expression) {
        Symbol symbol = ((Expression) init).Symbol;
        Assert.ErrorA(symbol.Type.Dimension == -1);

        int dimension;
        if (typeToDimensionMap.ContainsKey(symbol.Type)) {
          dimension = typeToDimensionMap[symbol.Type];
        }
        else {
          dimension = 0;
        }

        totalMap[init] = dimension;
        return dimension;
      }
      else {
        List<object> list = (List<object>) init;
        int maxDimension = 0;

        foreach (object member in list) {
          int dimension = GenerateInitToDimensionlMap(member, typeToDimensionMap, totalMap);
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