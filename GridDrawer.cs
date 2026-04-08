using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace DrawGrid;

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
		return DrawGrid(
			imagePath,
			verticalSpacing,
			verticalOffset,
			horizontalSpacing,
			horizontalOffset,
			BuildOutputPath(imagePath));
	}

	public static string DrawGrid(
		string imagePath,
		int verticalSpacing,
		int verticalOffset,
		int horizontalSpacing,
		int horizontalOffset,
		string outputPath)
	{
		ValidateArguments(imagePath, verticalSpacing, verticalOffset, horizontalSpacing, horizontalOffset);
		var outputExtension = ValidateOutputPath(outputPath);
		using var canvas = CreateGridBitmap(imagePath, verticalSpacing, verticalOffset, horizontalSpacing, horizontalOffset);
		canvas.Save(outputPath, GetImageFormat(outputExtension));
		return outputPath;
	}

	public static string GetSuggestedOutputPath(string imagePath)
	{
		if (string.IsNullOrWhiteSpace(imagePath))
		{
			throw new ArgumentException("图片路径不能为空。", nameof(imagePath));
		}

		return BuildOutputPath(imagePath);
	}

	public static Bitmap CreateGridBitmap(
		string imagePath,
		int verticalSpacing,
		int verticalOffset,
		int horizontalSpacing,
		int horizontalOffset)
	{
		ValidateArguments(imagePath, verticalSpacing, verticalOffset, horizontalSpacing, horizontalOffset);

		using var sourceImage = Image.FromFile(imagePath);
		var canvas = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);

		using (var graphics = Graphics.FromImage(canvas))
		using (var solidPen = CreateGridPen(isDashed: false))
		using (var dashedPen = CreateGridPen(isDashed: true))
		{
			graphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;

			DrawVerticalLines(graphics, canvas.Width, canvas.Height, verticalOffset, verticalSpacing, solidPen, dashedPen);
			DrawHorizontalLines(graphics, canvas.Width, canvas.Height, horizontalOffset, horizontalSpacing, solidPen, dashedPen);
		}

		return canvas;
	}

	private static string ValidateArguments(
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

		return extension;
	}

	private static string ValidateOutputPath(string outputPath)
	{
		if (string.IsNullOrWhiteSpace(outputPath))
		{
			throw new ArgumentException("导出路径不能为空。", nameof(outputPath));
		}

		var extension = Path.GetExtension(outputPath);
		if (!SupportedExtensions.Contains(extension))
		{
			throw new NotSupportedException("导出文件仅支持 .png、.bmp、.jpg 和 .jpeg。\n");
		}

		var directory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException("导出目录不存在。");
		}

		return extension;
	}

	private static void DrawVerticalLines(Graphics graphics, int width, int height, int offset, int spacing, Pen solidPen, Pen dashedPen)
	{
		var (x, lineIndex) = GetFirstVisibleCoordinateAndLineIndex(offset, spacing);
		for (; x < width; x += spacing, lineIndex++)
		{
			var pen = IsOddLine(lineIndex) ? solidPen : dashedPen;
			graphics.DrawLine(pen, x, 0, x, height - 1);
		}
	}

	private static void DrawHorizontalLines(Graphics graphics, int width, int height, int offset, int spacing, Pen solidPen, Pen dashedPen)
	{
		var (y, lineIndex) = GetFirstVisibleCoordinateAndLineIndex(offset, spacing);
		for (; y < height; y += spacing, lineIndex++)
		{
			var pen = IsOddLine(lineIndex) ? solidPen : dashedPen;
			graphics.DrawLine(pen, 0, y, width - 1, y);
		}
	}

	private static Pen CreateGridPen(bool isDashed)
	{
		var pen = new Pen(Color.Red, 1F);
		if (isDashed)
		{
			pen.DashStyle = DashStyle.Dash;
			pen.DashPattern = new float[] { 4F, 4F };
		}

		return pen;
	}

	private static (int Coordinate, int LineIndex) GetFirstVisibleCoordinateAndLineIndex(int offset, int spacing)
	{
		var coordinate = offset;
		var lineIndex = 1;
		while (coordinate < 0)
		{
			coordinate += spacing;
			lineIndex++;
		}

		return (coordinate, lineIndex);
	}

	private static bool IsOddLine(int lineIndex)
	{
		return lineIndex % 2 != 0;
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