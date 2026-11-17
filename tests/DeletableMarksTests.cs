// Copyright (c) dendr000. MIT License.
using System;
using System.IO;
using FolderSizeViewer;

// 실제 데이터를 건드리지 않고 임시 폴더에서만 시험한다.
public static class DeletableMarksTests
{
    private static int _pass, _fail;

    private static void Check(string name, bool ok)
    {
        if (ok) { _pass++; Console.WriteLine("  ok   " + name); }
        else { _fail++; Console.WriteLine("  FAIL " + name); }
    }

    public static int Main()
    {
        string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fsv_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        string file = System.IO.Path.Combine(tmp, "marks.txt");
        try
        {
            string cache = System.IO.Path.Combine(tmp, "Chrome", "Cache");
            string child = System.IO.Path.Combine(cache, "Data_0");

            var m = new DeletableMarks(file);
            Check("add new path returns true", m.Add(cache));
            Check("add same path again returns false", !m.Add(cache));
            Check("case-insensitive and trailing slash", m.IsMarked(cache.ToUpperInvariant() + "\\"));
            Check("state Marked", m.GetState(cache) == RowMark.Marked);
            Check("child is Inherited", m.GetState(child) == RowMark.Inherited);
            Check("sibling is None", m.GetState(System.IO.Path.Combine(tmp, "Chrome", "Other")) == RowMark.None);
            Check("prefix-named sibling is not inherited", m.GetState(cache + "2") == RowMark.None);

            Check("drive root not markable", !m.Add("C:\\"));
            Check("empty not markable", !m.Add("  "));
            Check("invalid chars not markable", !m.Add("C:\\a|b?"));
            Check("count is 1", m.Count == 1);

            m.Save();
            var m2 = new DeletableMarks(file);
            m2.Load();
            Check("reload keeps mark (folder does not exist)", m2.IsMarked(cache) && !Directory.Exists(cache));
            Check("no leftover tmp file", !File.Exists(file + ".tmp"));

            // 삭제 후 같은 경로에 다시 생겨도 표시가 살아 있다
            Directory.CreateDirectory(cache);
            Directory.Delete(cache, true);
            Directory.CreateDirectory(cache);
            Check("recreated folder is marked again", m2.IsMarked(cache));

            m2.Save();
            Check("remove returns true", m2.Remove(cache));
            Check("remove missing returns false", !m2.Remove(cache));
            m2.Save();
            var m3 = new DeletableMarks(file);
            m3.Load();
            Check("removal persisted", m3.Count == 0);

            // 깨진 파일
            File.WriteAllText(file, "# c\n\n  \nC:\\\nnot a path |?\n" + cache + "\n");
            var m4 = new DeletableMarks(file);
            m4.Load();
            Check("load ignores comments, blanks, roots, junk", m4.Count == 1 && m4.IsMarked(cache));

            var m5 = new DeletableMarks(System.IO.Path.Combine(tmp, "none", "x.txt"));
            m5.Load();
            Check("missing file loads empty", m5.Count == 0);
            m5.Add(cache);
            m5.Save();
            Check("save creates missing directory", File.Exists(System.IO.Path.Combine(tmp, "none", "x.txt")));

            int cachePass;
            Console.WriteLine("-- SizeCache --");
            _fail += SizeCacheTests.Run(tmp, out cachePass);
            _pass += cachePass;
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { }
        }

        Console.WriteLine();
        Console.WriteLine("passed " + _pass + ", failed " + _fail);
        return _fail == 0 ? 0 : 1;
    }
}
