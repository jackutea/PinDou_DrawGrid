namespace DrawGrid;

internal sealed class MainForm : Form
{
	private const int InitialLeftPanelWidth = 420;
	private const int LeftPanelMinWidth = 380;
	private const int RightPanelMinWidth = 420;
	private readonly SplitContainer splitContainer;
	private readonly TextBox imagePathTextBox;
	private readonly NumericUpDown verticalSpacingInput;
	private readonly NumericUpDown verticalOffsetInput;
	private readonly NumericUpDown horizontalSpacingInput;
	private readonly NumericUpDown horizontalOffsetInput;
	private readonly TextBox outputPathTextBox;
	private readonly Button drawButton;
	private readonly PictureBox previewPictureBox;
	private readonly Label previewStatusLabel;

	public MainForm()
	{
		Text = "DrawGrid";
		StartPosition = FormStartPosition.CenterScreen;
		MinimumSize = new Size(960, 560);
		ClientSize = new Size(1100, 680);

		splitContainer = new SplitContainer
		{
			Dock = DockStyle.Fill,
			FixedPanel = FixedPanel.Panel1
		};

		var layout = new TableLayoutPanel
		{
			ColumnCount = 3,
			Dock = DockStyle.Top,
			Padding = new Padding(16),
			AutoSize = true
		};

		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));

		imagePathTextBox = new TextBox
		{
			Anchor = AnchorStyles.Left | AnchorStyles.Right,
			PlaceholderText = "请选择图片文件"
		};

		verticalSpacingInput = CreateNumericInput(50, 1, 1_000_000);
		verticalOffsetInput = CreateNumericInput(0, -1_000_000, 1_000_000);
		horizontalSpacingInput = CreateNumericInput(50, 1, 1_000_000);
		horizontalOffsetInput = CreateNumericInput(0, -1_000_000, 1_000_000);

		outputPathTextBox = new TextBox
		{
			Anchor = AnchorStyles.Left | AnchorStyles.Right,
			ReadOnly = true
		};

		drawButton = new Button
		{
			Text = "导出网格图",
			AutoSize = true,
			Anchor = AnchorStyles.Right
		};

		previewPictureBox = new PictureBox
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = Color.White,
			SizeMode = PictureBoxSizeMode.Zoom
		};

		previewStatusLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "选择图片后会在这里预览输出效果。",
			AutoEllipsis = true,
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(0, 6, 0, 0)
		};

		var browseButton = new Button
		{
			Text = "浏览...",
			AutoSize = true,
			Anchor = AnchorStyles.Left
		};

		browseButton.Click += BrowseButton_Click;
		drawButton.Click += DrawButton_Click;
		imagePathTextBox.TextChanged += PreviewInputChanged;
		verticalSpacingInput.ValueChanged += PreviewInputChanged;
		verticalOffsetInput.ValueChanged += PreviewInputChanged;
		horizontalSpacingInput.ValueChanged += PreviewInputChanged;
		horizontalOffsetInput.ValueChanged += PreviewInputChanged;

		layout.Controls.Add(CreateLabel("图片文件"), 0, 0);
		layout.Controls.Add(imagePathTextBox, 1, 0);
		layout.Controls.Add(browseButton, 2, 0);

		layout.Controls.Add(CreateLabel("竖线间隔像素"), 0, 1);
		layout.Controls.Add(verticalSpacingInput, 1, 1);

		layout.Controls.Add(CreateLabel("竖线起始偏移量"), 0, 2);
		layout.Controls.Add(verticalOffsetInput, 1, 2);

		layout.Controls.Add(CreateLabel("横线间隔像素"), 0, 3);
		layout.Controls.Add(horizontalSpacingInput, 1, 3);

		layout.Controls.Add(CreateLabel("横线起始偏移量"), 0, 4);
		layout.Controls.Add(horizontalOffsetInput, 1, 4);

		layout.Controls.Add(CreateLabel("输出文件"), 0, 5);
		layout.Controls.Add(outputPathTextBox, 1, 5);

		layout.Controls.Add(drawButton, 2, 6);

		for (var row = 0; row < 6; row++)
		{
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
		}

		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

		var previewLayout = new TableLayoutPanel
		{
			ColumnCount = 1,
			Dock = DockStyle.Fill,
			Padding = new Padding(16)
		};

		previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
		previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

		previewLayout.Controls.Add(CreateLabel("输出预览"), 0, 0);
		previewLayout.Controls.Add(previewPictureBox, 0, 1);
		previewLayout.Controls.Add(previewStatusLabel, 0, 2);

		splitContainer.Panel1.Controls.Add(layout);
		splitContainer.Panel2.Controls.Add(previewLayout);

		Controls.Add(splitContainer);
		AcceptButton = drawButton;
		Shown += MainForm_Shown;
		UpdatePreview();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ReplacePreviewImage(null);
		}

		base.Dispose(disposing);
	}

	private static Label CreateLabel(string text) => new()
	{
		Text = text,
		AutoSize = true,
		Anchor = AnchorStyles.Left,
		TextAlign = ContentAlignment.MiddleLeft
	};

	private static NumericUpDown CreateNumericInput(decimal value, decimal minimum, decimal maximum) => new()
	{
		Value = value,
		Minimum = minimum,
		Maximum = maximum,
		Anchor = AnchorStyles.Left | AnchorStyles.Right,
		ThousandsSeparator = true
	};

	private void BrowseButton_Click(object? sender, EventArgs e)
	{
		using var dialog = new OpenFileDialog
		{
			Title = "选择图片",
			Filter = "图片文件|*.png;*.bmp;*.jpg;*.jpeg|PNG 图片|*.png|BMP 图片|*.bmp|JPEG 图片|*.jpg;*.jpeg|所有文件|*.*",
			CheckFileExists = true,
			CheckPathExists = true
		};

		if (dialog.ShowDialog(this) == DialogResult.OK)
		{
			imagePathTextBox.Text = dialog.FileName;
		}
	}

	private void PreviewInputChanged(object? sender, EventArgs e)
	{
		UpdatePreview();
	}

	private void MainForm_Shown(object? sender, EventArgs e)
	{
		splitContainer.Panel1MinSize = LeftPanelMinWidth;
		splitContainer.Panel2MinSize = RightPanelMinWidth;

		var maxLeftWidth = Math.Max(LeftPanelMinWidth, splitContainer.ClientSize.Width - RightPanelMinWidth);
		splitContainer.SplitterDistance = Math.Min(Math.Max(InitialLeftPanelWidth, LeftPanelMinWidth), maxLeftWidth);
	}

	private void DrawButton_Click(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(imagePathTextBox.Text))
		{
			ShowError("请先选择图片文件。");
			return;
		}

		try
		{
			drawButton.Enabled = false;
			var outputPath = SelectOutputPath();
			if (outputPath is null)
			{
				return;
			}

			outputPathTextBox.Text = outputPath;

			GridDrawer.DrawGrid(
				imagePathTextBox.Text,
				DecimalToInt(verticalSpacingInput.Value),
				DecimalToInt(verticalOffsetInput.Value),
				DecimalToInt(horizontalSpacingInput.Value),
				DecimalToInt(horizontalOffsetInput.Value),
				outputPath);

			MessageBox.Show(this, $"网格图片已生成:\n{outputPath}", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
		finally
		{
			drawButton.Enabled = true;
		}
	}

	private void ShowError(string message)
	{
		MessageBox.Show(this, message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	private string? SelectOutputPath()
	{
		var suggestedPath = GridDrawer.GetSuggestedOutputPath(imagePathTextBox.Text);

		using var dialog = new SaveFileDialog
		{
			Title = "选择导出路径",
			Filter = "PNG 图片|*.png|BMP 图片|*.bmp|JPEG 图片|*.jpg;*.jpeg",
			AddExtension = true,
			OverwritePrompt = true,
			CheckPathExists = true,
			InitialDirectory = Path.GetDirectoryName(suggestedPath),
			FileName = Path.GetFileName(suggestedPath),
			DefaultExt = Path.GetExtension(suggestedPath).TrimStart('.')
		};

		return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
	}

	private void UpdatePreview()
	{
		outputPathTextBox.Clear();

		if (string.IsNullOrWhiteSpace(imagePathTextBox.Text))
		{
			ReplacePreviewImage(null);
			previewStatusLabel.Text = "选择图片后会在这里预览输出效果。";
			return;
		}

		try
		{
			var previewImage = GridDrawer.CreateGridBitmap(
				imagePathTextBox.Text,
				DecimalToInt(verticalSpacingInput.Value),
				DecimalToInt(verticalOffsetInput.Value),
				DecimalToInt(horizontalSpacingInput.Value),
				DecimalToInt(horizontalOffsetInput.Value));

			ReplacePreviewImage(previewImage);
			previewStatusLabel.Text = $"预览已更新，尺寸：{previewImage.Width} x {previewImage.Height}";
		}
		catch (Exception ex)
		{
			ReplacePreviewImage(null);
			previewStatusLabel.Text = $"无法预览：{ex.Message}";
		}
	}

	private void ReplacePreviewImage(Image? image)
	{
		var oldImage = previewPictureBox.Image;
		previewPictureBox.Image = image;
		oldImage?.Dispose();
	}

	private static int DecimalToInt(decimal value) => decimal.ToInt32(value);
}