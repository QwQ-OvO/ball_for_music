using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Diagnostics;
using System.Threading;
using InformationString.Core.Config;

namespace InformationString.Input
{
    public sealed class SerialSensorInput : ISensorInput
    {
        public bool IsConnected => isConnected;
        public event Action<SensorFrame> OnFrameReceived;
        public event Action<string> OnDisconnected;

        private SystemConfig config;
        private Thread thread;
        private volatile bool running;
        private volatile bool isConnected;

        private SerialPort port;

        public void Initialize(SystemConfig config)
        {
            this.config = config;
        }

        public void StartReading()
        {
            if (running) return;
            if (config == null) throw new InvalidOperationException("SerialSensorInput requires SystemConfig.");
            running = true;

            thread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "SerialSensorInput"
            };
            thread.Start();
        }

        public void StopReading()
        {
            running = false;
            isConnected = false;

            try { port?.Close(); } catch { }
            port = null;

            try
            {
                if (thread != null && thread.IsAlive) thread.Join(200);
            }
            catch { }

            thread = null;
        }

        private void ReadLoop()
        {
            try
            {
                port = new SerialPort(config.SerialPortName, config.SerialBaudRate)
                {
                    NewLine = "\n",
                    ReadTimeout = 500,
                    Encoding = Encoding.UTF8,
                };

                port.Open();
                isConnected = true;
            }
            catch (Exception ex)
            {
                isConnected = false;
                OnDisconnected?.Invoke($"Serial open failed: {ex.Message}");
                running = false;
                return;
            }

            var expected = Math.Max(1, config.ExpectedSlotsPerSide);

            using var reader = new StreamReader(port.BaseStream, Encoding.UTF8, false, 1024, leaveOpen: true);
            while (running)
            {
                string line = null;
                try
                {
                    line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                }
                catch (TimeoutException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    isConnected = false;
                    OnDisconnected?.Invoke($"Serial read failed: {ex.Message}");
                    running = false;
                    break;
                }

                if (!TryParseFrame(line, expected, out var frame))
                {
                    continue;
                }

                OnFrameReceived?.Invoke(frame);
            }

            try { port?.Close(); } catch { }
            port = null;
            isConnected = false;
        }

        public static bool TryParseFrame(string line, int expectedSlots, out SensorFrame frame)
        {
            frame = default;

            if (!TryExtractStringArray(line, "\"info\"", expectedSlots, out var info)) return false;
            if (!TryExtractStringArray(line, "\"rhythm\"", expectedSlots, out var rhythm)) return false;

            if (!TryExtractLinks(line, out var links))
                links = Array.Empty<SensorLinkEntry>();

            frame = new SensorFrame(info, rhythm, links, Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
            return true;
        }

        private static bool TryExtractStringArray(string json, string key, int expectedSlots, out string[] values)
        {
            values = null;
            var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0) return false;

            var bracketStart = json.IndexOf('[', keyIndex);
            if (bracketStart < 0) return false;

            if (!TryFindMatchingBracket(json, bracketStart, out var bracketEnd)) return false;

            var inner = json.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            var parsed = SplitTopLevelJsonStrings(inner);
            if (parsed.Count != expectedSlots) return false;

            values = parsed.ToArray();
            return true;
        }

        private static bool TryExtractLinks(string json, out SensorLinkEntry[] links)
        {
            links = Array.Empty<SensorLinkEntry>();

            var keyIndex = json.IndexOf("\"links\"", StringComparison.Ordinal);
            if (keyIndex < 0) return true;

            var bracketStart = json.IndexOf('[', keyIndex);
            if (bracketStart < 0) return false;

            if (!TryFindMatchingBracket(json, bracketStart, out var bracketEnd)) return false;

            var inner = json.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
            if (inner.Length == 0) return true;

            var entries = new List<SensorLinkEntry>();
            var i = 0;
            while (i < inner.Length)
            {
                var objStart = inner.IndexOf('{', i);
                if (objStart < 0) break;

                if (!TryFindMatchingBrace(inner, objStart, out var objEnd)) return false;

                var obj = inner.Substring(objStart, objEnd - objStart + 1);
                if (!TryParseLinkObject(obj, out var entry)) return false;

                entries.Add(entry);
                i = objEnd + 1;
            }

            links = entries.ToArray();
            return true;
        }

        private static bool TryParseLinkObject(string obj, out SensorLinkEntry entry)
        {
            entry = default;

            if (!TryExtractJsonStringField(obj, "info", out var infoId)) return false;
            if (!TryExtractJsonIntField(obj, "rhythmSlot", out var rhythmSlot)) return false;

            entry = new SensorLinkEntry(infoId, rhythmSlot);
            return true;
        }

        private static bool TryExtractJsonStringField(string json, string field, out string value)
        {
            value = null;
            var key = $"\"{field}\"";
            var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0) return false;

            var colon = json.IndexOf(':', keyIndex + key.Length);
            if (colon < 0) return false;

            var quoteStart = json.IndexOf('"', colon + 1);
            if (quoteStart < 0) return false;

            var quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return false;

            value = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
            return true;
        }

        private static bool TryExtractJsonIntField(string json, string field, out int value)
        {
            value = 0;
            var key = $"\"{field}\"";
            var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0) return false;

            var colon = json.IndexOf(':', keyIndex + key.Length);
            if (colon < 0) return false;

            var start = colon + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            var end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;

            return end > start &&
                   int.TryParse(json.Substring(start, end - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static List<string> SplitTopLevelJsonStrings(string inner)
        {
            var result = new List<string>();
            var i = 0;
            while (i < inner.Length)
            {
                while (i < inner.Length && (char.IsWhiteSpace(inner[i]) || inner[i] == ',')) i++;
                if (i >= inner.Length) break;

                if (inner[i] != '"') break;

                var start = i + 1;
                var end = start;
                while (end < inner.Length)
                {
                    if (inner[end] == '"' && inner[end - 1] != '\\') break;
                    end++;
                }

                if (end >= inner.Length) break;

                result.Add(inner.Substring(start, end - start));
                i = end + 1;
            }

            return result;
        }

        private static bool TryFindMatchingBracket(string text, int openIndex, out int closeIndex)
        {
            closeIndex = -1;
            var depth = 0;
            for (var i = openIndex; i < text.Length; i++)
            {
                if (text[i] == '[') depth++;
                else if (text[i] == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        closeIndex = i;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryFindMatchingBrace(string text, int openIndex, out int closeIndex)
        {
            closeIndex = -1;
            var depth = 0;
            for (var i = openIndex; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        closeIndex = i;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
