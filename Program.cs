namespace DrawGrid;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		try
		{
			ApplicationConfiguration.Initialize();
			Application.Run(new MainForm());
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				ex.ToString(),
				"启动失败",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
	}
}
