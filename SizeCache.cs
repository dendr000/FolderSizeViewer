// Copyright (c) dendr000. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FolderSizeViewer
{
    /// <summary>
    /// 한 번 계산한 폴더 크기를 앱 안에 기억해 두었다가 다음에 바로 보여 주기 위한 저장소.
    /// 기억한 값은 "마지막으로 계산했을 때의 값"일 뿐이라, 화면에서는 새로 계산이 끝날 때까지 흐리게 보인다.
    /// </summary>
    public class SizeCache
    {
        public struct Entry
        {
            public long SizeBytes;
            public DateTime ScannedUtc;
        }

        private readonly string _filePath;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public SizeCache(string filePath)
        {
            _filePath = filePath;
        }

        public static string DefaultFilePath()
        {
            string overridePath = Environment.GetEnvironmentVariable("FOLDERSIZEVIEWER_SIZES_FILE");
            if (!string.IsNullOrEmpty(overridePath)) return overridePath;
            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderSizeViewer");
            return System.IO.Path.Combine(dir, "sizes.txt");
        }

        public int Count { get { return _entries.Count; } }

        public bool TryGet(string path, out Entry entry)
        {
            entry = default(Entry);
            string full = DeletableMarks.Normalize(path);
            return full != null && _entries.TryGetValue(full, out entry);
        }

        public void Set(string path, long sizeBytes, DateTime scannedUtc)
        {
            string full = DeletableMarks.Normalize(path);
            if (full == null || sizeBytes < 0) return;
            _entries[full] = new Entry { SizeBytes = sizeBytes, ScannedUtc = scannedUtc };
        }

        public void Clear() { _entries.Clear(); }

        /// <summary>기준 날짜보다 오래된 기록을 버린다. 버린 개수를 돌려준다.</summary>
        public int PruneOlderThan(DateTime cutoffUtc)
        {
            var old = _entries.Where(kv => kv.Value.ScannedUtc < cutoffUtc).Select(kv => kv.Key).ToList();
            foreach (string k in old) _entries.Remove(k);
            return old.Count;
        }

        public void Load()
        {
            _entries.Clear();
            if (!File.Exists(_filePath)) return;
            foreach (string line in File.ReadAllLines(_filePath, Encoding.UTF8))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                string[] parts = line.Split('\t');
                if (parts.Length != 3) continue;
                long size; long ticks;
                if (!long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out size)) continue;
                if (!long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out ticks)) continue;
                if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) continue;
                Set(parts[0], size, new DateTime(ticks, DateTimeKind.Utc));
            }
        }

        /// <summary>임시 파일에 먼저 쓰고 바꿔치기해서, 쓰다 끊겨도 기존 기록이 깨지지 않게 한다.</summary>
        public void Save()
        {
            string dir = System.IO.Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string tmp = _filePath + ".tmp";
            var lines = new List<string> { "# FolderSizeViewer: 폴더 크기 기억 (경로, 바이트, 계산 시각)" };
            foreach (var kv in _entries.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                lines.Add(kv.Key + "\t" + kv.Value.SizeBytes.ToString(CultureInfo.InvariantCulture) + "\t"
                    + kv.Value.ScannedUtc.Ticks.ToString(CultureInfo.InvariantCulture));
            File.WriteAllLines(tmp, lines.ToArray(), new UTF8Encoding(false));

            if (File.Exists(_filePath)) File.Replace(tmp, _filePath, null);
            else File.Move(tmp, _filePath);
        }
    }
}
