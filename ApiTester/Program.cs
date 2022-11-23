using System.Diagnostics;
using ApiTester;
using AutoModApi;

var sw = new Stopwatch();
sw.Start();

Api.projectName = "Testing Project";
Api.ReadDir("Scripts");
Api.Initialize("Scripts", "notes");

// loading
// Api.Compile();
Api.CompileWithLoading();

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

return;
Console.ReadLine();
Console.Clear();
Api.CompileWithLoading();