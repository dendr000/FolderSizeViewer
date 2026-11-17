// Copyright (c) dendr000. MIT License.
using System;
using System.IO;
using FolderSizeViewer;

// 임시 폴더에서만 시험한다.
public static class SizeCacheTests
{
    private static int _pass, _fail;

    private static void Check(string name, bool ok)
    {
        if (ok) { _pass++; Console.WriteLine("  ok   " + name); }
        else { _fail++; Console.WriteLine("  FAIL " + name); }
    }

    /// <summary>실패 개수를 돌려준다. 통과 개수는 passed 로 돌려준다.</summary>
    public static int Run(string tmp, out int passed)
    {
        string file = System.IO.Path.Combine(tmp, "sizes.txt");
        string a = System.IO.Path.Combine(tmp, "Chrome", "Cache");
        DateTime t0 = new DateTime(2026, 11, 17, 1, 0, 0, DateTimeKind.Utc);

        var c = new SizeCache(file);
        SizeCache.Entry e;
        Check("unknown path misses", !c.TryGet(a, out e));
        c.Set(a, 123456789012L, t0);
        Check("set then get", c.TryGet(a, out e) && e.SizeBytes == 123456789012L && e.ScannedUtc == t0);
        Check("case-insensitive get", c.TryGet(a.ToUpperInvariant() + "\\", out e));
        c.Set(a, 5, t0.AddHours(1));
        Check("overwrite keeps one entry", c.Count == 1 && c.TryGet(a, out e) && e.SizeBytes == 5);
        c.Set(a, -1, t0);
        Check("negative size ignored", c.TryGet(a, out e) && e.SizeBytes == 5);
        c.Set("  ", 10, t0);
        Check("invalid path ignored", c.Count == 1);

        c.Save();
        var c2 = new SizeCache(file);
        c2.Load();
        Check("reload keeps value and time", c2.TryGet(a, out e) && e.SizeBytes == 5 && e.ScannedUtc == t0.AddHours(1));
        Check("no leftover tmp", !File.Exists(file + ".tmp"));

        c2.Set(System.IO.Path.Combine(tmp, "Old"), 1, t0.AddDays(-100));
        int removed = c2.PruneOlderThan(t0.AddDays(-90));
        Check("prune removes only old", removed == 1 && c2.Count == 1);

        File.WriteAllText(file, "# c\n\nbad line\nC:\\x\tnotnum\t1\n" + a + "\t7\t" + t0.Ticks + "\n" + a + "2\t-3\t1\n");
        var c3 = new SizeCache(file);
        c3.Load();
        Check("load skips junk lines", c3.Count == 1 && c3.TryGet(a, out e) && e.SizeBytes == 7);

        var c4 = new SizeCache(System.IO.Path.Combine(tmp, "nope", "s.txt"));
        c4.Load();
        c4.Set(a, 1, t0);
        c4.Save();
        Check("missing dir and file handled", c4.Count == 1 && File.Exists(System.IO.Path.Combine(tmp, "nope", "s.txt")));

        c4.Clear();
        Check("clear empties entries", c4.Count == 0 && !c4.TryGet(a, out e));

        passed = _pass;
        return _fail;
    }
}
