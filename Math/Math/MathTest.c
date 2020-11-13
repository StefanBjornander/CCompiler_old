#include <math.h>
#include <time.h>
#include <stdio.h>
#include <ErrNo.h>
#include <String.h>

#define PI    3.1415926535897932384626433

#define PRINT(f) { printf("  " #f "(%f) = %f, errno = %i\n", x, f(x), errno); errno = 0; }
#define PRINT2(f) { printf(#f "(%f, %f) = %f, errno = %i\n", x, y, f(x, y), errno); errno = 0;}
//  if (errno != NO_ERROR) { perror("Error message"); errno = 0; }}
//  if (errno != NO_ERROR) { perror("Error message"); errno = 0; }}

void math_test_1(double x);
void math_test_2(double x, double y);
void math_test_int(double x, int i);

void math_test_1x() {
  math_test_1(-2 * PI);
  math_test_1(-PI);
  math_test_1(-PI / 2);
  math_test_1(-1);
  math_test_1(0);
  math_test_1(1);
  math_test_1(PI / 2);
  math_test_1(PI);
  math_test_1(2 * PI);
}

void acos_test(double x) {
  PRINT(acos);
}

void atan_test(double x) {
  PRINT(atan);
}

void main_math(void) {
  math_test_1(0.333333333);
  math_test_1(0.75);
  math_test_1(1.000010);
  math_test_1(1.0);
  math_test_1(0.999999);

  math_test_1(0.000010);
  math_test_1(0);
  math_test_1(-0.000010);

  math_test_1(-0.999999);
  math_test_1(-1.0);
  math_test_1(-1.000010);

  math_test_1(2 * PI);
  math_test_1(PI);
  math_test_1(PI / 2);

  math_test_1(-PI / 2);
  math_test_1(-PI);
  math_test_1(-2 * PI);

  math_test_2(1.0, 2.0);
  math_test_2(3.0, 4.0);
  math_test_2(0, 2.0);
  math_test_2(0, -2.0);
  math_test_2(1, 2.0);
  math_test_2(1, -2.0);
  math_test_2(0, 0);
  math_test_2(2, 0);
  math_test_2(-2, 0);
  math_test_2(-1.0, -1.0);
  math_test_2(-2.0, -4.0);
}

void math_test_1(double x) {
  printf("<%f>\n", x);
  PRINT(sin);
  PRINT(cos);
  PRINT(tan);

  PRINT(asin);
  PRINT(acos);
  PRINT(atan);

  PRINT(exp);
  PRINT(log);
  PRINT(log10);

  PRINT(sinh);
  PRINT(cosh);
  PRINT(tanh);

  PRINT(sqrt);
  PRINT(floor);
  PRINT(ceil);
  PRINT(fabs);

  { int j = 0;
    double z = frexp(x, &j);
    printf("frexp (%f, p) = (%f, %i), errno = %i\n", x, z, j, errno);
    if (errno != 0) { perror("Error message:"); errno = 0; }
  }

  { double w = 0;
    double z = modf(x, &w);
    printf("modf (%f, p) = (%f, %f), errno = %i\n", x, z, w, errno);
    if (errno != 0) { perror("Error message:"); errno = 0; }
  }

  printf("\n");
}

void math_test_2(double x, double y) {
  PRINT2(fmod);
  PRINT2(atan2);
  PRINT2(pow);
  printf("\n");
  printf("ldexp(%f, %i) = %f\n\n", x, (int) y, ldexp(x, (int) y));
}

void main() {
  time_t t = time(NULL);
  printf("%lu\n", (unsigned long) t);
  struct tm s;
  localtime_s(&s, &t);
  printf("%02i-%02i-%02i %02i:%02i:%02i\n", s.tm_year + 1900, s.tm_mon, s.tm_mday,
                                            s.tm_hour, s.tm_min, s.tm_sec);
  //main_math();
}
