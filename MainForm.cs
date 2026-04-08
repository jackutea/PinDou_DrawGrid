namespace DrawGrid;

internal sealed class MainForm : Form
{
	private readonly TextBox imagePathTextBox;
	private readonly NumericUpDown verticalSpacingInput;
	private readonly NumericUpDown verticalOffsetInput;
	private readonly NumericUpDown horizontalSpacingInput;
	private readonly NumericUpDown horizontalOffsetInput;
	private readonly TextBox outputPathTextBox;
	private readonly Button drawButton;

	public MainForm()
	{
		Text = "DrawGrid";
		StartPosition = FormStartPosition.CenterScreen;
		MinimumSize = new Size(640, 320);
		ClientSize = new Size(720, 320);

		var layout = new TableLayoutPanel
		{
			ColumnCount = 3,
			Dock = DockStyle.Fill,
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
			Text = "生成网格图",
			AutoSize = true,
			Anchor = AnchorStyles.Right
		};

		var browseButton = new Button
		{
			Text = "浏览...",
			AutoSize = true,
			Anchor = AnchorStyles.Left
		};

		browseButton.Click += BrowseButton_Click;
		drawButton.Click += DrawButton_Click;

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

		Controls.Add(layout);
		AcceptButton = drawButton;
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
			outputPathTextBox.Clear();

			var outputPath = GridDrawer.DrawGrid(
				imagePathTextBox.Text,
				DecimalToInt(verticalSpacingInput.Value),
				DecimalToInt(verticalOffsetInput.Value),
				DecimalToInt(horizontalSpacingInput.Value),
				DecimalToInt(horizontalOffsetInput.Value));

			outputPathTextBox.Text = outputPath;
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

	private static int DecimalToInt(decimal value) => decimal.ToInt32(value);
}