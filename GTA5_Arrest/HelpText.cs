using GTA.Native;

namespace GTA5_Arrest
{
	public static class HelpText
	{
		public static void Display(string textLabel, bool loop = true)
		{
			// Currently copied from shvdn 2.6.9 natives have incorrect names so falling back to hashcodes instead
			// names are displayed as comments next to their function calls.

			Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING"); // BEGIN_TEXT_COMMAND_DISPLAY_HELP

			const int maxStringLength = 99;

			for (int i = 0; i < textLabel.Length; i += maxStringLength)
			{
				Function.Call((Hash)0x6C188BE134E074AA, textLabel.Substring(i, System.Math.Min(maxStringLength, textLabel.Length - i))); // ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME
			}

			Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, loop, 1, -1); // END_TEXT_COMMAND_DISPLAY_HELP
		}

		public static bool IsActive()
		{
			return Function.Call<bool>(Hash.IS_HELP_MESSAGE_BEING_DISPLAYED);
		}

		public static void RemoveAll()
		{
			if (!IsActive())
				return;

			Function.Call(Hash.CLEAR_ALL_HELP_MESSAGES);
		}
	}
}
