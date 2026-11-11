## 2026-11-11 - .NET EnumerationOptions GC Pressure
**Learning:** Instantiating new `EnumerationOptions` objects inside recursive directory traversal loops in .NET causes significant GC pressure, as it's a reference type (class) in .NET.
**Action:** Extract `EnumerationOptions` instantiations to `static readonly` fields when used repeatedly without mutation in loops or recursive methods.
