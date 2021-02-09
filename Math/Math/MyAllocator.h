template <typename T>
class MyAllocator : allocator<T> {
  public:
    MyAllocator() throw();

    template <typename U>
    MyAllocator(const MyAllocator<U>&) throw();
};