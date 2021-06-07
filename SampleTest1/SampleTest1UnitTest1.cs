using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SampleTest1
{
    [TestClass]
    public class SampleTest1UnitTest1
    {
        private TestContext testContextInstance;
        public TestContext TestContext { get => testContextInstance; set => testContextInstance = value; }

        [TestMethod]
        public void TestMethod1()
        {
            RhinoInside.Resolver.Initialize();
            //AppDomain.CurrentDomain.AssemblyResolve += Tenrec.RhinoHeadless.ResolveAssemblies;
            Tenrec.Utils.RunTenrecGroups(System.IO.Path.Combine(System.IO.Directory.GetParent(typeof(Tenrec.Utils).Assembly.Location).Parent.Parent.Parent.FullName, "TenrecSAMPLETest.gh"), TestContext);
        }
    }
}
