// Copyright (c) dendr000. MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace FolderSizeViewer
{
    // 실제로 디스크를 얼마나 차지하는지 기준으로 크기를 계산한다.
    // 희소 파일(sparse)이나 동적 할당 가상 디스크(VHD/VHDX)는 논리적 파일 크기(FileInfo.Length)가
    // 실제 사용량보다 훨씬 크게 나올 수 있어, 탐색기의 "디스크 할당 크기"와 같은 방식으로 계산한다.
    internal static class DiskSize
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetCompressedFileSizeW")]
        private static extern uint GetCompressedFileSizeW(string lpFileName, out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetDiskFreeSpaceW(
            string lpRootPathName,
            out uint lpSectorsPerCluster,
            out uint lpBytesPerSector,
            out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);

        private const uint InvalidFileSize = 0xFFFFFFFF;

        private static readonly Dictionary<string, uint> ClusterSizeCache = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private static readonly object CacheLock = new object();

        public static long GetFileSizeOnDisk(string path)
        {
            uint high;
            uint low = GetCompressedFileSizeW(path, out high);

            if (low == InvalidFileSize)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 0) // 0 = NO_ERROR, 진짜 오류가 아니라 하위 32비트 값이 우연히 0xFFFFFFFF인 경우
                {
                    try { return new FileInfo(path).Length; }
                    catch { return 0; }
                }
            }

            long compressedSize = (long)(((ulong)high << 32) | low);

            uint clusterSize = GetClusterSize(path);
            if (clusterSize == 0) return compressedSize;

            // 클러스터 단위로 올림 (탐색기의 "디스크 할당 크기" 계산 방식과 동일)
            long remainder = compressedSize % clusterSize;
            return remainder == 0 ? compressedSize : compressedSize + (clusterSize - remainder);
        }

        private static uint GetClusterSize(string path)
        {
            string root;
            try { root = Path.GetPathRoot(path); }
            catch { return 0; }
            if (string.IsNullOrEmpty(root)) return 0;

            lock (CacheLock)
            {
                uint cached;
                if (ClusterSizeCache.TryGetValue(root, out cached)) return cached;

                uint sectorsPerCluster, bytesPerSector, freeClusters, totalClusters;
                uint size = 0;
                if (GetDiskFreeSpaceW(root, out sectorsPerCluster, out bytesPerSector, out freeClusters, out totalClusters))
                    size = sectorsPerCluster * bytesPerSector;

                ClusterSizeCache[root] = size;
                return size;
            }
        }
    }
}
