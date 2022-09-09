// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;
using ApiTester;
using AutoModApi;

var sw = new Stopwatch();
sw.Start();
Api.Initialize("Scripts");
sw.Stop();
Console.WriteLine($"Initialize Time: [{sw.Elapsed}]");
        
sw.Reset();
sw.Start();
Api.ReadDir("Scripts");
sw.Stop();
Console.WriteLine($"Compile Time: [{sw.Elapsed}]");
        
sw.Reset();
sw.Start();
var testItem1 = Api.CreateType<Item>("testItem1");
testItem1.OnUse();
sw.Stop();
Console.WriteLine($"Run Time: [{sw.Elapsed}]");