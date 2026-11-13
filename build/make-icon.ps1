<#
.SYNOPSIS
    앱 아이콘 원본(src/JoyConMouse.App/Assets/icon.png, 1024px 투명 PNG)으로
    여러 크기를 담은 JoyConMouse.ico 를 만든다.
.DESCRIPTION
    아이콘 원본은 자체 제작 이미지다(닌텐도 로고·제품 이미지 미사용).
    트레이 상태 아이콘은 앱이 실행 중에 같은 원본에 상태 점을 덧그려 만든다(AppIconPainter).
    결과 파일은 저장소에 커밋되어 있으므로 원본을 바꿀 때만 다시 실행한다.
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$srcPath  = Join-Path $repoRoot 'src\JoyConMouse.App\Assets\icon.png'
$outPath  = Join-Path $repoRoot 'src\JoyConMouse.App\Assets\JoyConMouse.ico'
$sizes    = 16, 20, 24, 32, 40, 48, 64, 128, 256

$source = [System.Drawing.Bitmap]::FromFile($srcPath)
$images = @()
foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $sz, $sz, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($source, 0, 0, $sz, $sz)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    if ($sz -ge 256) {
        # 256px 는 PNG 압축(Windows Vista+ 표준)
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    else {
        # 작은 크기는 32bpp DIB: BITMAPINFOHEADER(높이 x2) + 아래에서 위로 BGRA + 1bpp AND 마스크
        $bw = New-Object System.IO.BinaryWriter $ms
        $maskRow = [int]([math]::Ceiling($sz / 32.0) * 4)
        $bw.Write([UInt32]40); $bw.Write([Int32]$sz); $bw.Write([Int32]($sz * 2))
        $bw.Write([UInt16]1); $bw.Write([UInt16]32); $bw.Write([UInt32]0)
        $bw.Write([UInt32]($sz * $sz * 4 + $maskRow * $sz)); $bw.Write([Int32]0); $bw.Write([Int32]0)
        $bw.Write([UInt32]0); $bw.Write([UInt32]0)
        for ($y = $sz - 1; $y -ge 0; $y--) {
            for ($x = 0; $x -lt $sz; $x++) {
                $c = $bmp.GetPixel($x, $y)
                $bw.Write([Byte]$c.B); $bw.Write([Byte]$c.G); $bw.Write([Byte]$c.R); $bw.Write([Byte]$c.A)
            }
        }
        for ($y = $sz - 1; $y -ge 0; $y--) {
            $row = New-Object byte[] $maskRow
            for ($x = 0; $x -lt $sz; $x++) {
                if ($bmp.GetPixel($x, $y).A -eq 0) { $row[[int][math]::Floor($x / 8)] = $row[[int][math]::Floor($x / 8)] -bor (0x80 -shr ($x % 8)) }
            }
            $bw.Write($row)
        }
        $bw.Flush()
    }
    $bmp.Dispose()
    $images += , $ms.ToArray()
}
$source.Dispose()

# ICO: ICONDIR(6) + ICONDIRENTRY(16)*n + PNG 데이터
$fs = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]; $data = $images[$i]
    $dim = if ($sz -ge 256) { 0 } else { $sz }
    $w.Write([Byte]$dim); $w.Write([Byte]$dim); $w.Write([Byte]0); $w.Write([Byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32)
    $w.Write([UInt32]$data.Length); $w.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($data in $images) { $w.Write([byte[]]$data) }
$w.Flush()
[System.IO.File]::WriteAllBytes($outPath, $fs.ToArray())
Write-Host "아이콘 생성: $outPath"
