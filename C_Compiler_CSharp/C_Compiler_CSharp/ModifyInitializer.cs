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
        IDictionary<int,int> dimensionToSizeMap = new Dictionary<int,int>();
        int maxDimension = DimensionToSizeMap(type, dimensionToSizeMap);
        IDictionary<object,int> initToDimensionMap =
          new Dictionary<object,int>();
        InitToDimensionMap(list, initToDimensionMap);

        // int a[2][2][2] = {1,2,3,4,5,6,7,8};
        // {{1,2],{3,4},{5,6},{7,8}}
        // {{{1,2],{3,4}},{{5,6},{7,8}}}

        for (int dimension = 1; dimension < maxDimension; ++dimension) {
          List<object> totalList =
            new List<object>(), currentList = new List<object>();
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

        return list;
      }

      return init;
    }

    private static int DimensionToSizeMap(Type type,
                                  IDictionary<int,int> dimensionToSizeMap) {
      if (type.IsArray()) {
        int dimension =
          DimensionToSizeMap(type.ArrayType, dimensionToSizeMap) + 1;
        dimensionToSizeMap[dimension] = type.ArraySize;
      }

      return 0;
    }

    private static int InitToDimensionMap(object init,
                             IDictionary<object,int> initToDimensionMap) {
      if (init is List<object>) {
        List<object> list = (List<object>) init;
        int maxDimension = 0;

        foreach (object member in list) {
          int dimension = InitToDimensionMap(member, initToDimensionMap);
          maxDimension = Math.Max(maxDimension, dimension);
        }

        initToDimensionMap[list] = maxDimension + 1;
        return maxDimension + 1;
      }

      return 0;
    }

    public static void PrintList(TextWriter textWriter, object init) {
      if (init is Expression) {
        textWriter.Write(Symbol.SimpleName((((Expression) init)).
                                           Symbol.UniqueName));
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
  }
}