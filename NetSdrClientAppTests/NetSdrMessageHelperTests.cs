using NetSdrClientApp.Messages;
using NUnit.Framework;
using System;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [Test]
        public void GetControlItemMessageTest()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            // Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2).ToArray();
            var codeBytes = msg.Skip(2).Take(2).ToArray();
            var parametersBytes = msg.Skip(4).ToArray();

            var num = BitConverter.ToUInt16(headerBytes);
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes);

            // Assert (Sonar-friendly)
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes, Has.Length.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(actualLength));
                Assert.That(actualType, Is.EqualTo(type));
                Assert.That((short)code, Is.EqualTo(actualCode));
                Assert.That(parametersBytes, Has.Length.EqualTo(parametersLength));
            });
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            // Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2).ToArray();
            var parametersBytes = msg.Skip(2).ToArray();

            var num = BitConverter.ToUInt16(headerBytes);
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            // Assert (Sonar-friendly)
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes, Has.Length.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(actualLength));
                Assert.That(actualType, Is.EqualTo(type));
                Assert.That(parametersBytes, Has.Length.EqualTo(parametersLength));
            });
        }

        // COVERAGE: Test for ArgumentOutOfRangeException in GetSamples
        [Test]
        public void GetSamples_ThrowsException_WhenSampleSizeIsTooLarge()
        {
            ushort sampleSize = 40; // too big
            byte[] body = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            Assert.That(
                () => NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray(),
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );
        }

        // COVERAGE: Test for successful 16-bit sample retrieval
        [Test]
        public void GetSamples_ReturnsCorrectSamples_For16Bit()
        {
            // Arrange: 3 samples (16-bit = 2 bytes each)
            byte[] body = { 0x01, 0x00, 0x02, 0x00, 0x00, 0x00 };
            ushort sampleSize = 16;

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray();

            // Assert (Sonar-friendly)
            Assert.Multiple(() =>
            {
                Assert.That(samples, Has.Length.EqualTo(3));
                Assert.That(samples[0], Is.EqualTo(1));
                Assert.That(samples[1], Is.EqualTo(2));
                Assert.That(samples[2], Is.EqualTo(0));
            });
        }
    }
}
