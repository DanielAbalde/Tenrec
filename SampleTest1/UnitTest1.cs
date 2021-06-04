using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SampleTest1
{
    [TestClass]
    public class UnitTest1
    {
        private TestContext testContextInstance;
        public TestContext TestContext { get => testContextInstance; set => testContextInstance = value; }

        [TestMethod]
        public void TestMethod1()
        {
            Tenrec.Utils.RunTenrecGroup(@"C:\Users\Dani Gonzalez\Desktop\New folder (2)\PorcupineSAMPLETest.gh",
              new System.Guid("aca484d6-28a7-4fb9-88e6-0b2a4e282bbd"), TestContext);
        }
    }
}
