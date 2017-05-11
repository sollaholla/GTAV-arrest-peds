using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using GTA;
using GTA.Math;
using GTA.Native;
using Control = GTA.Control;

namespace GTA5_Arrest
{
	public class PlayerBehaviour : Script
	{
		private readonly Vector3 _offsetOfPedInPrisonCellWhenAttached;
		private readonly float _rotationOffset = 0;
		private readonly int _timeUntilPedDecidesToRunAway = 10000;
		private readonly string _prisonCellModelName = "modernprisoncell";
		private readonly bool _loadMapOnStart = true;
		private readonly string _mapName = "./scripts/GTA5_Arrest_Maps/prison.xml";
		private InGameMap _inGameMap;
		private Model _prisonCellModel;

		// Ewwwwww, flags... 
		// but it's all i can really think of that's gonna allow the help text to be displayed
		// in the brief menu properly..
		private bool _removedHelp1 = true;
		private bool _removedHelp2 = true;
		private bool _removedHelp3 = true;
		private bool _initialized = false;

		public PlayerBehaviour()
		{
			Tick += OnTick;
			Aborted += OnAborted;

			string v3 = Settings.GetValue("prop", "offset_of_ped_in_prison_cell_when_attached");
			_offsetOfPedInPrisonCellWhenAttached = V3Parse.Read(v3);
			_timeUntilPedDecidesToRunAway = Settings.GetValue("ped", "time_until_ped_decides_to_run_away", _timeUntilPedDecidesToRunAway);
			_prisonCellModelName = Settings.GetValue("prop", "prison_cell_model", _prisonCellModelName);
			_rotationOffset = Settings.GetValue("prop", "rotation_offset", _rotationOffset);
			_loadMapOnStart = Settings.GetValue("map", "load_map_on_start", _loadMapOnStart);
			_mapName = Settings.GetValue("map", "path_to_mapeditor_file", _mapName);

			Settings.SetValue("prop", "offset_of_ped_in_prison_cell_when_attached", _offsetOfPedInPrisonCellWhenAttached);
			Settings.SetValue("ped", "time_until_ped_decides_to_run_away", _timeUntilPedDecidesToRunAway);
			Settings.SetValue("prop", "prison_cell_model", _prisonCellModelName);
			Settings.SetValue("prop", "rotation_offset", _rotationOffset);
			Settings.SetValue("map", "load_map_on_start", _loadMapOnStart);
			Settings.SetValue("map", "path_to_mapeditor_file", _mapName);
			Settings.Save();

			// we're gonna use this so later on we can get the hash of the prison cell model, and ask the game
			// if there are any of them nearby using GET_CLOSEST_OBJECT_OF_TYPE
			_prisonCellModel = new Model(_prisonCellModelName);
			_prisonCellModel.Request();

			// We're going to request the dictionary we need for later use in this script.
			Function.Call(Hash.REQUEST_ANIM_DICT, "mp_arresting");
			Function.Call(Hash.REQUEST_ANIM_DICT, "random@arrests");

			// DEBUG: Just using this for testing so that I can spawn some peds and debug.
			//KeyUp += (sender, args) =>
			//{
			//	if (args.KeyCode == Keys.K)
			//	{
			//		World.CreatePed(PedHash.Beach02AMY, Player.Character.Position + Player.Character.ForwardVector * 2);
			//	}
			//};

			PedsArrestedByPlayer = new List<Ped>();
		}

		public Player Player => Game.Player;
		public List<Ped> PedsArrestedByPlayer { get; set; }

		private void OnTick(object sender, EventArgs eventArgs)
		{
			MakePedsPutTheirHandsUp();
			ArrestPedsWithTheirHandsUp();
			PutPedsIntoPrisonCells();
			LetGoCuffedPeds();

			// we want to clean our list of any peds that are dead or fleeing.
			PedsArrestedByPlayer = PedsArrestedByPlayer.Where(x => !x.IsDead && !x.IsFleeing).ToList();

			// let's put this at the bottom so that when the help text gets cleared
			// in the first frame it's not going to affect the meta data being displayed
			// on screen.
			if (!_initialized && _loadMapOnStart)
			{
				LoadMap();
				_initialized = true;
			}
		}

		private void OnAborted(object sender, EventArgs eventArgs)
		{
			// reset any peds that were arrested by the player
			// so that they can return to their 64bit lives.
			ResetPeds();

			// remove the animations from memory.
			RemoveAnimsFromMem();

			// also remove the models from memory.
			if (_prisonCellModel != null)
				_prisonCellModel.MarkAsNoLongerNeeded();

			// remove the ingame map if it exists.
			_inGameMap?.Remove();
		}

		/// <summary>
		/// This functions purpose is to allow the player to do a sort of "arrest stop" action.
		/// This way the peds we are aiming at, put their hands up and give us the oppertunity to arrest them.
		/// </summary>
		private void MakePedsPutTheirHandsUp()
		{
			// We're going to get the ped that the player is targetting,
			// and with that ped, we're going to give them a task sequence
			// so that they:
			// 1. Put hands up for (some seconds???)
			// 2. Run away from player.

			// We probably don't want to do it "auto-magically" maybe we press a button while aiming at the ped, and then
			// the player says something, and there's some random chance that the ped will either run away or fight the player.
			// If the ped has a weapon and is shooting the player we might not want the ped to put their hands up, because
			// it's more realistic.

			// we want to make sure the players currentPedGroup isn't over 7
			if (Player.Character.CurrentPedGroup.MemberCount >= 7)
				return;

			// get the ped
			Ped targettedPed = Player.GetTargetedEntity() as Ped;

			if (!Entity.Exists(targettedPed))
				targettedPed = Player.Character.GetMeleeTarget();

			// make sure this ped is not null
			// we also want to make sure this ped doesn't already have his/her hands up.
			// AND we want to check to see if this ped isn't already in the players ped group.
			if (Entity.Exists(targettedPed) && Player.Character.IsInRangeOf(targettedPed.Position, 15) && !PedsArrestedByPlayer.Contains(targettedPed) && !IsPedInPlayerGroup(targettedPed) && !targettedPed.IsDead && !targettedPed.IsAttached())
			{
				// check if the ped is carrying a weapon (making sure we're not in a fire fight too)
				if (!targettedPed.IsInCombatAgainst(Player.Character) || targettedPed.Weapons.Current.Hash == WeaponHash.Unarmed)
				{
					// draw a marker over the ped.
					World.DrawMarker(MarkerType.UpsideDownCone, targettedPed.Position + Vector3.WorldUp, new Vector3(0, 1, 0), Vector3.Zero, new Vector3(0.25f, 0.25f, 0.25f), Color.Blue, true, false, 9, true, string.Empty, string.Empty, false);

					// disable the talking controls
					Game.DisableControlThisFrame(2, Control.Talk);

					// it makes sense to use the "talk" button (imo)
					if (Game.IsDisabledControlJustPressed(2, Control.Talk))
					{
						// make the player do some audio queing.
						Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Player.Character.Handle, "Generic_Insult_High", "Speech_Params_Force");

						// initialize the arrest sequence
						TaskSequence arrestedPedSequence = new TaskSequence();
						if (!targettedPed.IsInVehicle())
							arrestedPedSequence.AddTask.ClearAllImmediately();
						else arrestedPedSequence.AddTask.ClearAll();
						arrestedPedSequence.AddTask.LookAt(Player.Character, _timeUntilPedDecidesToRunAway);
						arrestedPedSequence.AddTask.HandsUp(_timeUntilPedDecidesToRunAway);
						arrestedPedSequence.AddTask.FleeFrom(Player.Character);
						arrestedPedSequence.AddTask.ClearLookAt();
						arrestedPedSequence.Close(false);

						// make the ped perform the sequence.
						targettedPed.Task.PerformSequence(arrestedPedSequence);

						var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);

						while (targettedPed.TaskSequenceProgress == -1)
						{
							if (DateTime.UtcNow > timeout)
								break;

							Yield();
						}

						// make sure to dispose of this sequence.
						arrestedPedSequence.Dispose();

						// now we keep track of this one so we can do some operations with this ped later on.
						PedsArrestedByPlayer.Add(targettedPed);
					}
				}

				// draw a marker over the ped
				// check if we pressed a button
			}
		}

		/// <summary>
		/// This function is going to handle arresting the peds whom have their hands already up.
		/// Earlier we added peds to a list so that we can keep track of who already has their hands up, 
		/// this is going to be really useful at this point, because now we can get the closest ped in that
		/// list and actually arrest him/her.
		/// </summary>
		private void ArrestPedsWithTheirHandsUp()
		{
			// 1. Get the closest ped whom is contained in the peds list.
			// 2. GTA doesn't allow the "Player" to arrest other peds, so we're going to need to make our own sequence, using 
			//		task sequences and the advanced animation native.
			// 3. Once we have the ped under arrest, we want to make this ped follow us, as a "bodyguard". (get in our vehicle, go where we go etc.)
			// 4. We have a settings value for the prop name of the prison cell, and we also have a settings value for the props attachment offset.
			//		We need this to get the correct position in the "cell" and make sure that the cell we want to get is actually our desired prop.
			// 5. Once the ped is in the cell, we'll give him/her a random scenario from a list of previously selected scenarios. This is also a toggelable setting
			//		which the user can choose to use or not.

			// lets get the closest ped in our peds list.
			Ped closestPedWithHandsUp = World.GetClosest(Player.Character.Position, PedsArrestedByPlayer.ToArray());

			// lets keep a constant here that will be used as the text that we display on screen when we are about to arrest the ped in question.
			const string textLabel = "Press ~INPUT_ARREST~ to begin arrest.";

			// now lets check to make sure this ped is not null, and make sure the player is in proper range of the ped.
			// to check for "range" we're using a vector to make sure the direction we're approaching the ped is from behind.
			// we also want to make sure that this ped is not part of our ped group already.
			if (Entity.Exists(closestPedWithHandsUp) && Player.Character.IsInRangeOf(closestPedWithHandsUp.Position, 1.5f) && !IsPedInPlayerGroup(closestPedWithHandsUp))
			{
				// then we're going to wait for a button press. we're using the arrest action, because that just makes more sense.
				Game.DisableControlThisFrame(2, Control.Arrest);

				// let's make sure that the player is not trying to enter any vehicle at this time.
				Function.Call(Hash.SET_PLAYER_MAY_NOT_ENTER_ANY_VEHICLE, Player.Handle);

				// first before we display the help text, let's make sure it's not visible already.
				if (!HelpText.IsActive())
				{
					_removedHelp1 = false;

					// now lets display some help text on screen to show the player what he needs to do to arrest this ped.
					HelpText.Display(textLabel);
				}

				// check to see if we've pressed the arrest button
				if (Game.IsDisabledControlJustPressed(2, Control.Arrest))
				{
					// The animation will work like this:
					// 1. the player will wait for the peds animation to finish.
					// 2. the ped will play the get on floor animation (at the players position).
					// 3. the player will play the cop animation.
					// 4. the ped will play the crook animation.
					// 5. then we set the peds movement clipset to be the cuffed movement clipset.
					// 6. then we make the ped follow us by setting him/her in our relationship group.
					// We're going to need to disable collision or freeze position of our peds until while they're playing the arrest anims.

					// cop animation is: arrest_on_floor_front_left_a
					// ped animation is: arrest_on_floor_front_left_b

					// we wait the duration of the first animation played by the ped in question.
					TaskSequence copArrestPedSequence = new TaskSequence();
					copArrestPedSequence.AddTask.ClearAllImmediately();
					copArrestPedSequence.AddTask.LookAt(closestPedWithHandsUp);
					copArrestPedSequence.AddTask.TurnTo(closestPedWithHandsUp, (int)(Function.Call<float>(Hash._GET_ANIM_DURATION, "random@arrests", "idle_2_hands_up") * 1000));
					copArrestPedSequence.AddTask.ClearLookAt();
					Vector3 copPos = Player.Character.Position;
					var copDuration = (int)(Function.Call<float>(Hash._GET_ANIM_DURATION, "mp_arresting", "arrest_on_floor_front_left_a") * 1000);
					Function.Call(Hash.TASK_PLAY_ANIM_ADVANCED, 0, "mp_arresting", "arrest_on_floor_front_left_a",
						copPos.X, copPos.Y, copPos.Z, 0, 0, Player.Character.Rotation.Z, 1.0f, 1.0f, copDuration, 0, 0f, 0, 0);
					copArrestPedSequence.Close(false);

					// there's no FULL documentation on TASK_PLAY_ANIM_ADVANCED so some the params I'm just guessing at. Seems to work just fine though.
					TaskSequence crookGetArrestedSequence = new TaskSequence();
					crookGetArrestedSequence.AddTask.ClearAllImmediately();
					Vector3 arrestPos = Player.Character.Position +
										(closestPedWithHandsUp.Position - Player.Character.Position).Normalized * .8f + (Player.Character.RightVector * 0.5f);
					crookGetArrestedSequence.AddTask.PlayAnimation("random@arrests", "idle_2_hands_up");
					var crookDuration = (int)(Function.Call<float>(Hash._GET_ANIM_DURATION, "mp_arresting", "arrest_on_floor_front_left_b") * 1000);
					Function.Call(Hash.TASK_PLAY_ANIM_ADVANCED, 0, "mp_arresting", "arrest_on_floor_front_left_b",
						arrestPos.X, arrestPos.Y, arrestPos.Z, 0, 0, Player.Character.Rotation.Z - 45, 1.0f, 1.0f, crookDuration, 0, 0f, 0, 0);
					crookGetArrestedSequence.AddTask.PlayAnimation("mp_arresting", "idle", 4.0f, -4.0f, -1, (AnimationFlags)49, 0.0f);
					crookGetArrestedSequence.Close(false);

					// tell the peds to perform their sequences
					Player.Character.Task.PerformSequence(copArrestPedSequence);
					closestPedWithHandsUp.Task.PerformSequence(crookGetArrestedSequence);

					// We HAVE to dispose of these, because there's a limit to the amount of task sequences you can
					// have during the current game. They stay in memory until you dispose of them, so this
					// again, mgiht help someone with a low-end pc or heavily modded game
					copArrestPedSequence.Dispose();
					crookGetArrestedSequence.Dispose();

					// lets set some of the arrested peds properties, so that he doesn't lose the mp_arresting/idle animation
					closestPedWithHandsUp.CanRagdoll = false;
					closestPedWithHandsUp.CanPlayGestures = false;

					// now we want to add the ped to our ped group
					Player.Character.CurrentPedGroup.Add(closestPedWithHandsUp, false);

					// then let's yield the script since we're using this same control action somewhere else.
					Yield();
				}
			}
			else
			{
				// now that there's no entity to arrest, let's remove the help text we had displayed previously.
				if (!_removedHelp1 && HelpText.IsActive())
				{
					// remove all help text, since we can't target one specifically.
					HelpText.RemoveAll();
					_removedHelp1 = true;
				}
			}
		}

		/// <summary>
		/// This method simple goes through all the tasked peds and clears their current task
		/// so that they aren't left running around in the game.
		/// </summary>
		private void ResetPeds()
		{
			int count = PedsArrestedByPlayer.Count;
			while (count > 0)
			{
				Ped ped = PedsArrestedByPlayer[count - 1];
				ped.Task.ClearAll();
				ped.MarkAsNoLongerNeeded();
				ped.CurrentPedGroup?.Dispose();
				ResetPedFlags(ped);
				count--;
			}
		}

		// this resets a peds flags, since we changed them earlier.
		private static void ResetPedFlags(Ped ped)
		{
			ped.CanRagdoll = true;
			ped.BlockPermanentEvents = false;
		}

		/// <summary>
		/// This function simply takes the animation dicts that we instantiated in the constructor and
		/// removes them from game memory. Idk whether this matters or not, but I have a feeling
		/// someone with a heavily modded gta will appreciate this cleanup.
		/// </summary>
		private void RemoveAnimsFromMem()
		{
			// remove the animation dicts we loaded earlier
			Function.Call(Hash.REMOVE_ANIM_DICT, "mp_arresting");
			Function.Call(Hash.REMOVE_ANIM_DICT, "random@arrests");
			Player.Character.Task.ClearAll();
		}

		/// <summary>
		/// purpose of this is to allow the player to put peds into the prison cells (models defined in settings)
		/// </summary>
		private void PutPedsIntoPrisonCells()
		{
			// here's how I'd like this to go:
			// 1. We find the closest prop with the "prisoncellmodel"
			// 2. Once we go up to it, we press e on the cell, and it will teleport a ped into it. (the first ped in the array) (maybe we do a screen fade or something??)
			// 3. Then it's complete and we can continue to do it over and over but we need to make sure that there's no peds attached to the given prop before doing that.

			Prop closestPrisonCellProp;

			// Make sure the closest prop exists and that there's no ped "attached to that point", the way we check for that is by seeing if
			// there's a ped at the offset from the prop, which (the offset) was defined in the settings.
			if (GetClosestPrisonCell(out closestPrisonCellProp) && Player.Character.CurrentPedGroup.Contains(PedsArrestedByPlayer[0]))
			{
				if (!HelpText.IsActive())
				{
					_removedHelp2 = false;

					// display the help text.
					HelpText.Display("Press ~INPUT_CONTEXT~ to place a ped in this cell.");
				}

				// We're disabling both these controls since their both "relevant", and in most cases bound to the same input button.
				Game.DisableControlThisFrame(2, Control.Talk);
				Game.DisableControlThisFrame(2, Control.Context);

				// let's check for the control press.
				if (Game.IsDisabledControlJustPressed(2, Control.Context))
				{
					Ped firstPedInStack = PedsArrestedByPlayer[0];

					// BOOM, we jail'd that sum bitch. KKona!
					firstPedInStack.Task.ClearAllImmediately();
					firstPedInStack.AttachTo(closestPrisonCellProp, 0, _offsetOfPedInPrisonCellWhenAttached, new Vector3(0, 0, _rotationOffset));
					firstPedInStack.LeaveGroup();
					PedsArrestedByPlayer.RemoveAt(0);
				}
			}
			else
			{
				// now as we've done before let's remove the help text.
				if (!_removedHelp2 && HelpText.IsActive())
				{
					HelpText.RemoveAll();
					_removedHelp2 = true;
				}
			}

		}

		/// <summary>
		/// This function simply allows us to release any peds we've already arrested.
		/// </summary>
		private void LetGoCuffedPeds()
		{
			// we get the closest ped contained in the PedsArrestedByPlayer list.
			Ped closestPed = World.GetClosest(Player.Character.Position, PedsArrestedByPlayer.ToArray());

			// if the ped exists and he's close enough to us, we want to allow the player to release them.
			if (Entity.Exists(closestPed) && closestPed.CurrentPedGroup == Player.Character.CurrentPedGroup && closestPed.Position.DistanceTo(Player.Character.Position) < 1.75f)
			{
				// make sure to display help text to let the player know we can release this ped.
				if (!HelpText.IsActive())
				{
					_removedHelp3 = false;

					HelpText.Display("Press ~INPUT_ARREST~ to release this ped.");
				}

				// if we press the arrest hotkey then make the ped flee from us or attack us.
				Game.DisableControlThisFrame(2, Control.Arrest);
				if (Game.IsDisabledControlJustPressed(2, Control.Arrest))
				{
					// clear the players task in case we where arresting the ped.
					Player.Character.Task.ClearAll();

					PedsArrestedByPlayer.Remove(closestPed);
					closestPed.LeaveGroup();
					closestPed.Task.ClearAllImmediately();
					ResetPedFlags(closestPed);

					// If the ped has any weapons than we want the ped to equip that weapon
					// and start attacking us.
					WeaponHash weaponFound;
					if (HasPedGotAnyWeapon(closestPed, out weaponFound))
					{
						closestPed.Weapons.Select(weaponFound, true);
						closestPed.Task.FightAgainst(Player.Character);
					}
					else closestPed.Task.FleeFrom(Player.Character);
				}
			}
			else
			{
				if (!_removedHelp3 && HelpText.IsActive())
				{
					HelpText.RemoveAll();
					_removedHelp3 = true;
				}
			}
		}

		/// <summary>
		/// this is a basic function. it just does some null checks and trys to get the closest prison cell.
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		private bool GetClosestPrisonCell(out Prop cell)
		{
			if (PedsArrestedByPlayer.Count <= 0 || string.IsNullOrEmpty(_prisonCellModelName) || _prisonCellModel == null)
			{
				cell = null;
				return false;
			}

			// Let's get the closest prison cell.
			Vector3 playerPos = Player.Character.Position;
			cell = new Prop(Function.Call<int>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, playerPos.X, playerPos.Y, playerPos.Z, 2.5f, _prisonCellModel.Hash, false, true, true));

			// we're using this to detect if any ped is near this specific point.
			// see the if below.
			Vector3 detectionPosition = cell.GetOffsetInWorldCoords(_offsetOfPedInPrisonCellWhenAttached);

			// the radius is just going to serve as a margin, so instead of putting 0f, we'll put 0.5f.
			float detectionMargin = 0.5f;

			// now that we have the prison cell, let's check to make sure there's no peds at the position of the prison cell.
			if (Function.Call<bool>(Hash.IS_ANY_PED_NEAR_POINT, detectionPosition.X, detectionPosition.Y, detectionPosition.Z, detectionMargin))
			{
				return false;
			}

			// Return true if the entity exists.
			return Entity.Exists(cell);
		}

		private bool IsPedInPlayerGroup(Ped ped)
		{
			return Player.Character.CurrentPedGroup.Contains(ped);
		}

		/// <summary>
		/// This function checks if the <see cref="Ped"/> specified has any weapon that is defined in the <see cref="WeaponHash"/> enum.
		/// </summary>
		/// <param name="ped"></param>
		/// <param name="weaponFound"></param>
		/// <returns></returns>
		private bool HasPedGotAnyWeapon(Ped ped, out WeaponHash weaponFound)
		{
			WeaponHash[] weaponHashes = (WeaponHash[]) Enum.GetValues(typeof(WeaponHash));
			foreach (WeaponHash weaponHash in weaponHashes)
			{
				if (weaponHash == WeaponHash.Unarmed)
					continue;

				if (ped.Weapons.HasWeapon(weaponHash))
				{
					weaponFound = weaponHash;
					return true;
				}
			}
			weaponFound = WeaponHash.Unarmed;
			return false;
		}

		private void LoadMap()
		{
			// this function is going to deserialize the map editor file in the path we specified in the settings.
			// if the file doesn't exist we just return to make sure we're not getting any errors.

			try
			{
				var serializer = new XmlSerializer(typeof(Map));
				StreamReader reader = new StreamReader(_mapName);
				Map map = (Map)serializer.Deserialize(reader);
				reader.Close();
				_inGameMap = MapBuilder.BuildMap(map);
				var blip = _inGameMap.AddBlip();
				blip.Sprite = BlipSprite.CaptureHouse;
				blip.Name = Path.GetFileNameWithoutExtension(_mapName);

				// make sure that the meta data is not null.
				if (map.Metadata == null)
					return;

				// There's no meta data to display.
				if (string.IsNullOrEmpty(map.Metadata.Name) && string.IsNullOrEmpty(map.Metadata.Creator) &&
				    string.IsNullOrEmpty(map.Metadata.Description))
					return;

				HelpText.Display($"{(!string.IsNullOrEmpty(map.Metadata.Name) ? "Name: " + map.Metadata.Name : "")}" +
								 $"\n{(!string.IsNullOrEmpty(map.Metadata.Creator) ? "Creator: " + map.Metadata.Creator : "")}~s~" +
								 $"\n{(!string.IsNullOrEmpty(map.Metadata.Description) ? "Description: " + map.Metadata.Description : "")}", false);
			}
			catch (Exception e)
			{
				UI.Notify("Map Error: ~r~" + e.Message);
				string prevText = string.Empty;
				const string path = "./scripts/ArrestErrors.log";
				if (File.Exists(path))
					prevText = File.ReadAllText(path);
				File.WriteAllText(path, $"{prevText}[{DateTime.UtcNow:hh:mm:ss}] [ERROR] " + e.Message + "\n" + e.StackTrace + "\n");
			}
		}
	}
}
