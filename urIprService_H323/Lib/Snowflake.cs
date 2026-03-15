using System;
using System.IO;

namespace MyProject.Utils {
    public static class Snowflake {
        private static readonly object _lock = new();
        private static readonly DateTime Epoch = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static int _machineId;
        private static string _persistFile = "last-id.txt";

        private static ulong _lastTimestamp = 0; // 改為 ulong
        private static ulong _sequence = 0;
        private static ulong _lastGeneratedId = 0;

        private const int MachineBits = 10;
        private const int SequenceBits = 12;

        // 使用 ulong 最大值進行掩碼運算
        private const ulong MaxSequence = (1u << SequenceBits) - 1;

        private static bool _initialized = false;

        public static void Init(int machineId, string persistFile = "last-id.txt") {
            if (machineId < 0 || machineId >= (1 << MachineBits))
                throw new ArgumentException($"MachineId 必須介於 0 ~ {(1 << MachineBits) - 1}");

            _machineId = machineId;
            _persistFile = persistFile;

            if (File.Exists(_persistFile)) {
                var text = File.ReadAllText(_persistFile);
                if (ulong.TryParse(text, out var lastId)) { // 解析為 ulong
                    _lastGeneratedId = lastId;
                }
            }
            _initialized = true;
        }

        public static ulong NextId() {
            if (!_initialized)
                throw new InvalidOperationException("Snowflake 尚未初始化");

            lock (_lock) {
                ulong timestamp = (ulong)GetTimestamp();

                if (timestamp < _lastTimestamp) {
                    // 如果發生時鐘回撥，強制使用上次時間戳
                    timestamp = _lastTimestamp;
                }

                if (timestamp == _lastTimestamp) {
                    _sequence = (_sequence + 1) & MaxSequence;
                    if (_sequence == 0) {
                        timestamp = WaitUntilNextMillis(_lastTimestamp);
                    }
                }
                else {
                    _sequence = 0;
                }

                _lastTimestamp = timestamp;

                // 位移運算全部使用 ulong
                ulong id = (timestamp << (MachineBits + SequenceBits))
                          | ((ulong)_machineId << SequenceBits)
                          | _sequence;

                // 確保絕對遞增
                if (id <= _lastGeneratedId) {
                    id = _lastGeneratedId + 1;
                }

                _lastGeneratedId = id;

                // 保存到檔案 (建議非同步或定期保存，頻繁 IO 可能影響效能)
                File.WriteAllText(_persistFile, _lastGeneratedId.ToString());

                return id;
            }
        }

        private static long GetTimestamp() => (long)(DateTime.UtcNow - Epoch).TotalMilliseconds;

        private static ulong WaitUntilNextMillis(ulong lastTimestamp) {
            ulong ts;
            while ((ts = (ulong)GetTimestamp()) <= lastTimestamp) { }
            return ts;
        }
    }
}
