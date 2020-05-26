using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Compiler_CSharp {
  class BigDecimal {
    bool m_minus;
    private decimal m_mantissa;
    private int m_exponent;

    public BigDecimal(string text) {
    }

    private BigDecimal(bool minus, decimal mantissa, int exponent) {
      m_minus = minus;
      m_mantissa = mantissa;
      m_exponent = exponent;
    }

    public override bool Equals(object obj) {
      if (obj is BigDecimal) {
        return (this == ((BigDecimal) obj));
      }

      return false;
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override String ToString() {
      return (m_minus ? "-" : "") + m_mantissa.ToString() + "e" + m_exponent.ToString();
    }

    public static BigDecimal operator+(BigDecimal bigDecimal) {
      return (new BigDecimal(bigDecimal.m_minus, bigDecimal.m_mantissa, bigDecimal.m_exponent));
    }

    public static BigDecimal operator-(BigDecimal bigDecimal) {
      return (new BigDecimal(!bigDecimal.m_minus, bigDecimal.m_mantissa, bigDecimal.m_exponent));
    }

    public static BigDecimal operator+(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      return null;
    }

    public static BigDecimal operator-(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      return null;
    }

    public static BigDecimal operator*(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      bool minus = (leftDecimal.m_minus && !rightDecimal.m_minus) ||
                   (!leftDecimal.m_minus && rightDecimal.m_minus);
      decimal mantissa = leftDecimal.m_mantissa * rightDecimal.m_mantissa;
      int exponent = leftDecimal.m_exponent + rightDecimal.m_exponent;

      BigDecimal resultDecimal = new BigDecimal(minus, mantissa, exponent);
      resultDecimal.Normalize();
      return resultDecimal;
    }

    public static BigDecimal operator/(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      bool minus = (leftDecimal.m_minus && !rightDecimal.m_minus) ||
                   (!leftDecimal.m_minus && rightDecimal.m_minus);
      decimal mantissa = leftDecimal.m_mantissa / rightDecimal.m_mantissa;
      int exponent = leftDecimal.m_exponent - rightDecimal.m_exponent;

      BigDecimal resultDecimal = new BigDecimal(minus, mantissa, exponent);
      resultDecimal.Normalize();
      return resultDecimal;
    }

    private void Normalize() {
      while (m_mantissa >= 1) {
        m_mantissa /= 2;
        ++m_exponent;
      }

      while (m_mantissa < ((decimal) 0.5)) {
        m_mantissa *= 2;
        --m_exponent;
      }
    }

    public static bool operator==(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      return (leftDecimal.m_minus == rightDecimal.m_minus) &&
             (leftDecimal.m_mantissa == rightDecimal.m_mantissa) &&
             (leftDecimal.m_exponent == rightDecimal.m_exponent);             
    }

    public static bool operator!=(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      return !(leftDecimal == rightDecimal);
    }

    public static bool operator<(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      if (leftDecimal.m_minus != rightDecimal.m_minus) {
        return leftDecimal.m_minus;
      }
      else if (leftDecimal.m_exponent != rightDecimal.m_exponent) {
        return leftDecimal.m_minus ? (leftDecimal.m_exponent > rightDecimal.m_exponent)
                                   : (leftDecimal.m_exponent < rightDecimal.m_exponent);
      }
      else if (leftDecimal.m_mantissa != rightDecimal.m_mantissa) {
        return leftDecimal.m_minus ? (leftDecimal.m_mantissa > rightDecimal.m_mantissa)
                                   : (leftDecimal.m_mantissa < rightDecimal.m_mantissa);
      }
      else {
        return false;
      }
    }

    public static bool operator<=(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      return (leftDecimal < rightDecimal) || (leftDecimal == rightDecimal);
    }

    public static bool operator>(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      return !(leftDecimal <= rightDecimal);
    }

    public static bool operator>=(BigDecimal leftDecimal, BigDecimal rightDecimal) {
      return !(leftDecimal < rightDecimal);
    }
  }
}
