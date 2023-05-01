// See https://aka.ms/new-console-template for more information
using NAudio.Wave;
using System;
using System.Diagnostics;
using Whisper.net;
using Sharprompt;

const int audioSampleLengthS = 1;
const int audioSampleLengthMs = audioSampleLengthS * 1000;
const int totalBufferLength = 30 / audioSampleLengthS;
List<float[]> slidingBuffer = new(totalBufferLength + 1);

var devices = new List<AudioDevices>();

for (int i = 0; i < WaveIn.DeviceCount; i++)
{
    var capabilities = WaveIn.GetCapabilities(i);
    devices.Add(new AudioDevices() { DeviceNumber = i, DeviceName = capabilities.ProductName });
}

var audioDevice = Prompt.Select("オーディオデバイスを選択してください", devices.Select(n => n.DeviceName));

var selectedDevice = devices.First(n => n.DeviceName == audioDevice)!;

var factory = WhisperFactory.FromBuffer(Drastic.WhisperSample.EmbeddedModels.LoadTinyModel());

var processor = factory.CreateBuilder()
    .WithLanguage("auto")
    .WithSegmentEventHandler((e) =>
    {
        Console.WriteLine($"Segment: {e.Text}");
    })
    .Build();

WaveInEvent waveIn = new()
{
    DeviceNumber = selectedDevice.DeviceNumber, // indicates which microphone to use
    WaveFormat = new WaveFormat(rate: 16000, bits: 16, channels: 1), // must be supported by the microphone
    BufferMilliseconds = audioSampleLengthMs,
};
waveIn.DataAvailable += WaveInDataAvailable;
waveIn.StartRecording();
Console.WriteLine("話を聞く準備をしています…");

void WaveInDataAvailable(object? sender, WaveInEventArgs e)
{
    var values = new short[e.Buffer.Length / 2];
    Buffer.BlockCopy(e.Buffer, 0, values, 0, e.Buffer.Length);
    var samples = values.Select(x => x / (short.MaxValue + 1f)).ToArray();

    var silenceCount = samples.Count(x => IsSilence(x, -40));

    if (silenceCount < values.Length - values.Length / 12)
    {
        slidingBuffer.Add(samples);

        if (slidingBuffer.Count > totalBufferLength)
        {
            slidingBuffer.RemoveAt(0);
        }

        processor.Process(slidingBuffer.SelectMany(x => x).ToArray());
    }
}

Console.WriteLine("書き起こすのを停止するには、任意のキーを押してください");
Console.ReadLine();

static bool IsSilence(float amplitude, sbyte threshold)
    => GetDecibelsFromAmplitude(amplitude) < threshold;

static double GetDecibelsFromAmplitude(float amplitude)
    => 20 * Math.Log10(Math.Abs(amplitude));

public class AudioDevices
{
    public int DeviceNumber { get; set; }

    public string DeviceName { get; set; } = string.Empty;
}