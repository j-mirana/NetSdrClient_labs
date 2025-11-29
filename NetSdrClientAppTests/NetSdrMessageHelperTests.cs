using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        // COVERAGE: Test for ArgumentOutOfRangeException in GetSamples
        [Test]
        public void GetSamples_ThrowsException_WhenSampleSizeIsTooLarge()
        {
            // Arrange: 4 bytes (32 bit) is max size. 40 bit is 5 bytes, which should fail.
            ushort sampleSize = 40;
            byte[] body = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray());
        }

        // COVERAGE: Test for successful 16-bit sample retrieval
        [Test]
        public void GetSamples_ReturnsCorrectSamples_For16Bit()
        {
            // Arrange: 16 bit samples (2 bytes each). Total 3 samples.
            byte[] body = { 0x01, 0x00, 0x02, 0x00, 0x00, 0x00 };
            ushort sampleSize = 16;

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray();

            // Assert
            Assert.That(samples.Length, Is.EqualTo(3));
            Assert.That(samples[0], Is.EqualTo(1));
            Assert.That(samples[1], Is.EqualTo(2));
            Assert.That(samples[2], Is.EqualTo(0));
        }
    }
}