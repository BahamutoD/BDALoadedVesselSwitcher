using System;
using BahaTurret;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BDALoadedVesselSwitcher
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class LoadedVesselSwitcher : MonoBehaviour
	{
		public LoadedVesselSwitcher Instance;

		List<MissileFire> wmgrsA;
		List<MissileFire> wmgrsB;

		//gui params
		float windowHeight = 0;//auto adjusting
		float windowWidth = 250;
		float buttonHeight = 20;
		float buttonGap = 1;
		float margin = 5;
		float titleHeight = 30;
		Rect windowRect;

		bool ready = false;
		bool showGUI = false;

		int guiCheckIndex = 0;

		void Awake()
		{
			if(Instance)
			{
				Destroy(this);
			}
			else
			{
				Instance = this;
			}
		}

		void Start()
		{
			UpdateList();	
			GameEvents.onVesselCreate.Add(VesselEventUpdate);
			GameEvents.onVesselDestroy.Add(VesselEventUpdate);
			GameEvents.onVesselGoOffRails.Add(VesselEventUpdate);
			GameEvents.onVesselGoOnRails.Add(VesselEventUpdate);
			MissileFire.OnToggleTeam += MissileFireOnToggleTeam;

			//showGui = true;

			windowRect = new Rect(10, Screen.height / 6f, windowWidth, 10);

			ready = false;
			StartCoroutine(WaitForBDASettings());
		}

		IEnumerator WaitForBDASettings()
		{
			while(BDArmorySettings.Instance == null)
			{
				yield return null;
			}

			ready = true;
			BDArmorySettings.Instance.hasVS = true;
			guiCheckIndex = Misc.RegisterGUIRect(new Rect());
		}


		void MissileFireOnToggleTeam (MissileFire wm, BDArmorySettings.BDATeams team)
		{
			if(showGUI)
			{
				UpdateList();
			}
		}

		void VesselEventUpdate(Vessel v)
		{
			if(showGUI)
			{
				UpdateList();
			}
		}

		void Update()
		{
			if(ready)
			{
				if(BDArmorySettings.Instance.showVSGUI != showGUI)
				{
					showGUI = BDArmorySettings.Instance.showVSGUI;
					if(showGUI)
					{
						UpdateList();
					}
				}

				if(showGUI)
				{
					Hotkeys();
				}
			}
		}

		void Hotkeys()
		{
			if(Input.GetKeyDown(KeyCode.PageUp))
			{
				SwitchToNextVessel();
			}
			if(Input.GetKeyDown(KeyCode.PageDown))
			{
				SwitchToPreviousVessel();
			}
		}


		void UpdateList()
		{
			if(wmgrsA == null)
			{
				wmgrsA = new List<MissileFire>();
			}
			wmgrsA.Clear();

			if(wmgrsB == null)
			{
				wmgrsB = new List<MissileFire>();
			}
			wmgrsB.Clear();

			foreach(var v in FlightGlobals.Vessels)
			{
				if(!v) continue;

				if(!v.loaded || v.packed)
				{
					continue;
				}

				foreach(var wm in v.FindPartModulesImplementing<MissileFire>())
				{
					if(!wm.team)
					{
						wmgrsA.Add(wm);
					}
					else
					{
						wmgrsB.Add(wm);
					}
					break;
				}
			}
		}

		void OnGUI()
		{
			if(ready)
			{
				if(showGUI && BDArmorySettings.GAME_UI_ENABLED)
				{
					windowRect.height = windowHeight;
					windowRect = GUI.Window(10293444, windowRect, ListWindow, "BDA Vessel Switcher", HighLogic.Skin.window);
					Misc.UpdateGUIRect(windowRect, guiCheckIndex);
				}
				else
				{
					Misc.UpdateGUIRect(new Rect(), guiCheckIndex);
				}

				if(teamSwitchDirty)
				{
					if(wmToSwitchTeam)
					{
						wmToSwitchTeam.ToggleTeam();
					}
					teamSwitchDirty = false;
					wmToSwitchTeam = null;
				}
			}
		}

		MissileFire wmToSwitchTeam;
		bool teamSwitchDirty = false;

		void ListWindow(int id)
		{
			GUI.DragWindow(new Rect(0,0,windowWidth-buttonHeight-4, titleHeight));
			if(GUI.Button(new Rect(windowWidth - buttonHeight-4, 4, buttonHeight, buttonHeight), "X", HighLogic.Skin.button))
			{
				BDArmorySettings.Instance.showVSGUI = false;
				return;
			}
			float height = 0;
			float vesselLineA = 0;
			float vesselLineB = 0;
			height += margin + titleHeight;
			GUI.Label(new Rect(margin, height, windowWidth - (2 * margin), buttonHeight), "Team A:", HighLogic.Skin.label);
			height += buttonHeight;
			float vesselButtonWidth = windowWidth - (2 * margin);
			vesselButtonWidth -= (3 * buttonHeight);
			foreach(var wm in wmgrsA)
			{
				float lineY = height + (vesselLineA * (buttonHeight + buttonGap));
				Rect buttonRect = new Rect(margin, lineY, vesselButtonWidth, buttonHeight);
				GUIStyle vButtonStyle = wm.vessel.isActiveVessel ? HighLogic.Skin.box : HighLogic.Skin.button;
				if(GUI.Button(buttonRect, wm.vessel.GetName(), vButtonStyle))
				{
					FlightGlobals.ForceSetActiveVessel(wm.vessel);
				}

				//guard toggle
				GUIStyle guardStyle = wm.guardMode ? HighLogic.Skin.box : HighLogic.Skin.button;
				Rect guardButtonRect = new Rect(margin+vesselButtonWidth, lineY, buttonHeight, buttonHeight);
				if(GUI.Button(guardButtonRect, "G", guardStyle))
				{
					wm.ToggleGuardMode();
				}

				//AI toggle
				if(wm.pilotAI)
				{
					GUIStyle aiStyle = wm.pilotAI.pilotEnabled ? HighLogic.Skin.box : HighLogic.Skin.button;
					Rect aiButtonRect = new Rect(margin + vesselButtonWidth + buttonHeight, lineY, buttonHeight, buttonHeight);
					if(GUI.Button(aiButtonRect, "P", aiStyle))
					{
						wm.pilotAI.TogglePilot();
					}
				}

				//team toggle
				Rect teamButtonRect = new Rect(margin + vesselButtonWidth + buttonHeight + buttonHeight, lineY, buttonHeight, buttonHeight);
				if(GUI.Button(teamButtonRect, "T", HighLogic.Skin.button))
				{
					wmToSwitchTeam = wm;
					teamSwitchDirty = true;
				}
				vesselLineA++;
			}
			height += (vesselLineA * (buttonHeight + buttonGap));
			height += margin;
			GUI.Label(new Rect(margin, height, windowWidth - (2 * margin), buttonHeight), "Team B:", HighLogic.Skin.label);
			height += buttonHeight;
			foreach(var wm in wmgrsB)
			{
				float lineY = height + (vesselLineB * (buttonHeight + buttonGap));

				Rect buttonRect = new Rect(margin, lineY, vesselButtonWidth, buttonHeight);
				GUIStyle vButtonStyle = wm.vessel.isActiveVessel ? HighLogic.Skin.box : HighLogic.Skin.button;
				if(GUI.Button(buttonRect, wm.vessel.GetName(), vButtonStyle))
				{
					FlightGlobals.ForceSetActiveVessel(wm.vessel);
				}


				//guard toggle
				GUIStyle guardStyle = wm.guardMode ? HighLogic.Skin.box : HighLogic.Skin.button;
				Rect guardButtonRect = new Rect(margin+vesselButtonWidth, lineY, buttonHeight, buttonHeight);
				if(GUI.Button(guardButtonRect, "G", guardStyle))
				{
					wm.ToggleGuardMode();
				}

				//AI toggle
				if(wm.pilotAI)
				{
					GUIStyle aiStyle = wm.pilotAI.pilotEnabled ? HighLogic.Skin.box : HighLogic.Skin.button;
					Rect aiButtonRect = new Rect(margin + vesselButtonWidth + buttonHeight, lineY, buttonHeight, buttonHeight);
					if(GUI.Button(aiButtonRect, "P", aiStyle))
					{
						wm.pilotAI.TogglePilot();
					}
				}

				//team toggle
				Rect teamButtonRect = new Rect(margin + vesselButtonWidth + buttonHeight + buttonHeight, lineY, buttonHeight, buttonHeight);
				if(GUI.Button(teamButtonRect, "T", HighLogic.Skin.button))
				{
					wmToSwitchTeam = wm;
					teamSwitchDirty = true;
				}
				vesselLineB++;
			}
			height += (vesselLineB * (buttonHeight + buttonGap));
			height += margin;

			windowHeight = height;
		}

		void SwitchToNextVessel()
		{
			bool switchNext = false;
			foreach(var wm in wmgrsA)
			{
				if(switchNext)
				{
					FlightGlobals.ForceSetActiveVessel(wm.vessel);
					return;
				}
				else if(wm.vessel.isActiveVessel)
				{
					switchNext = true;
				}
			}

			foreach(var wm in wmgrsB)
			{
				if(switchNext)
				{
					FlightGlobals.ForceSetActiveVessel(wm.vessel);
					return;
				}
				else if(wm.vessel.isActiveVessel)
				{
					switchNext = true;
				}
			}


			if(wmgrsA.Count > 0 && wmgrsA[0] && !wmgrsA[0].vessel.isActiveVessel)
			{
				FlightGlobals.ForceSetActiveVessel(wmgrsA[0].vessel);
			}
			else if(wmgrsB.Count > 0 && wmgrsB[0] && !wmgrsB[0].vessel.isActiveVessel)
			{
				FlightGlobals.ForceSetActiveVessel(wmgrsB[0].vessel);
			}
		}

		void SwitchToPreviousVessel()
		{
			if(wmgrsB.Count > 0)
			{
				for(int i = wmgrsB.Count - 1; i >= 0; i--)
				{
					if(wmgrsB[i].vessel.isActiveVessel)
					{
						if(i > 0)
						{
							FlightGlobals.ForceSetActiveVessel(wmgrsB[i - 1].vessel);
							return;
						}
						else if(wmgrsA.Count > 0)
						{
							FlightGlobals.ForceSetActiveVessel(wmgrsA[wmgrsA.Count-1].vessel);
							return;
						}
						else if(wmgrsB.Count > 0)
						{
							FlightGlobals.ForceSetActiveVessel(wmgrsB[wmgrsB.Count-1].vessel);
							return;
						}
					}
				}
			}

			if(wmgrsA.Count > 0)
			{
				for(int i = wmgrsA.Count - 1; i >= 0; i--)
				{
					if(wmgrsA[i].vessel.isActiveVessel)
					{
						if(i > 0)
						{
							FlightGlobals.ForceSetActiveVessel(wmgrsA[i - 1].vessel);
							return;
						}
						else if(wmgrsB.Count > 0)
						{
							FlightGlobals.ForceSetActiveVessel(wmgrsB[wmgrsB.Count-1].vessel);
							return;
						}
						else if(wmgrsA.Count > 0)
						{
							FlightGlobals.ForceSetActiveVessel(wmgrsA[wmgrsA.Count-1].vessel);
							return;
						}
					}
				}
			}
		}
	}
}

