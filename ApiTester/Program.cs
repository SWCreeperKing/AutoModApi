//https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md

using System.Diagnostics;
using ApiTester;
using AutoModApi;

var sw = new Stopwatch();
sw.Start();

Api.projectName = "Testing Project";
Api.ReadDir("Scripts");
Api.Initialize("Scripts");

// loading
var i = 0;
Api.Compile();

while (Api.CompilationPercent != 1)
{
    i++;
    Task.Delay(150).GetAwaiter().GetResult();
    Console.SetCursorPosition(0, 0);
    Console.WriteLine(
        $"Compilation Progress: [{Api.CompilationPercent:000.00%}]\nCompiling Please Wait [{".".Repeat(i)}{" ".Repeat(3 - i)}]    ");
    i %= 3;
}

Console.SetCursorPosition(0, 1);
Console.WriteLine("                                                   ");
Console.SetCursorPosition(0, 1);

// main test
sw.Stop();
Console.WriteLine($"Compile Time: [{sw.Elapsed}]");

sw.Reset();
sw.Start();

var testItem1 = Api.CreateType<Item>("testItem1");
testItem1.OnUse();

var test2 = Api.CreateType<Item>("testItem2");
test2.OnUse();
Console.WriteLine($"test item 2's name is [{test2.name}]");

// mini performance testing
var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

var test3 = Api.CreateType<Item>("testItem3");
for (var j = 0; j < 10000; j++) test3.OnUse();

var end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
Console.WriteLine($"i: {test3.i} | time: [{end - start:###,###}]ms");

// end
sw.Stop();
Console.WriteLine($"Run Time: [{sw.Elapsed}]");