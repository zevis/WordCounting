using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WordCounting;

namespace WordCountingTests
{
    [TestClass]
    public class SimpleTests
    {
        private class FakeBytesSource : FileBytesSource
        {
            private readonly int _testNum;

            public string Text { get; private set; }

            public FakeBytesSource(int testNum, int parts) : base(string.Empty, string.Empty, parts)
            {
                _testNum = testNum;
            }

            protected override byte[] GetBytes()
            {
                switch (_testNum)
                {
                    case 1:
                        return new byte[0];
                    case 2:
                        return new byte[] { 32 };
                    case 3:
                        return new byte[] { 255 };
                    case 4:
                        return new byte[] { 255, 32 };
                    case 5:
                        return new byte[] { 32, 255 };
                    case 6:
                        return new byte[] { 32, 254, 254, 32, 254, 254, 32 };
                }

                return new byte[0];
            }

            protected override bool WriteText(string text)
            {
                Text = text;
                return true;
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(RunTest(1), "");
        }

        [TestMethod]
        public void TestMethod2()
        {
            Assert.AreEqual(RunTest(2), "");
        }

        [TestMethod]
        public void TestMethod3()
        {
            Assert.AreEqual(RunTest(3), "я,1");
        }

        [TestMethod]
        public void TestMethod4()
        {
            Assert.AreEqual(RunTest(4), "я,1");
        }

        [TestMethod]
        public void TestMethod5()
        {
            Assert.AreEqual(RunTest(5), "я,1");
        }

        [TestMethod]
        public void TestMethod6()
        {
            Assert.AreEqual(RunTest(6), "юю,2");
        }

        private string RunTest(int num)
        {
            var bytesSource = new FakeBytesSource(num, Environment.ProcessorCount * 16);
            AsyncWordAccumulator asyncWordAccumulator = new AsyncWordAccumulator();
            asyncWordAccumulator.Configure(Environment.ProcessorCount);
            LogicWordCounts.Start(bytesSource, asyncWordAccumulator).Wait();
            return bytesSource.Text;
        }
    }
}
