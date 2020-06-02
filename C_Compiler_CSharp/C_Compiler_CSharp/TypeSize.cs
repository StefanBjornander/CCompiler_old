using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCompiler {
  class TypeSize {
    public static int PointerSize;
    public static int SignedIntegerSize;

    public static IDictionary<Sort,int>
      m_sizeMap = new Dictionary<Sort,int>();
    public static IDictionary<int,Type>
      m_signedMap = new Dictionary<int,Type>(),
      m_unsignedMap = new Dictionary<int,Type>();
    private static IDictionary<int, BigInteger>
      m_maskMap = new Dictionary<int, BigInteger>();
    private static IDictionary<Sort,BigInteger>
      m_minValueMap = new Dictionary<Sort,BigInteger>(),
      m_maxValueMap = new Dictionary<Sort,BigInteger>();
    private static IDictionary<Sort,decimal>
      m_minValueFloatMap = new Dictionary<Sort,decimal>(),
      m_maxValueFloatMap = new Dictionary<Sort,decimal>();
  
    static TypeSize() {
      m_maskMap.Add(1, (BigInteger) 0x000000FF);
      m_maskMap.Add(2, (BigInteger) 0x0000FFFF);
      m_maskMap.Add(4, (BigInteger) 0xFFFFFFFF);

      if (Start.Windows) {
        PointerSize = 2;
        SignedIntegerSize = 2;

        m_sizeMap.Add(Sort.Void, 0);
        m_sizeMap.Add(Sort.Function, 0);
        m_sizeMap.Add(Sort.Logical, 1);
        m_sizeMap.Add(Sort.Array, 2);
        m_sizeMap.Add(Sort.Pointer, 2);
        m_sizeMap.Add(Sort.String, 2);
        m_sizeMap.Add(Sort.Signed_Char, 1);
        m_sizeMap.Add(Sort.Unsigned_Char, 1);
        m_sizeMap.Add(Sort.Signed_Short_Int, 1);
        m_sizeMap.Add(Sort.Unsigned_Short_Int, 1);
        m_sizeMap.Add(Sort.Signed_Int, 2);
        m_sizeMap.Add(Sort.Unsigned_Int, 2);
        m_sizeMap.Add(Sort.Signed_Long_Int, 4);
        m_sizeMap.Add(Sort.Unsigned_Long_Int, 4);
        m_sizeMap.Add(Sort.Float, 4);
        m_sizeMap.Add(Sort.Double, 8);
        m_sizeMap.Add(Sort.Long_Double, 8);

        m_signedMap.Add(1, Type.SignedCharType);
        m_signedMap.Add(2, Type.SignedIntegerType);
        m_signedMap.Add(4, Type.SignedLongIntegerType);

        m_unsignedMap.Add(1, Type.UnsignedCharType);
        m_unsignedMap.Add(2, Type.UnsignedIntegerType);
        m_unsignedMap.Add(4, Type.UnsignedLongIntegerType);

        m_minValueMap.Add(Sort.Logical, 0);
        m_minValueMap.Add(Sort.Signed_Char, -128);
        m_minValueMap.Add(Sort.Unsigned_Char, 0);
        m_minValueMap.Add(Sort.Signed_Short_Int, -128);
        m_minValueMap.Add(Sort.Unsigned_Short_Int, 0);
        m_minValueMap.Add(Sort.Signed_Int, -32768);
        m_minValueMap.Add(Sort.Unsigned_Int, 0);
        m_minValueMap.Add(Sort.Array, 0);
        m_minValueMap.Add(Sort.Pointer, 0);
        m_minValueMap.Add(Sort.Signed_Long_Int, -2147483648);
        m_minValueMap.Add(Sort.Unsigned_Long_Int, 0);

        m_maxValueMap.Add(Sort.Logical, 1);
        m_maxValueMap.Add(Sort.Signed_Char, 127);
        m_maxValueMap.Add(Sort.Unsigned_Char, 255);
        m_maxValueMap.Add(Sort.Signed_Short_Int, 127);
        m_maxValueMap.Add(Sort.Unsigned_Short_Int, 255);
        m_maxValueMap.Add(Sort.Signed_Int, 32767);
        m_maxValueMap.Add(Sort.Unsigned_Int, 65535);
        m_maxValueMap.Add(Sort.Array, 65535);
        m_maxValueMap.Add(Sort.Pointer, 65535);
        m_maxValueMap.Add(Sort.Signed_Long_Int, 2147483647);
        m_maxValueMap.Add(Sort.Unsigned_Long_Int, 4294967295);

        /*m_minValueFloatMap.Add(Sort.Float, decimal.
                                 Parse("1.2E-38", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));

        m_maxValueFloatMap.Add(Sort.Float, decimal.
                               Parse("3.4E+38", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));*/

        /*m_maskMap.Add(Sort.Unsigned_Char, 0x00000000000000FF);
        m_maskMap.Add(Sort.Unsigned_Short_Int, 0x00000000000000FF);
        m_maskMap.Add(Sort.Unsigned_Int, 0x000000000000FFFF);
        m_maskMap.Add(Sort.Unsigned_Long_Int, 0x00000000FFFFFFFF);*/
      }
      
      if (Start.Linux) {
        PointerSize = 8;
        SignedIntegerSize = 4;

        m_sizeMap.Add(Sort.Void, 0);
        m_sizeMap.Add(Sort.Function, 0);
        m_sizeMap.Add(Sort.Logical, 1);
        m_sizeMap.Add(Sort.Pointer, 8);
        m_sizeMap.Add(Sort.Array, 8);
        m_sizeMap.Add(Sort.String, 4);
        m_sizeMap.Add(Sort.Signed_Char, 1);
        m_sizeMap.Add(Sort.Unsigned_Char, 1);
        m_sizeMap.Add(Sort.Signed_Short_Int, 2);
        m_sizeMap.Add(Sort.Unsigned_Short_Int, 2);
        m_sizeMap.Add(Sort.Signed_Int, 4);
        m_sizeMap.Add(Sort.Unsigned_Int, 4);
        m_sizeMap.Add(Sort.Signed_Long_Int, 8);
        m_sizeMap.Add(Sort.Unsigned_Long_Int, 8);
        m_sizeMap.Add(Sort.Float, 4);
        m_sizeMap.Add(Sort.Double, 8);
        m_sizeMap.Add(Sort.Long_Double, 8);

        m_signedMap.Add(1, Type.SignedCharType);
        m_signedMap.Add(2, Type.SignedShortIntegerType);
        m_signedMap.Add(4, Type.SignedIntegerType);
        m_signedMap.Add(8, Type.SignedLongIntegerType);

        m_unsignedMap.Add(1, Type.UnsignedCharType);
        m_unsignedMap.Add(2, Type.UnsignedShortIntegerType);
        m_unsignedMap.Add(4, Type.UnsignedIntegerType);
        m_unsignedMap.Add(8, Type.UnsignedLongIntegerType);

        m_minValueMap.Add(Sort.Logical, 0);
        m_minValueMap.Add(Sort.Signed_Char, -128);
        m_minValueMap.Add(Sort.Unsigned_Char, 0);
        m_minValueMap.Add(Sort.Signed_Short_Int, -32768);
        m_minValueMap.Add(Sort.Unsigned_Short_Int, 0);
        m_minValueMap.Add(Sort.Signed_Int, -2147483648);
        m_minValueMap.Add(Sort.Unsigned_Int, 0);
        m_minValueMap.Add(Sort.Array, 0);
        m_minValueMap.Add(Sort.Pointer, 0);
        m_minValueMap.Add(Sort.Signed_Long_Int, -9223372036854775808);
        m_minValueMap.Add(Sort.Unsigned_Long_Int, 0);

        m_maxValueMap.Add(Sort.Logical, 1);
        m_maxValueMap.Add(Sort.Signed_Char, 127);
        m_maxValueMap.Add(Sort.Unsigned_Char, 255);
        m_maxValueMap.Add(Sort.Signed_Short_Int, 32767);
        m_maxValueMap.Add(Sort.Unsigned_Short_Int, 65535);
        m_maxValueMap.Add(Sort.Signed_Int, 2147483647);
        m_maxValueMap.Add(Sort.Unsigned_Int, 4294967295);
        m_maxValueMap.Add(Sort.Array, 4294967295);
        m_maxValueMap.Add(Sort.Pointer, 4294967295);
        m_maxValueMap.Add(Sort.Signed_Long_Int, 9223372036854775807);
        m_maxValueMap.Add(Sort.Unsigned_Long_Int, 18446744073709551615);

        /*m_minValueFloatMap.Add(Sort.Float, decimal.
                                 Parse("1.2E-38", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));

        m_maxValueFloatMap.Add(Sort.Float, decimal.
                               Parse("3.4E+38", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));*/

/*        m_maskMap.Add(Sort.Unsigned_Char, 0x00000000000000FF);
                        m_maskMap.Add(Sort.Unsigned_Short_Int,
                                      0x00000000000000FF);
                        m_maskMap.Add(Sort.Unsigned_Int, 0x000000000000FFFF);
                        m_maskMap.Add(Sort.Unsigned_Long_Int,
                                      0x0FFFFFFFFFFFFFFF);*/
      }
    }

    public static BigInteger GetMinValue(Sort sort) {
      return m_minValueMap[sort];
    }

    public static BigInteger GetMaxValue(Sort sort) {
      return m_maxValueMap[sort];
    }

    public static BigInteger GetMask(Sort sort) {
      return m_maskMap[m_sizeMap[sort]];
    }

    public static Type SizeToSignedType(int size) {
      return m_signedMap[size];
    }

    public static Type SizeToUnsignedType(int size) {
      return m_unsignedMap[size];
    }

    public static int Size(Sort sort) {
      return m_sizeMap[sort];
    }
  }
}
