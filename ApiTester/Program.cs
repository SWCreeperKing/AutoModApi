// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ApiTester;
using AutoModApi;

var sw = new Stopwatch();
sw.Start();
Api.projectName = "Testing Project";
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

var test2 = Api.CreateType<Item>("testItem2");
test2.OnUse();
Console.WriteLine($"test item 2's name is [{test2.name}]");

sw.Stop();
Console.WriteLine($"Run Time: [{sw.Elapsed}]");