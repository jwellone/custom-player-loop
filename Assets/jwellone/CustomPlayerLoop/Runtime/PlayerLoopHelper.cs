using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.LowLevel;

namespace jwellone
{
	public static class PlayerLoopHelper
	{
		public static readonly string assetName = "CustomPlayerLoopSettings";
		public static readonly string folder = "CustomPlayerLoop";
		public static readonly string fullPath = Path.Combine(folder, assetName);

		private static CustomPlayerLoopSettings _settings = null;
		private static Action _onSettingProcess { get; set; } = OnSetting;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void OnAfterSceneLoad()
		{
			_onSettingProcess?.Invoke();
			_onSettingProcess = null;
			_settings = null;
		}

		public static void SetPlayerLoop(in PlayerLoopSystem playerLoop)
		{
			PlayerLoop.SetPlayerLoop(playerLoop);
		}

		public static void SetSettingProcess(Action process)
		{
			_onSettingProcess = process;
		}

		public static PlayerLoopSystem GetDefaultPlayerLoop()
		{
			return PlayerLoop.GetDefaultPlayerLoop();
		}

		public static PlayerLoopSystem GetCurrentPlayerLoop()
		{
			return PlayerLoop.GetCurrentPlayerLoop();
		}

		public static PlayerLoopSystem GetCustomPlayerLoop()
		{
			if (_settings == null)
			{
				_settings = Resources.Load<CustomPlayerLoopSettings>(fullPath);
				if (_settings == null)
				{
					Debug.LogWarning($"[PlayerLoop]{fullPath}.asset load failed.");
					return PlayerLoop.GetCurrentPlayerLoop();
				}
			}

			var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
			var enabledSubSystemList = new List<PlayerLoopSystem>(64);
			var data = _settings.DataList;
			for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
			{
				var system = playerLoop.subSystemList[i];
				enabledSubSystemList.Clear();

				foreach (var d in data)
				{
					if (d.parameter.type != system.type)
					{
						continue;
					}

					if (d.parameter.enabled)
					{
						foreach (var sub in system.subSystemList)
						{
							foreach (var dSub in d.subSystemParameters)
							{
								if (dSub.type == sub.type && dSub.enabled)
								{
									enabledSubSystemList.Add(sub);
									break;
								}
							}
						}
					}

					playerLoop.subSystemList[i].subSystemList = enabledSubSystemList.ToArray();
					break;
				}
			}

			return playerLoop;
		}

		private static void OnSetting()
		{
			SetPlayerLoop(GetCustomPlayerLoop());
		}
	}
}
