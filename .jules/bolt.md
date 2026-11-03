## 2024-05-24 - Cached EnumerationOptions for Recursive Traversal
**Learning:** Instantiating `EnumerationOptions` inside a recursive directory traversal function (like `EnumerateDirectoryAsync`) causes massive memory allocations and GC pressure because two new objects are created for every single directory on the filesystem.
**Action:** Always cache `EnumerationOptions` as static readonly fields when performing recursive file system traversals.
