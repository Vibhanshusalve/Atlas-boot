
## 2023-11-07 - FileSystem Enumeration Optimization
**Learning:** In .NET, enumerating a directory by calling `EnumerateFiles` followed by `EnumerateDirectories` iterates over the underlying OS directory entries twice, causing redundant I/O overhead.
**Action:** Use `EnumerateFileSystemInfos` to retrieve both files and directories in a single pass, cutting I/O iteration time by up to 50% for recursive file system scanners.
