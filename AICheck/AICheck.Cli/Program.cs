using System;
using System.IO;
using AICheck.Core;

if (args.Length < 1)
{
    Console.WriteLine("Usage: review <path-to-script.cs>");
    return;
}

string scriptPath = args[0];

if (!File.Exists(scriptPath))
{
    Console.WriteLine($"❌ File not found: {scriptPath}");
    return;
}

try
{
    string sourceCode = File.ReadAllText(scriptPath);
    string formatted = ScriptAnalyzer.ProcessScript(sourceCode);

    File.WriteAllText(scriptPath, formatted);

    Console.WriteLine($"✅ Formatted: {scriptPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}