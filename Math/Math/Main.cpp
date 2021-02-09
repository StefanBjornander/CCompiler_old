#include <iostream>
#include <vector>
//#include <allocator>
using namespace std;

#include "MyAllocator.h"

const int f() {
  return 1;
}

void main(void) {
  //vector<int>(10, MyAllocator<int>());

  int i = 1;
  int *const p = &i;

  int j = 2;
  int &q = j;

  double x = 1., y = .2, z = 1.e3, z2 = .3e3;
  cout << "Hello, World! " << *p << " " << q << endl;
}

// func
//strlen

int strlenX(const char* text) {
  if (text == NULL) {
    return 0;
  }

  int size = 0;
  for (; *text != '\0'; text++) {
    size++;
  }

  return size;
}


int strlenY(const char* text) {
  if (text == NULL) {
    return 0;
  }

  char *p = (char*) text;
  for (; *p != 0; p += 1) {
    // Empty.
  }

  return p - text;
}