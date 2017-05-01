using System;
using SCCMaker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SCCMaker_Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestTimestamps()
        {
            Timestamp t = new Timestamp();
            Timestamp.DropFrame = false;
            Timestamp.FrameRate = 59.94m;
            t.Hour = 5;
            t.Minute = 59;
            t.Second = 59;
            t.Frame = 0;

            t.Frame += 60;

            Assert.AreEqual(6, t.Hour, "Hour not proper");
            Assert.AreEqual(0, t.Minute, "Minute not proper");
            Assert.AreEqual(0, t.Second, "Second not proper");
            Assert.AreEqual(0, t.Frame, "Frame not proper");
        }
    }
}
