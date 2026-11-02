## 2024-05-18 - Avoid allocations in recursive file system scans
**Learning:** During deep recursive directory enumerations in SystemAtlas, instantiating objects like `EnumerationOptions` inside the `EnumerateFiles` and `EnumerateDirectories` loops causes unnecessary heap allocations for every directory visited, which triggers the garbage collector more often.
**Action:** Extract reusable configurations into `static readonly` fields and pass them into the API instead to reduce GC pressure and overhead during large system scans.
