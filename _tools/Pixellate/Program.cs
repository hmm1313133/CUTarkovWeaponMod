using System;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--info")
        {
            var img = Image.Identify(args[1]);
            Console.WriteLine($"size: {img.Width}x{img.Height}");
            return 0;
        }
        if (args.Length > 0 && args[0] == "--preview")
        {
            string src = args[1];
            string dst = args[2];
            int scale = args.Length > 3 ? int.Parse(args[3]) : 4;
            using var img = Image.Load<Rgba32>(src);
            int nw = img.Width * scale;
            int nh = img.Height * scale;
            img.Mutate(c => c.Resize(new ResizeOptions
            {
                Size = new Size(nw, nh),
                Sampler = KnownResamplers.NearestNeighbor,
            }));
            img.Save(dst);
            Console.WriteLine($"preview saved: {dst} ({nw}x{nh})");
            return 0;
        }

        // 支持 base64 编码的 UTF-8 路径（避免 PowerShell 中文路径乱码）
        string inputPath, outputPath;
        int targetW, targetH;
        if (args.Length > 0 && args[0] == "--b64")
        {
            inputPath = Encoding.UTF8.GetString(Convert.FromBase64String(args[1]));
            outputPath = Encoding.UTF8.GetString(Convert.FromBase64String(args[2]));
            targetW = int.Parse(args[3]);
            targetH = args.Length > 4 ? int.Parse(args[4]) : targetW;
        }
        else if (args.Length > 0 && args[0] == "--leatherman")
        {
            inputPath = @"g:\modmake\TKF_weapon\Framework\Assets\guns\leatherman\leatherman.webp";
            outputPath = @"g:\modmake\TKF_weapon\Framework\Assets\guns\leatherman\leatherman.png";
            targetW = int.Parse(args[1]);
            targetH = args.Length > 2 ? int.Parse(args[2]) : targetW;
        }
        else if (args.Length > 0 && args[0] == "--auto")
        {
            string dir = @"g:\modmake\TKF_weapon\Framework\Assets\guns\工具钳";
            var files = Directory.GetFiles(dir, "*.webp");
            if (files.Length == 0)
            {
                Console.WriteLine($"No webp found in {dir}");
                return 4;
            }
            inputPath = files[0];
            string outDir = @"g:\modmake\TKF_weapon\Framework\Assets\guns\leatherman";
            Directory.CreateDirectory(outDir);
            outputPath = Path.Combine(outDir, "leatherman.png");
            targetW = int.Parse(args[1]);
            targetH = args.Length > 2 ? int.Parse(args[2]) : targetW;
        }
        else
        {
            Console.WriteLine("usage: Pixellate <input> <output> <W> [H]  OR  --leatherman <W> [H]  OR  --b64 <inB64> <outB64> <W> [H]  OR  --auto <W> [H]  OR  --info <file>  OR  --preview <file> <out> [scale]");
            return 1;
        }

        return ProcessFile(inputPath, outputPath, targetW, targetH);
    }

    static int ProcessFile(string inputPath, string outputPath, int targetW, int targetH)
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Input not found: {inputPath}");
            return 3;
        }

        using var src = Image.Load<Rgba32>(inputPath);
        Console.WriteLine($"source: {src.Width}x{src.Height}");

        // 1. 找到非白色&非透明的 bbox
        int minX = src.Width, minY = src.Height, maxX = -1, maxY = -1;
        src.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref var p = ref row[x];
                    bool isWhite = p.R > 235 && p.G > 235 && p.B > 235;
                    if (!isWhite && p.A > 8)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }
        });

        if (maxX < minX || maxY < minY)
        {
            Console.WriteLine("No opaque content found, aborting.");
            return 2;
        }
        Console.WriteLine($"content bbox: ({minX},{minY}) - ({maxX},{maxY})");

        minX = Math.Max(0, minX - 2);
        minY = Math.Max(0, minY - 2);
        maxX = Math.Min(src.Width - 1, maxX + 2);
        maxY = Math.Min(src.Height - 1, maxY + 2);
        int cropW = maxX - minX + 1;
        int cropH = maxY - minY + 1;
        Console.WriteLine($"crop: {cropW}x{cropH}");

        src.Mutate(c => c.Crop(new Rectangle(minX, minY, cropW, cropH)));

        double ratio = (double)cropW / cropH;
        int fitW, fitH;
        if (ratio >= 1.0)
        {
            fitW = targetW;
            fitH = Math.Max(2, (int)Math.Round(targetW / ratio));
            if (fitH > targetH)
            {
                fitH = targetH;
                fitW = Math.Max(2, (int)Math.Round(targetH * ratio));
            }
        }
        else
        {
            fitH = targetH;
            fitW = Math.Max(2, (int)Math.Round(targetH * ratio));
            if (fitW > targetW)
            {
                fitW = targetW;
                fitH = Math.Max(2, (int)Math.Round(targetW / ratio));
            }
        }
        int smallW = Math.Max(6, fitW / 2);
        int smallH = Math.Max(4, fitH / 2);
        Console.WriteLine($"small: {smallW}x{smallH} -> fit: {fitW}x{fitH} -> canvas: {targetW}x{targetH}");

        src.Mutate(c => c.Resize(new ResizeOptions
        {
            Size = new Size(smallW, smallH),
            Sampler = KnownResamplers.Box,
        }));
        src.Mutate(c => c.Resize(new ResizeOptions
        {
            Size = new Size(fitW, fitH),
            Sampler = KnownResamplers.NearestNeighbor,
        }));

        using var canvas = new Image<Rgba32>(targetW, targetH);
        int ox = (targetW - fitW) / 2;
        int oy = (targetH - fitH) / 2;
        canvas.Mutate(c =>
        {
            var point = new Point(ox, oy);
            c.DrawImage(src, point, 1f);
        });

        var encoder = new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.NoCompression,
            TransparentColorMode = PngTransparentColorMode.Preserve,
        };
        using var fs = File.Create(outputPath);
        canvas.Save(fs, encoder);
        Console.WriteLine($"saved: {outputPath} ({canvas.Width}x{canvas.Height})");
        return 0;
    }
}