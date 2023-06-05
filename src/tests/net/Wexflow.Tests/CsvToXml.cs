﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Wexflow.Tests
{
    [TestClass]
    public class CsvToXml
    {
        private static readonly string ExpectedResult =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<Lines>\r\n" +
            "  <Line>\r\n" +
            "    <Column>content</Column>\r\n" +
            "    <Column>content</Column>\r\n" +
            "    <Column>content</Column>\r\n" +
            "  </Line>\r\n" +
            "  <Line>\r\n" +
            "    <Column>content</Column>\r\n" +
            "    <Column>content</Column>\r\n" +
            "    <Column>content</Column>\r\n" +
            "  </Line>\r\n" +
            "  <Line>\r\n" +
            "    <Column>content</Column>\r\n" +
            "    <Column>content</Column>\r\n" +
            "    <Column>content</Column>\r\n" +
            "  </Line>\r\n" +
            "</Lines>";

        [TestInitialize]
        public void TestInitialize()
        {
            DeleteXmls();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DeleteXmls();
        }

        [TestMethod]
        public void CsvToXmlTest()
        {
            Helper.StartWorkflow(1);

            // Check the workflow result
            string[] xmlFiles = Directory.GetFiles(@"C:\WexflowTesting\CsvToXml\", "*.xml");
            Assert.AreEqual(2, xmlFiles.Length);

            foreach (string xmlFile in xmlFiles)
            {
                string xmlContent = File.ReadAllText(xmlFile);
                Assert.AreEqual(ExpectedResult, xmlContent);
            }
        }

        private void DeleteXmls()
        {
            foreach (string file in Directory.GetFiles(@"C:\WexflowTesting\CsvToXml\", "*.xml"))
            {
                File.Delete(file);
            }
        }
    }
}
