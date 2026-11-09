
## 2024-05-14 - FileSystemCollector Async I/O Overhead
**Learning:** Using `await WriteLineAsync` in a deep recursion for thousands of tiny iterations (like logging thousands of files) allocates an unnecessary amount of state machines and Tasks, slowing down fine-grained operations considerably.
**Action:** Use synchronous `WriteLine` with a larger `StreamWriter` buffer (e.g., 64KB) for high-frequency string logging when the I/O per log is tiny, and wrap it in a pseudo-async outer layer to satisfy interfaces without the tight-loop penalty.
