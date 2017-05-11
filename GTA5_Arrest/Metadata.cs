using GTA;

namespace GTA5_Arrest
{
	public class Metadata
	{
		public Metadata()
		{
			Creator = Game.Player.Name;
			Name = "Nameless Map";
			Description = string.Empty;
		}

		public string Creator { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}
}
