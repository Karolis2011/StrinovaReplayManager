#:package System.Drawing.Common@10.0.0

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

#pragma warning disable CA1416

string assetsDirectory = FindAssetsDirectory();
string sourcePath = Path.Combine(assetsDirectory, "SRM.png");

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Source image not found: {sourcePath}");
    return 1;
}

using Image sourceImage = Image.FromFile(sourcePath);

var pngTargets = new (string Name, int Width, int Height, float ContentScale)[]
{
    ("LockScreenLogo.scale-200.png", 48, 48, 1.0f),
    ("SplashScreen.scale-200.png", 1240, 600, 0.5f),
    ("Square150x150Logo.scale-200.png", 300, 300, 1.0f),
    ("Square44x44Logo.scale-200.png", 88, 88, 1.0f),
    ("Square44x44Logo.targetsize-24_altform-unplated.png", 24, 24, 1.0f),
    ("Square44x44Logo.targetsize-48_altform-lightunplated.png", 48, 48, 1.0f),
    ("StoreLogo.png", 50, 50, 1.0f),
    ("Wide310x150Logo.scale-200.png", 620, 300, 0.5f),
};

foreach (var target in pngTargets)
{
    string targetPath = Path.Combine(assetsDirectory, target.Name);
    using Bitmap bitmap = RenderImage(sourceImage, target.Width, target.Height, target.ContentScale);
    bitmap.Save(targetPath, ImageFormat.Png);
}

string iconPath = Path.Combine(assetsDirectory, "AppIcon.ico");
WriteIconFile(sourceImage, iconPath, [16, 24, 32, 48, 64, 128, 256]);

return 0;

static string FindAssetsDirectory()
{
    DirectoryInfo? directory = new DirectoryInfo(Environment.CurrentDirectory);

    while (directory is not null)
    {
        string candidate = Path.Combine(directory.FullName, "Assets", "SRM.png");
        if (File.Exists(candidate))
        {
            return Path.Combine(directory.FullName, "Assets");
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate the Assets directory containing SRM.png.");
}

static Bitmap RenderImage(Image sourceImage, int width, int height, float contentScale)
{
    Bitmap bitmap = new(width, height, PixelFormat.Format32bppPArgb);

    using (Graphics graphics = Graphics.FromImage(bitmap))
    {
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        float scale = Math.Min((float)width / sourceImage.Width, (float)height / sourceImage.Height) * contentScale;
        int drawWidth = Math.Max(1, (int)Math.Round(sourceImage.Width * scale));
        int drawHeight = Math.Max(1, (int)Math.Round(sourceImage.Height * scale));
        int x = (width - drawWidth) / 2;
        int y = (height - drawHeight) / 2;

        graphics.DrawImage(sourceImage, new Rectangle(x, y, drawWidth, drawHeight));
    }

    return bitmap;
}

static void WriteIconFile(Image sourceImage, string iconPath, IReadOnlyList<int> sizes)
{
    var iconImages = new List<(int Size, byte[] Data)>();

    foreach (int size in sizes)
    {
        using Bitmap bitmap = RenderImage(sourceImage, size, size, 1.0f);
        using MemoryStream stream = new();
        bitmap.Save(stream, ImageFormat.Png);
        iconImages.Add((size, stream.ToArray()));
    }

    using FileStream fileStream = File.Create(iconPath);
    using BinaryWriter writer = new(fileStream);

    writer.Write((short)0);
    writer.Write((short)1);
    writer.Write((short)iconImages.Count);

    int offset = 6 + (16 * iconImages.Count);

    foreach (var iconImage in iconImages)
    {
        writer.Write(iconImage.Size == 256 ? (byte)0 : (byte)iconImage.Size);
        writer.Write(iconImage.Size == 256 ? (byte)0 : (byte)iconImage.Size);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(iconImage.Data.Length);
        writer.Write(offset);
        offset += iconImage.Data.Length;
    }

    foreach (var iconImage in iconImages)
    {
        writer.Write(iconImage.Data);
    }
}