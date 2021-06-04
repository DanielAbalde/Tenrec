using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tenrec.Tests
{
    [TestClass]
    public class UnitTestSAMPLE
    {
        private TestContext testContextInstance;
        public TestContext TestContext { get => testContextInstance; set => testContextInstance = value; }

        [TestMethod]
        public void Test_TestGroup1()
        {
            Utils.RunTenrecGroups(@"C:\Users\Dani Gonzalez\Desktop\New folder (2)\PorcupineSAMPLETest.gh", TestContext);
        }
        [TestMethod]
        public void Test_TestGrasshopper()
        {
            var pt = new Grasshopper.Kernel.Types.GH_Point(new Rhino.Geometry.Point3d(1, 1, 1));
            Assert.AreEqual(pt.Value, new Rhino.Geometry.Point3d(1, 1, 1));

        }
    }
}
