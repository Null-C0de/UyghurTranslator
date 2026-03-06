using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using Newtonsoft.Json;

namespace UyghurTranslator;

/// <summary>
/// TTS引擎 - 仅支持维语(ONNX)
/// </summary>
public class TTSEngine : IDisposable
{
    private InferenceSession? _onnxSession;
    private Dictionary<string, int>? _vocab;
    private int _samplingRate = 16000;
    private int _maxSeqLen = 64;
    private float _volume = 1.0f;
    private bool _isSpeaking;
    private readonly object _lock = new();

    public bool IsSpeaking => _isSpeaking;
    public bool IsOnnxAvailable => _onnxSession != null;

    public TTSEngine()
    {
        LoadOnnxModel();
    }

    private void LoadOnnxModel()
    {
        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var onnxPath = Path.Combine(exeDir, "onnx_model", "mms_tts_uighur.onnx");
            var vocabPath = Path.Combine(exeDir, "onnx_model", "vocab.json");
            var configPath = Path.Combine(exeDir, "onnx_model", "config.json");

            if (!File.Exists(onnxPath) || !File.Exists(vocabPath))
                return;

            _onnxSession = new InferenceSession(onnxPath);

            var vocabJson = File.ReadAllText(vocabPath);
            _vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabJson);

            if (File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
                if (config != null)
                {
                    if (config.TryGetValue("sampling_rate", out var sr))
                        _samplingRate = Convert.ToInt32(sr);
                    if (config.TryGetValue("max_seq_len", out var ms))
                        _maxSeqLen = Convert.ToInt32(ms);
                }
            }
        }
        catch { _onnxSession = null; }
    }

    public void SetVolume(float volume) => _volume = Math.Clamp(volume, 0f, 1f);

    public async Task<(bool success, string error)> SpeakAsync(string text, string language)
    {
        // 只支持维语朗读
        if (language != "ug")
            return (false, "仅支持维语朗读");

        lock (_lock)
        {
            if (_isSpeaking) return (false, "正在朗读中");
            _isSpeaking = true;
        }

        try
        {
            return await SpeakUyghurAsync(text);
        }
        finally
        {
            _isSpeaking = false;
        }
    }

    private async Task<(bool success, string error)> SpeakUyghurAsync(string text)
    {
        if (_onnxSession == null || _vocab == null)
            return (false, "维语朗读不可用: ONNX模型未加载");

        return await Task.Run(() =>
        {
            try
            {
                var tokens = Tokenize(text);
                if (tokens.Count == 0)
                    return (false, "文本无法分词");

                var inputIds = new long[_maxSeqLen];
                var attentionMask = new long[_maxSeqLen];

                for (int i = 0; i < Math.Min(tokens.Count, _maxSeqLen); i++)
                {
                    inputIds[i] = tokens[i];
                    attentionMask[i] = 1;
                }

                var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, _maxSeqLen });
                var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, _maxSeqLen });

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                    NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
                };

                using var results = _onnxSession.Run(inputs);
                var waveform = results.First().AsEnumerable<float>().ToArray();

                var amplifiedWaveform = new float[waveform.Length];
                var amplifyFactor = _volume * 5.0f;
                for (int i = 0; i < waveform.Length; i++)
                    amplifiedWaveform[i] = Math.Clamp(waveform[i] * amplifyFactor, -1f, 1f);

                PlayAudio(amplifiedWaveform, _samplingRate);
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, $"维语朗读失败: {ex.Message}");
            }
        });
    }

    private List<int> Tokenize(string text)
    {
        var tokens = new List<int> { 0 };
        foreach (var c in text)
        {
            if (_vocab!.TryGetValue(c.ToString(), out var tokenId))
                tokens.Add(tokenId);
        }
        tokens.Add(2);
        return tokens;
    }

    private void PlayAudio(float[] waveform, int sampleRate)
    {
        var samples = new short[waveform.Length];
        for (int i = 0; i < waveform.Length; i++)
            samples[i] = (short)(Math.Clamp(waveform[i], -1f, 1f) * short.MaxValue);

        using var waveOut = new WaveOutEvent();
        using var provider = new RawSourceWaveStream(
            new MemoryStream(ShortToBytes(samples)),
            new WaveFormat(sampleRate, 16, 1));

        waveOut.Init(provider);
        waveOut.Play();
        while (waveOut.PlaybackState == PlaybackState.Playing)
            Thread.Sleep(50);
    }

    private static byte[] ShortToBytes(short[] values)
    {
        var bytes = new byte[values.Length * 2];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public void Dispose() => _onnxSession?.Dispose();
}
