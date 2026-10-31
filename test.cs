using System;
using System.IO;

class Program
{
    static void Main()
    {
        var options = new EnumerationOptions();
        Console.WriteLine($"Default AttributesToSkip: {options.AttributesToSkip}");
    }
}
