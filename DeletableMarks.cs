// Copyright (c) dendr000. MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FolderSizeViewer
{
    /// <summary>
    /// "삭제해도 되는 폴더" 표시를 경로 기준으로 앱 안에만 저장한다.
    /// 폴더가 지워져도 기록은 남고, 같은 경로에 같은 이름의 폴더가 다시 생기면 다시 표시된다.
    /// 이 클래스는 파일을 지우거나 옮기지 않는다.
    /// </summary>
    public class DeletableMarks
    {
        private readonly string _filePath;
        private readonly HashSet<string> _marks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public DeletableMarks(string filePath)
        {
            _filePath = filePath;
        }

        public static string DefaultFilePath()
        {
            // 화면 시험 때 실제 기록을 건드리지 않도록 경로를 바꿀 수 있다.
            string overridePath = Environment.GetEnvironmentVariable("FOLDERSIZEVIEWER_MARKS_FILE");
            if (!string.IsNullOrEmpty(overridePath)) return overridePath;

            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderSizeViewer");
            return System.IO.Path.Combine(dir, "marks.txt");
        }

        public int Count { get { return _marks.Count; } }

        public IEnumerable<string> All { get { return _marks.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList(); } }

        /// <summary>경로를 비교용으로 정리한다. 잘못된 경로면 null.</summary>
        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                string full = System.IO.Path.GetFullPath(path.Trim());
                string root = System.IO.Path.GetPathRoot(full);
                if (full.Length > root.Length)
                    full = full.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                return full;
            }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
            catch (System.Security.SecurityException) { return null; }
            catch (PathTooLongException) { return null; }
        }

        /// <summary>드라이브 루트는 표시 대상이 아니다.</summary>
        public static bool IsMarkable(string path)
        {
            string full = Normalize(path);
            if (full == null) return false;
            return !string.Equals(full, System.IO.Path.GetPathRoot(full), StringComparison.OrdinalIgnoreCase);
        }

        public bool IsMarked(string path)
        {
            string full = Normalize(path);
            return full != null && _marks.Contains(full);
        }

        /// <summary>자기 자신은 표시되지 않았지만 상위 폴더 중 하나가 표시된 경우 true.</summary>
        public bool IsInherited(string path)
        {
            string full = Normalize(path);
            if (full == null) return false;
            string parent = System.IO.Path.GetDirectoryName(full);
            while (!string.IsNullOrEmpty(parent))
            {
                if (_marks.Contains(parent)) return true;
                parent = System.IO.Path.GetDirectoryName(parent);
            }
            return false;
        }

        public RowMark GetState(string path)
        {
            if (IsMarked(path)) return RowMark.Marked;
            if (IsInherited(path)) return RowMark.Inherited;
            return RowMark.None;
        }

        /// <summary>표시한다. 새로 추가됐으면 true. 드라이브 루트나 잘못된 경로는 false.</summary>
        public bool Add(string path)
        {
            if (!IsMarkable(path)) return false;
            return _marks.Add(Normalize(path));
        }

        public bool Remove(string path)
        {
            string full = Normalize(path);
            return full != null && _marks.Remove(full);
        }

        public void Load()
        {
            _marks.Clear();
            if (!File.Exists(_filePath)) return;
            foreach (string line in File.ReadAllLines(_filePath, Encoding.UTF8))
            {
                string t = line.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;
                if (IsMarkable(t)) _marks.Add(Normalize(t));
            }
        }

        /// <summary>임시 파일에 먼저 쓰고 바꿔치기해서, 쓰다 끊겨도 기존 기록이 깨지지 않게 한다.</summary>
        public void Save()
        {
            string dir = System.IO.Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string tmp = _filePath + ".tmp";
            var lines = new List<string> { "# FolderSizeViewer: 삭제해도 된다고 표시한 폴더 (한 줄에 하나)" };
            lines.AddRange(All);
            File.WriteAllLines(tmp, lines.ToArray(), new UTF8Encoding(false));

            if (File.Exists(_filePath)) File.Replace(tmp, _filePath, null);
            else File.Move(tmp, _filePath);
        }
    }

    public enum RowMark
    {
        None = 0,
        Inherited = 1,
        Marked = 2
    }
}
