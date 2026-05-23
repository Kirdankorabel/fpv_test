using NUnit.Framework;

namespace FpvSim.Flight.Tests
{
    public class PilotCommandTests
    {
        [Test]
        public void Ctor_StoresAllFields()
        {
            PilotCommand cmd = new PilotCommand(0.5f, 0.1f, -0.2f, 0.3f, true, false);
            Assert.AreEqual(0.5f, cmd.Throttle);
            Assert.AreEqual(0.1f, cmd.Roll);
            Assert.AreEqual(-0.2f, cmd.Pitch);
            Assert.AreEqual(0.3f, cmd.Yaw);
            Assert.IsTrue(cmd.ArmRequested);
            Assert.IsFalse(cmd.RespawnRequested);
        }
    }
}
