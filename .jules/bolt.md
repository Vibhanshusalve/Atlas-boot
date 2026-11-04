
## 2024-11-04 - EnumerationOptions Allocation during Recursive Traversal
**Learning:** Instantiating new `EnumerationOptions` within deeply nested recursive file system traversals (e.g., in `FileSystemCollector.cs`) causes measurable GC and allocation overhead.
**Action:** Always cache and reuse `EnumerationOptions` in static readonly fields when they are used repeatedly, especially inside loops or recursive directory enumeration methods.
