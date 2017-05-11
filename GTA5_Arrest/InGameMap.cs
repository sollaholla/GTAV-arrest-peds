using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;

namespace GTA5_Arrest
{
	public class InGameMap
	{
		public InGameMap()
		{
			Objects = new List<Prop>();
			Vehicles = new List<Vehicle>();
			Peds = new List<Ped>();
			CurrentBlip = new Blip(0);
		}

		public List<Prop> Objects { get; }
		public List<Vehicle> Vehicles { get; }
		public List<Ped> Peds { get; }
		public Blip CurrentBlip { get; private set; }

		public Blip AddBlip()
		{
			// add all the lists together into one big ol' list.
			List<Entity> concat = Objects.Concat(Vehicles.Select(i => (Entity)i)).Concat(Peds).ToList();
			
			// now we convert each of the objects to a collection of vector3's
			Vector3[] vectors = concat.Select(i => i.Position).ToArray();
			Vector3 average = Vector3.Zero;
			for (int i = 0; i < vectors.Length; i++) {
				average += vectors[i];
			}
			average /= vectors.Length;
			return CurrentBlip = World.CreateBlip(average);
		}

		public void Remove()
		{
			while (Objects.Count > 0)
			{
				Prop p = Objects[Objects.Count - 1];
				p.Delete();
				Objects.RemoveAt(Objects.Count - 1);
			}

			while (Vehicles.Count > 0)
			{
				Vehicle v = Vehicles[Vehicles.Count - 1];
				v.Delete();
				Vehicles.RemoveAt(Vehicles.Count - 1);
			}

			while (Peds.Count > 0)
			{
				Ped p = Peds[Peds.Count - 1];
				p.Delete();
				Peds.RemoveAt(Peds.Count - 1);
			}

			if (Blip.Exists(CurrentBlip))
			{
				CurrentBlip.Remove();
			}
		}
	}
}
