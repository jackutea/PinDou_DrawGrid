using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

if (args.Length == 0)
{
	PrintUsage();
	return;
}

if (args.Length != 5 ||
	!int.TryParse(args[1], out var verticalSpacing) ||
	!int.TryParse(args[2], out var verticalOffset) ||
	!int.TryParse(args[3], out var horizontalSpacing) ||
	!int.TryParse(args[4], out var horizontalOffset))
{
	Console.Error.WriteLine("参数无效。请按要求提供图片路径和 4 个整数参数。\n");
	PrintUsage();
	Environment.ExitCode = 1;
	return;
}

try
{
	if (!OperatingSystem.IsWindows())
	{
		Console.Error.WriteLine("当前实现依赖 System.Drawing，仅支持 Windows。");
		Environment.ExitCode = 1;
		return;
	}

	var outputPath = GridDrawer.DrawGrid(
		args[0],
		verticalSpacing,
		verticalOffset,
		horizontalSpacing,
		horizontalOffset);

	Console.WriteLine($"网格图片已生成: {outputPath}");
}
catch (Exception ex)
{
	Console.Error.WriteLine($"处理失败: {ex.Message}");
	Environment.ExitCode = 1;
}

static void PrintUsage()
{
	Console.WriteLine("用法:");
	Console.WriteLine("  DrawGrid <图片路径> <竖线间隔像素> <竖线起始偏移量> <横线间隔像素> <横线起始偏移量>");
	Console.WriteLine();
	Console.WriteLine("示例:");
	Console.WriteLine("  DrawGrid sample.png 50 0 50 0");
}

[SupportedOSPlatform("windows")]
internal static class GridDrawer
{
	private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".png",
		".bmp",
		".jpg",
		".jpeg"
	};

	public static string DrawGrid(
		string imagePath,
		int verticalSpacing,
		int verticalOffset,
		int horizontalSpacing,
		int horizontalOffset)
	{
		if (!OperatingSystem.IsWindows())
		{
			throw new PlatformNotSupportedException("当前实现依赖 System.Drawing，仅支持 Windows。");
		}

		if (string.IsNullOrWhiteSpace(imagePath))
		{
			throw new ArgumentException("图片路径不能为空。", nameof(imagePath));
		}

		if (!File.Exists(imagePath))
		{
			throw new FileNotFoundException("找不到指定图片。", imagePath);
		}

		if (verticalSpacing <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(verticalSpacing), "竖线间隔必须大于 0。");
		}

		if (horizontalSpacing <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(horizontalSpacing), "横线间隔必须大于 0。");
		}

		var extension = Path.GetExtension(imagePath);
		if (!SupportedExtensions.Contains(extension))
		{
			throw new NotSupportedException("仅支持 .png、.bmp、.jpg 和 .jpeg 图片。");
		}

		using var sourceImage = Image.FromFile(imagePath);
		using var canvas = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);

		using (var graphics = Graphics.FromImage(canvas))
		using (var lineBrush = new SolidBrush(Color.Red))
		{
			graphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);

			for (var x = GetFirstVisibleCoordinate(verticalOffset, verticalSpacing); x < canvas.Width; x += verticalSpacing)
			{
				graphics.FillRectangle(lineBrush, x, 0, 1, canvas.Height);
			}

			for (var y = GetFirstVisibleCoordinate(horizontalOffset, horizontalSpacing); y < canvas.Height; y += horizontalSpacing)
			{
				graphics.FillRectangle(lineBrush, 0, y, canvas.Width, 1);
			}
		}

		var outputPath = BuildOutputPath(imagePath);
		canvas.Save(outputPath, GetImageFormat(extension));
		return outputPath;
	}

	private static int GetFirstVisibleCoordinate(int offset, int spacing)
	{
		var coordinate = offset;
		while (coordinate < 0)
		{
			coordinate += spacing;
		}

		return coordinate;
	}

	private static string BuildOutputPath(string imagePath)
	{
		var directory = Path.GetDirectoryName(imagePath) ?? Environment.CurrentDirectory;
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);
		var extension = Path.GetExtension(imagePath);
		var outputPath = Path.Combine(directory, $"{fileNameWithoutExtension}_grid{extension}");
		var index = 1;

		while (File.Exists(outputPath))
		{
			outputPath = Path.Combine(directory, $"{fileNameWithoutExtension}_grid_{index}{extension}");
			index++;
		}

		return outputPath;
	}

	private static ImageFormat GetImageFormat(string extension) => extension.ToLowerInvariant() switch
	{
		".png" => ImageFormat.Png,
		".bmp" => ImageFormat.Bmp,
		".jpg" or ".jpeg" => ImageFormat.Jpeg,
		_ => throw new NotSupportedException("仅支持 .png、.bmp、.jpg 和 .jpeg 图片。")
	};
}
