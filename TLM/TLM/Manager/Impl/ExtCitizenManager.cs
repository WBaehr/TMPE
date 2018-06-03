﻿using ColossalFramework;
using CSUtil.Commons;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TrafficManager.Custom.AI;
using TrafficManager.State;
using TrafficManager.Traffic;
using TrafficManager.Traffic.Data;
using TrafficManager.Traffic.Enums;
using TrafficManager.Util;
using UnityEngine;
using static TrafficManager.Traffic.Data.ExtCitizen;
using static TrafficManager.Traffic.Data.ExtCitizenInstance;

namespace TrafficManager.Manager.Impl {
	public class ExtCitizenManager : AbstractCustomManager, ICustomDataManager<List<Configuration.ExtCitizenData>>, IExtCitizenManager {
		public static ExtCitizenManager Instance = new ExtCitizenManager();

		/// <summary>
		/// All additional data for citizens. Index: citizen id
		/// </summary>
		public ExtCitizen[] ExtCitizens = null;

		private ExtCitizenManager() {
			ExtCitizens = new ExtCitizen[CitizenManager.MAX_CITIZEN_COUNT];
			for (uint i = 0; i < CitizenManager.MAX_CITIZEN_COUNT; ++i) {
				ExtCitizens[i] = new ExtCitizen(i);
			}
		}

		protected override void InternalPrintDebugInfo() {
			base.InternalPrintDebugInfo();
			Log._Debug($"Extended citizen data:");
			for (int i = 0; i < ExtCitizens.Length; ++i) {
				if (!ExtCitizens[i].IsValid()) {
					continue;
				}
				Log._Debug($"Citizen {i}: {ExtCitizens[i]}");
			}
		}

		public void OnReleaseCitizen(uint citizenId) {
			ExtCitizens[citizenId].Reset();
		}

		public void OnArriveAtDestination(uint citizenId, ref Citizen citizenData, ref CitizenInstance instanceData) {
#if DEBUG
			bool citDebug = GlobalConfig.Instance.Debug.CitizenId == 0 || GlobalConfig.Instance.Debug.CitizenId == citizenId;
			bool debug = GlobalConfig.Instance.Debug.Switches[2] && citDebug;
			bool fineDebug = GlobalConfig.Instance.Debug.Switches[4] && citDebug;

			if (fineDebug)
				Log._Debug($"ExtCitizenManager.OnArriveAtDestination({citizenId}) called. switching transportMode={ExtCitizens[citizenId].transportMode} to lastTransportMode={ExtCitizens[citizenId].lastTransportMode}");
#endif
			if (ExtCitizens[citizenId].lastLocation == Citizen.Location.Home) {
				ExtCitizens[citizenId].lastTransportMode = ExtCitizens[citizenId].transportMode;
			}
			ExtCitizens[citizenId].transportMode = ExtTransportMode.None;

			ushort targetBuildingId = instanceData.m_targetBuilding;
			Services.CitizenService.ProcessCitizen(citizenId, delegate (uint citId, ref Citizen cit) {
				cit.SetLocationByBuilding(citId, targetBuildingId);
				return true;
			});

			if (citizenData.CurrentLocation != Citizen.Location.Moving) {
				ExtCitizens[citizenId].lastLocation = citizenData.CurrentLocation;
			}
		}

		public void ResetCitizen(uint citizenId) {
			ExtCitizens[citizenId].Reset();
		}

		// stock code
		public Citizen.AgeGroup GetAgeGroup(Citizen.AgePhase agePhase) {
			switch (agePhase) {
				case Citizen.AgePhase.Child:
					return Citizen.AgeGroup.Child;
				case Citizen.AgePhase.Teen0:
				case Citizen.AgePhase.Teen1:
					return Citizen.AgeGroup.Teen;
				case Citizen.AgePhase.Young0:
				case Citizen.AgePhase.Young1:
				case Citizen.AgePhase.Young2:
					return Citizen.AgeGroup.Young;
				case Citizen.AgePhase.Adult0:
				case Citizen.AgePhase.Adult1:
				case Citizen.AgePhase.Adult2:
				case Citizen.AgePhase.Adult3:
					return Citizen.AgeGroup.Adult;
				case Citizen.AgePhase.Senior0:
				case Citizen.AgePhase.Senior1:
				case Citizen.AgePhase.Senior2:
				case Citizen.AgePhase.Senior3:
					return Citizen.AgeGroup.Senior;
				default:
					return Citizen.AgeGroup.Adult;
			}
		}

		public override void OnLevelUnloading() {
			base.OnLevelUnloading();
			Reset();
		}

		internal void Reset() {
			for (int i = 0; i < ExtCitizens.Length; ++i) {
				ExtCitizens[i].Reset();
			}
		}

		public bool LoadData(List<Configuration.ExtCitizenData> data) {
			bool success = true;
			Log.Info($"Loading {data.Count} extended citizens");

			foreach (Configuration.ExtCitizenData item in data) {
				try {
					uint citizenId = item.citizenId;
					ExtCitizens[citizenId].transportMode = (ExtTransportMode)item.lastTransportMode;
				} catch (Exception e) {
					// ignore, as it's probably corrupt save data. it'll be culled on next save
					Log.Warning("Error loading ext. citizen: " + e.ToString());
					success = false;
				}
			}

			return success;
		}

		public List<Configuration.ExtCitizenData> SaveData(ref bool success) {
			List<Configuration.ExtCitizenData> ret = new List<Configuration.ExtCitizenData>();
			for (uint citizenId = 0; citizenId < CitizenManager.MAX_CITIZEN_COUNT; ++citizenId) {
				try {
					if ((Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId].m_flags & Citizen.Flags.Created) == Citizen.Flags.None) {
						continue;
					}

					if (ExtCitizens[citizenId].transportMode == ExtTransportMode.None) {
						continue;
					}

					Configuration.ExtCitizenData item = new Configuration.ExtCitizenData(citizenId);
					item.lastTransportMode = (int)ExtCitizens[citizenId].transportMode;
					ret.Add(item);
				} catch (Exception ex) {
					Log.Error($"Exception occurred while saving ext. citizen @ {citizenId}: {ex.ToString()}");
					success = false;
				}
			}
			return ret;
		}
	}
}