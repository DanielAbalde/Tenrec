using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TenrecGeneratedTests
{
    [TestClass]
    public class AutoTest_PorcupineSAMPLETest
    {
        public string FilePath => @"C:\Users\Dani Gonzalez\Desktop\New folder (2)\PorcupineSAMPLETest.gh";
        private TestContext testContextInstance;
        public TestContext TestContext { get => testContextInstance; set => testContextInstance = value; }
        [TestMethod]
        public void TestGroup_A()
        {
            Tenrec.Utils.RunTenrecGroup(FilePath, new System.Guid("aca484d6-28a7-4fb9-88e6-0b2a4e282bbd"), TestContext);
        }
        [TestMethod]
        public void TestGroup_B()
        {
            Tenrec.Utils.RunTenrecGroup(FilePath, new System.Guid("2fffe353-a782-4b06-86ff-06e16e751f49"), TestContext);
        }
        [TestMethod]
        public void TestGroup_C()
        {
            Tenrec.Utils.RunTenrecGroup(FilePath, new System.Guid("20fe24eb-dff5-498f-ad42-72194b162d17"), TestContext);
        }
    }

}
