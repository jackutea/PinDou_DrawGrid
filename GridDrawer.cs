using System.Drawing;
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
		var extension = ValidateArguments(imagePath, verticalSpacing, verticalOffset, horizontalSpacing, horizontalOffset);
		using var canvas = CreateGridBitmap(imagePath, verticalSpacing, verticalOffset, horizontalSpacing, horizontalOffset);
		var outputPath = BuildOutputPath(imagePath);
		canvas.Save(outputPath, GetImageFormat(extension));
		return outputPath;
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