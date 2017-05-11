using System.Xml.Serialization;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTA5_Arrest
{
	public enum MapObjectType
	{
		Prop,
		Vehicle,
		Ped
	}

	public class MapObject : ISpatial
	{
		public MapObjectType MapObjectType { get; set; }
		public Vector3 Rotation { get; set; }
		public Vector3 Position { get; set; }
		public Quaternion Quaternion { get; set; }
		public int Hash { get; set; }
		public bool Dynamic { get; set; }
		public bool Door { get; set; }
		public string Action { get; set; }
		public string Relationship { get; set; }
		public WeaponHash? WeaponHash { get; set; }
		public bool SirenActive { get; set; }
		public int PrimaryColor { get; set; }
		public int SecondaryColor { get; set; }

		[XmlAttribute("Id")]
		public string Id { get; set; }
	}
}
