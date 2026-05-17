using Godot;

public partial class Debug : Node
{
	public const bool IsRelease = true;

	public static void Print(params object[] args)
	{
		if (IsRelease)
		{
			return;
		}

		GD.Print(args);
	}

	public static void PrintErr(params object[] args)
	{
		if (IsRelease)
		{
			return;
		}

		GD.PrintErr(args);
	}

	public static void PushError(object message)
	{
		if (IsRelease)
		{
			return;
		}

		GD.PushError(message?.ToString() ?? string.Empty);
	}
}

