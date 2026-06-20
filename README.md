# 用于在Windows平台上将文件移动到系统回收站的C#库

这个库提供了一个类 `RecycleBinHelper` 通过 `P\Invoke` 的方式封装Win32API实现这个功能.

## 怎么用?

很简单,类 `RecycleBinHelper` 提供了两个静态方法 `MoveToRecycleBin(string filePath)` 和 `MoveToRecycleBin(FileInfo file)` 传入文件路径或 `FileInfo` 实例即可
