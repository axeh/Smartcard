using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartCard;

namespace SmartCardTest
{
    [TestClass]
    public class SmartCardTests
    {
        [TestMethod]
        public void CheckIfSmartCardIsConnected()
        {
            SmartCardManager cs = new SmartCardManager();
            var readers = cs.ListReaders();
            Assert.IsTrue(readers != null);

            if (readers.Count == 0)
            {
                Assert.Inconclusive("There isn`t any connected smart card readers");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (var reader in readers)
                {
                    sb.Append(reader + System.Environment.NewLine);
                }

                Assert.IsTrue(1==1, (string.Format("There is {0} detected smart card readers{1}{2}", readers.Count, System.Environment.NewLine, sb.ToString())));
            }

        }

        [TestMethod]
        public void CheckIfSmartCardIsConnectedPrefferedCsp()
        {
            //enter for preffered CSP. SmartCardManager will try to locate preffered one
            SmartCardManager cs = new SmartCardManager("AKDSHCard CSP");
            var readers = cs.ListReaders();
            Assert.IsTrue(readers != null);
            Assert.IsTrue(readers.Count > 0);

        }
    }
}
