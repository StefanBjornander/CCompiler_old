#include <iostream>
using namespace std;

const int f() {
  return 1;
}

void main(void) {
  int i = 1;
  int *const p = &i;

  int j = 2;
  int &q = j;

  double x = 1., y = .2, z = 1.e3, z2 = .3e3;
  cout << "Hello, World! " << *p << " " << q << endl;
}