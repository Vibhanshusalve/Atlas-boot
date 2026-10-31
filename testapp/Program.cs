using System;
using System.IO;

class Program
{
    static void Main()
    {
        var e = new EnumerationOptions { IgnoreInaccessible = true };
        Console.WriteLine(e.AttributesToSkip);
    }
}
