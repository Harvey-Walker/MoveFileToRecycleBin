# C# Library for Moving Files to the System Recycle Bin on Windows

This library provides a class `RecycleBinHelper` that wraps Win32 APIs via `P/Invoke` to achieve this functionality.

## How to Use?

It's simple. The `RecycleBinHelper` class provides two static methods: `MoveToRecycleBin(string filePath)` and `MoveToRecycleBin(FileInfo file)`. Just pass in a file path or a `FileInfo` instance.


