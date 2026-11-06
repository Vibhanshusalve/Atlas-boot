
## 2024-05-19 - Cache EnumerationOptions in Recursive Methods
**Learning:** In C#, instantiating new `EnumerationOptions` objects during recursive directory traversal can cause significant GC pressure and performance bottlenecks when scanning large hierarchies like `C:\Users`.
**Action:** Always cache `EnumerationOptions` as `static readonly` fields when they are used within hot paths or recursive enumeration loops.
