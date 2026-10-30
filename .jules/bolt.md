## 2024-06-25 - Reducing EnumerationOptions allocations
**Learning:** During recursive file system enumeration in .NET using methods like `EnumerateDirectories` and `EnumerateFiles`, passing inline instantiated `EnumerationOptions` inside loops results in heavy allocations (one per directory layer). In deep filesystem traversals like a full disk scan, this puts severe, unnecessary pressure on the GC.
**Action:** Extract `EnumerationOptions` into static readonly fields at the class level when they do not change dynamically.
