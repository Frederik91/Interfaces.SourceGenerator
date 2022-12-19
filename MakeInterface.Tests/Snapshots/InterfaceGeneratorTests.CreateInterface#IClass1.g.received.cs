﻿//HintName: IClass1.g.cs
#nullable enable
using MakeInterface.Tests.Models;
using MakeInterface.Contracts.Attributes;
using System.Collections.Generic;

// <auto-generated/>
#pragma warning disable
#nullable enable
namespace MakeInterface.Tests;
public partial interface IClass1
{
    void Method1();
    TestModel Test();
    void Test2<T>(T data);
    void Test3<T>(T data)
        where T : MakeInterface.Tests.Models.TestModel;
    string? Property1 { get; set; }

    List<ITestModel?>? TestCollection();
    void OutMethod(out string data);
    void RefMethod(ref string data);
    void DefaultNullMethod(string? data = default);
    void DefaultMethod(int data = default);
    string? GeneratedProperty { get; set; }

    void Method2();
}