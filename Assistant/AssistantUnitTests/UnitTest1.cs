using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using NSubstitute;
namespace AssistantUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var substitute = Substitute.For<Console.Write()>();


        }
    }
}
