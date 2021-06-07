using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tenrec.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private TestContext testContextInstance;
        public TestContext TestContext { get => testContextInstance; set => testContextInstance = value; }

        [TestMethod]
        public void RhinoInside_RunGrasshopperFile()
        {
            Utils.RunTenrecGroups(System.IO.Path.Combine(System.IO.Directory.GetParent(typeof(Utils).Assembly.Location).Parent.FullName, "TenrecSAMPLETest.gh"), TestContext);
        }
        [TestMethod]
        public void RhinoInside_CreateGHPoint_IsAsExpected()
        {
            var pt = new Grasshopper.Kernel.Types.GH_Point(new Rhino.Geometry.Point3d(1, 1, 1));
            Assert.AreEqual(pt.Value, new Rhino.Geometry.Point3d(1, 1, 1));

        }
    }
}
