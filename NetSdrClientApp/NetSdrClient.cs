using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientApp
{
    public sealed class NetSdrClient
    {
        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;
        private readonly ILogger _logger;

        public bool IQStarted { get; private set; }

        private TaskCompletionSource<byte[]>? responseTaskSource = null;

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient, ILogger logger)
        {
            _tcpClient = tcpClient;
            _udpClient = udpClient;
            _logger = logger;

            _tcpClient.MessageReceived += _tcpClient_MessageReceived;
            _udpClient.MessageReceived += _udpClient_MessageReceived;
        }

        public async Task ConnectAsync()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect();

                var sampleRate = BitConverter.GetBytes((long)100000).Take(5).ToArray();
                var autoFilter = BitConverter.GetBytes((ushort)0).ToArray();
                var adMode = new byte[] { 0x00, 0x03 };

                var msgs = new List<byte[]>
                {
                    NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.IQOutputDataSampleRate, sampleRate),
                    NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.RFFilter, autoFilter),
                    NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ADModes, adMode)
                };

                foreach (var msg in msgs)
                    await SendTcpRequest(msg);
            }
        }

        public void Disconnect()
        {
            _tcpClient.Disconnect();
            if (!_tcpClient.Connected)
                _logger.Log("No active connection to disconnect.");
            else
                _logger.Log("Disconnected.");
        }

        public async Task StartIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                _logger.Log("No active connection. Cannot start IQ.");
                return;
            }

            byte iqDataMode = 0x80;
            byte start = 0x02;
            byte fifo16 = 0x01;
            byte n = 1;

            var args = new[] { iqDataMode, start, fifo16, n };
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);

            await SendTcpRequest(msg);

            IQStarted = true;

            _ = _udpClient.StartListeningAsync();
        }

        public async Task StopIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                _logger.Log("No active connection. Cannot stop IQ.");
                return;
            }

            var args = new byte[] { 0, 0x01, 0, 0 };
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);

            await SendTcpRequest(msg);

            IQStarted = false;
            _udpClient.StopListening();
        }

        public async Task<byte[]?> ChangeFrequencyAsync(long hz, int channel)
        {
            if (!_tcpClient.Connected)
            {
                _logger.Log("No active connection. TCP request aborted.");
                return null;
            }

            var channelArg = (byte)channel;
            var freq = BitConverter.GetBytes(hz).Take(5).ToArray();
            var args = (new[] { channelArg }).Concat(freq).ToArray();

            var msg = NetSdrMessageHelper.GetControlItemMessage(
                MsgTypes.SetControlItem, ControlItemCodes.ReceiverFrequency, args);

            return await SendTcpRequest(msg);
        }

        private void _udpClient_MessageReceived(object? sender, byte[] data)
        {
            // small and fast
            try
            {
                NetSdrMessageHelper.TranslateMessage(data, out _, out _, out _, out var body);
                var samples = NetSdrMessageHelper.GetSamples(16, body);

                _logger.Log("Samples recieved: " +
                    string.Join(" ", body.Select(b => b.ToString("X2"))));

                using var fs = new FileStream("samples.bin", FileMode.Append, FileAccess.Write, FileShare.Read);
                using var bw = new BinaryWriter(fs);

                foreach (var s in samples)
                    bw.Write((short)s);
            }
            catch (Exception ex)
            {
                _logger.Log($"UDP Parse Error: {ex.Message}");
            }
        }

        private async Task<byte[]?> SendTcpRequest(byte[] msg)
        {
            if (!_tcpClient.Connected)
            {
                _logger.Log("No active connection. TCP request aborted.");
                return null;
            }

            responseTaskSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = responseTaskSource.Task;

            await _tcpClient.SendMessageAsync(msg);

            return await task;
        }

        private void _tcpClient_MessageReceived(object? sender, byte[] data)
        {
            if (responseTaskSource != null)
            {
                responseTaskSource.TrySetResult(data);
                responseTaskSource = null;
            }

            _logger.Log("Response recieved: " +
                string.Join(" ", data.Select(b => b.ToString("X2"))));
        }
    }
}
