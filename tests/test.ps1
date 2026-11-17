# Copyright (c) dendr000. MIT License.
# 표시 기능(DeletableMarks)의 시험을 임시 폴더에서 컴파일하고 실행한다.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$exe = Join-Path ([System.IO.Path]::GetTempPath()) ('fsv_tests_' + [guid]::NewGuid().ToString('N') + '.exe')

& $csc /nologo /target:exe /out:$exe `
    (Join-Path $root 'DeletableMarks.cs') `
    (Join-Path $root 'SizeCache.cs') `
    (Join-Path $PSScriptRoot 'SizeCacheTests.cs') `
    (Join-Path $PSScriptRoot 'DeletableMarksTests.cs')
if ($LASTEXITCODE -ne 0) { throw "test build failed" }

& $exe
$code = $LASTEXITCODE
Remove-Item $exe -Force
exit $code
