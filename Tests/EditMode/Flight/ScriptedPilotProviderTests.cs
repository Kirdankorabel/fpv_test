using NUnit.Framework;
using FpvSim.Input;

namespace FpvSim.Flight.Tests
{
    public class ScriptedPilotProviderTests
    {
        [Test]
        public void Read_DelegatesToSequenceWithMonotonicTime()
        {
            int callCount = 0;
            ScriptedPilotProvider sut = new ScriptedPilotProvider(t =>
            {
                callCount++;
                return new PilotCommand(t, 0f, 0f, 0f, false, false);
            });
            PilotCommand c1 = sut.Read();
            PilotCommand c2 = sut.Read();
            Assert.AreEqual(2, callCount);
            Assert.Less(c1.Throttle, c2.Throttle);
        }
    }
}
