using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace DrawGrid;

internal sealed class ZoomablePreviewControl : Panel
{
	private const float ZoomStep = 1.25F;
	private const float MinRelativeZoom = 1F;
	private const float MaxRelativeZoom = 16F;
	private Image? previewImage;
	private float fitScale = 1F;
	private float relativeZoom = 1F;
	private Size scaledImageSize = Size.Empty;
	private bool isPanning;
	private Point lastMousePosition;

	public ZoomablePreviewControl()
	{
		AutoScroll = true;
		BorderStyle = BorderStyle.FixedSingle;
		TabStop = true;
		SetStyle(
			ControlStyles.AllPaintingInWmPaint |
			ControlStyles.OptimizedDoubleBuffer |
			ControlStyles.ResizeRedraw |
			ControlStyles.Selectable |
			ControlStyles.UserPaint,
			true);
	}

	public event EventHandler? ZoomChanged;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Image? PreviewImage
	{
		get => previewImage;
		set
		{
			if (ReferenceEquals(previewImage, value))
			{
				return;
			}

			previewImage = value;
			relativeZoom = 1F;
			RecalculateLayout();
			OnZoomChanged();
		}
	}

	public float ZoomPercentage => GetCurrentScale() * 100F;

	protected override void OnResize(EventArgs eventArgs)
	{
		base.OnResize(eventArgs);
		RecalculateLayout();
		OnZoomChanged();
	}

	protected override void OnMouseEnter(EventArgs eventArgs)
	{
		base.OnMouseEnter(eventArgs);
		Focus();
		UpdateCursor();
	}

	protected override void OnMouseDown(MouseEventArgs eventArgs)
	{
		base.OnMouseDown(eventArgs);
		Focus();

		if (eventArgs.Button == MouseButtons.Left && CanPan())
		{
			isPanning = true;
			lastMousePosition = eventArgs.Location;
			Capture = true;
			Cursor = Cursors.SizeAll;
		}
	}

	protected override void OnMouseMove(MouseEventArgs eventArgs)
	{
		base.OnMouseMove(eventArgs);

		if (!isPanning)
		{
			UpdateCursor();
			return;
		}

		var currentScrollOffset = GetCurrentScrollOffset();
		var deltaX = eventArgs.X - lastMousePosition.X;
		var deltaY = eventArgs.Y - lastMousePosition.Y;
		var maxScrollOffset = GetMaxScrollOffset();
		var targetX = Math.Clamp(currentScrollOffset.X - deltaX, 0, maxScrollOffset.Width);
		var targetY = Math.Clamp(currentScrollOffset.Y - deltaY, 0, maxScrollOffset.Height);

		AutoScrollPosition = new Point(targetX, targetY);
		lastMousePosition = eventArgs.Location;
	}

	protected override void OnMouseUp(MouseEventArgs eventArgs)
	{
		base.OnMouseUp(eventArgs);

		if (eventArgs.Button == MouseButtons.Left)
		{
			isPanning = false;
			Capture = false;
			UpdateCursor();
		}
	}

	protected override void OnMouseLeave(EventArgs eventArgs)
	{
		base.OnMouseLeave(eventArgs);

		if (!isPanning)
		{
			Cursor = Cursors.Default;
		}
	}

	protected override void OnMouseWheel(MouseEventArgs eventArgs)
	{
		if (previewImage is null)
		{
			base.OnMouseWheel(eventArgs);
			return;
		}

		var newRelativeZoom = eventArgs.Delta > 0
			? Math.Min(relativeZoom * ZoomStep, MaxRelativeZoom)
			: Math.Max(relativeZoom / ZoomStep, MinRelativeZoom);

		if (Math.Abs(newRelativeZoom - relativeZoom) < float.Epsilon)
		{
			return;
		}

		var oldScale = GetCurrentScale();
		var oldRectangle = GetImageDisplayRectangle();
		var imageX = Math.Clamp((eventArgs.X - oldRectangle.X) / oldScale, 0F, previewImage.Width);
		var imageY = Math.Clamp((eventArgs.Y - oldRectangle.Y) / oldScale, 0F, previewImage.Height);

		relativeZoom = newRelativeZoom;
		RecalculateLayout();

		var newScale = GetCurrentScale();
		var centerOffsetX = Math.Max((ClientSize.Width - scaledImageSize.Width) / 2, 0);
		var centerOffsetY = Math.Max((ClientSize.Height - scaledImageSize.Height) / 2, 0);
		var maxScrollX = Math.Max(scaledImageSize.Width - ClientSize.Width, 0);
		var maxScrollY = Math.Max(scaledImageSize.Height - ClientSize.Height, 0);
		var scrollX = Math.Clamp((int)Math.Round(centerOffsetX + imageX * newScale - eventArgs.X), 0, maxScrollX);
		var scrollY = Math.Clamp((int)Math.Round(centerOffsetY + imageY * newScale - eventArgs.Y), 0, maxScrollY);

		AutoScrollPosition = new Point(scrollX, scrollY);
		OnZoomChanged();
	}

	protected override void OnPaint(PaintEventArgs paintEventArgs)
	{
		base.OnPaint(paintEventArgs);

		if (previewImage is null)
		{
			return;
		}

		paintEventArgs.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
		paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
		paintEventArgs.Graphics.DrawImage(previewImage, GetImageDisplayRectangle());
	}

	private void RecalculateLayout()
	{
		if (previewImage is null)
		{
			fitScale = 1F;
			scaledImageSize = Size.Empty;
			isPanning = false;
			AutoScrollMinSize = Size.Empty;
			AutoScrollPosition = Point.Empty;
			Cursor = Cursors.Default;
			Invalidate();
			return;
		}

		fitScale = CalculateFitScale();
		scaledImageSize = new Size(
			Math.Max(1, (int)Math.Round(previewImage.Width * GetCurrentScale())),
			Math.Max(1, (int)Math.Round(previewImage.Height * GetCurrentScale())));
		AutoScrollMinSize = scaledImageSize;
		ClampScrollOffset();
		UpdateCursor();
		Invalidate();
	}

	private float CalculateFitScale()
	{
		if (previewImage is null)
		{
			return 1F;
		}

		var availableWidth = Math.Max(ClientSize.Width, 1);
		var availableHeight = Math.Max(ClientSize.Height, 1);
		var widthScale = availableWidth / (float)previewImage.Width;
		var heightScale = availableHeight / (float)previewImage.Height;
		return Math.Min(Math.Min(widthScale, heightScale), 1F);
	}

	private float GetCurrentScale()
	{
		return fitScale * relativeZoom;
	}

	private Rectangle GetImageDisplayRectangle()
	{
		var x = AutoScrollPosition.X + Math.Max((ClientSize.Width - scaledImageSize.Width) / 2, 0);
		var y = AutoScrollPosition.Y + Math.Max((ClientSize.Height - scaledImageSize.Height) / 2, 0);
		return new Rectangle(x, y, scaledImageSize.Width, scaledImageSize.Height);
	}

	private bool CanPan()
	{
		return previewImage is not null && (scaledImageSize.Width > ClientSize.Width || scaledImageSize.Height > ClientSize.Height);
	}

	private Point GetCurrentScrollOffset()
	{
		return new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y);
	}

	private Size GetMaxScrollOffset()
	{
		return new Size(
			Math.Max(scaledImageSize.Width - ClientSize.Width, 0),
			Math.Max(scaledImageSize.Height - ClientSize.Height, 0));
	}

	private void ClampScrollOffset()
	{
		var currentScrollOffset = GetCurrentScrollOffset();
		var maxScrollOffset = GetMaxScrollOffset();
		var clampedX = Math.Clamp(currentScrollOffset.X, 0, maxScrollOffset.Width);
		var clampedY = Math.Clamp(currentScrollOffset.Y, 0, maxScrollOffset.Height);

		if (clampedX != currentScrollOffset.X || clampedY != currentScrollOffset.Y)
		{
			AutoScrollPosition = new Point(clampedX, clampedY);
		}
	}

	private void UpdateCursor()
	{
		Cursor = CanPan() ? Cursors.SizeAll : Cursors.Default;
	}

	private void OnZoomChanged()
	{
		ZoomChanged?.Invoke(this, EventArgs.Empty);
	}
}