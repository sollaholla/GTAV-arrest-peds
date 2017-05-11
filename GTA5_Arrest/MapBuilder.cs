using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTA5_Arrest
{
	public static class MapBuilder
	{
		internal static Dictionary<string, string> ScenarioDatabase = new Dictionary<string, string>
		{
			{"Drink Coffee",  "WORLD_HUMAN_AA_COFFEE"},
			{"Smoke", "WORLD_HUMAN_AA_SMOKE" },
			{"Smoke 2", "WORLD_HUMAN_SMOKING" },
			{"Binoculars",  "WORLD_HUMAN_BINOCULARS"},
			{"Bum", "WORLD_HUMAN_BUM_FREEWAY" },
			{"Cheering", "WORLD_HUMAN_CHEERING" },
			{"Clipboard", "WORLD_HUMAN_CLIPBOARD" },
			{"Drilling",  "WORLD_HUMAN_CONST_DRILL"},
			{"Drinking", "WORLD_HUMAN_DRINKING" },
			{"Drug Dealer", "WORLD_HUMAN_DRUG_DEALER"},
			{"Drug Dealer Hard", "WORLD_HUMAN_DRUG_DEALER_HARD" },
			{"Traffic Signaling",  "WORLD_HUMAN_CAR_PARK_ATTENDANT"},
			{"Filming", "WORLD_HUMAN_MOBILE_FILM_SHOCKING" },
			{"Leaf Blower", "WORLD_HUMAN_GARDENER_LEAF_BLOWER" },
			{"Golf Player", "WORLD_HUMAN_GOLF_PLAYER" },
			{"Guard Patrol", "WORLD_HUMAN_GUARD_PATROL" },
			{"Hammering", "WORLD_HUMAN_HAMMERING" },
			{"Janitor", "WORLD_HUMAN_JANITOR" },
			{"Musician", "WORLD_HUMAN_MUSICIAN" },
			{"Paparazzi", "WORLD_HUMAN_PAPARAZZI" },
			{"Party", "WORLD_HUMAN_PARTYING" },
			{"Picnic", "WORLD_HUMAN_PICNIC" },
			{"Push Ups", "WORLD_HUMAN_PUSH_UPS"},
			{"Shine Torch", "WORLD_HUMAN_SECURITY_SHINE_TORCH" },
			{"Sunbathe", "WORLD_HUMAN_SUNBATHE" },
			{"Sunbathe Back", "WORLD_HUMAN_SUNBATHE_BACK"},
			{"Tourist", "WORLD_HUMAN_TOURIST_MAP" },
			{"Mechanic", "WORLD_HUMAN_VEHICLE_MECHANIC" },
			{"Welding", "WORLD_HUMAN_WELDING" },
			{"Yoga", "WORLD_HUMAN_YOGA" },
		};

		public static InGameMap BuildMap(Map map)
		{
			InGameMap igMap = new InGameMap();

			foreach (MapObject mapObject in map.Objects)
			{
				switch (mapObject.MapObjectType)
				{
					case MapObjectType.Ped:
						igMap.Peds.Add(CreatePed(mapObject));
						break;
					case MapObjectType.Prop:
						igMap.Objects.Add(CreateObject(mapObject));
						break;
					case MapObjectType.Vehicle:
						igMap.Vehicles.Add(CreateVehicle(mapObject));
						break;
				}
				Script.Yield();
			}

			return igMap;
		}

		private static Ped CreatePed(MapObject mapObj)
		{
			Ped ped = World.CreatePed(mapObj.Hash, mapObj.Position + Vector3.WorldDown);
			if (!Entity.Exists(ped))
				ped = new Ped(0);
			ped.Rotation = mapObj.Rotation;
			ped.Quaternion = mapObj.Quaternion;
			ped.FreezePosition = !mapObj.Dynamic;

			if (mapObj.WeaponHash != null)
			{
				ped.Weapons.Give(mapObj.WeaponHash.Value, 0, true, true);
			}

			switch (mapObj.Action)
			{
				case "Any - Warp":
					Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD_WARP, ped.Handle, mapObj.Position.X, mapObj.Position.Y, mapObj.Position.Z, 100f, -1);
					break;
				case "Any - Walk":
				case "Any":
					Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD, ped.Handle, ped.Handle, mapObj.Position.X, mapObj.Position.Z, mapObj.Position.Z, 100f, -1);
					break;
				case "None":
					break;
				default:
					string scenario = ScenarioDatabase[mapObj.Action];
					Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped.Handle, scenario, 0, true);
					break;
			}

			return ped;
		}

		private static Prop CreateObject(MapObject mapObj)
		{
			Prop prop = World.CreateProp(mapObj.Hash, mapObj.Position, mapObj.Rotation, mapObj.Dynamic && mapObj.Door, false);
			if (!Entity.Exists(prop))
				prop = new Prop(0);
			prop.PositionNoOffset = mapObj.Position;
			prop.FreezePosition = !(mapObj.Dynamic && !mapObj.Door);
			prop.Quaternion = mapObj.Quaternion;
			return prop;
		}

		private static Vehicle CreateVehicle(MapObject mapObj)
		{
			Vehicle vehicle = World.CreateVehicle(mapObj.Hash, mapObj.Position);
			if (!Entity.Exists(vehicle))
				vehicle = new Vehicle(0);
			vehicle.Rotation = mapObj.Rotation;
			vehicle.Quaternion = mapObj.Quaternion;
			vehicle.PrimaryColor = (VehicleColor) mapObj.PrimaryColor;
			vehicle.SecondaryColor = (VehicleColor) mapObj.SecondaryColor;
			vehicle.SirenActive = mapObj.SirenActive;
			vehicle.FreezePosition = !mapObj.Dynamic;
			return vehicle;
		}
	}
}
