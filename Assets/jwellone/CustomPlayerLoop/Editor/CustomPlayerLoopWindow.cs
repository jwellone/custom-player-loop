using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.LowLevel;
using UnityEngine.Profiling;

namespace jwellone.Editor
{
	public class CustomPlayerLoopWindow : EditorWindow
	{
		[InitializeOnLoad]
		private static class Settings
		{
			public static bool isValid => !(asset is null);
			public static CustomPlayerLoopSettings asset = null;

			static Settings()
			{
				EditorApplication.delayCall -= Setup;
				EditorApplication.delayCall += Setup;
			}


			private static void Setup()
			{
				var assetPath = AssetDatabase.FindAssets($"t:scriptableobject {PlayerLoopHelper.assetName}")
					.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();

				if (!string.IsNullOrEmpty(assetPath))
				{
					asset = AssetDatabase.LoadAssetAtPath<CustomPlayerLoopSettings>(assetPath);
					return;
				}

				var dataList = new List<CustomPlayerLoopSettings.Data>();
				var defaultPlayerLoop = PlayerLoop.GetDefaultPlayerLoop();
				foreach (var system in defaultPlayerLoop.subSystemList)
				{
					var data = new CustomPlayerLoopSettings.Data();
					data.parameter = new CustomPlayerLoopSettings.Parameter();
					data.parameter.type = system.type;

					var subParameters = new CustomPlayerLoopSettings.Parameter[system.subSystemList.Length];
					for (var i = 0; i < subParameters.Length; ++i)
					{
						subParameters[i] = new CustomPlayerLoopSettings.Parameter();
						subParameters[i].type = system.subSystemList[i].type;
					}

					data.subSystemParameters = subParameters;
					dataList.Add(data);
				}

				var instance = CreateInstance<CustomPlayerLoopSettings>();
				var type = instance.GetType();
				var field = type.GetField("_data", (BindingFlags.InvokeMethod
													| BindingFlags.NonPublic
													| BindingFlags.Instance));
				field?.SetValue(instance, dataList);

				var path = "Assets";
				var subFolders = new string[]
				{
					"CustomPlayerLoop",
					"Resources",
					PlayerLoopHelper.folder
				};

				for (var i = 0; i < subFolders.Length; ++i)
				{
					var target = subFolders[i];
					if (!AssetDatabase.IsValidFolder(Path.Combine(path, target)))
					{
						AssetDatabase.CreateFolder(path, target);
					}

					path = Path.Combine(path, target);
				}

				path = Path.Combine(path, PlayerLoopHelper.assetName + ".asset");
				AssetDatabase.CreateAsset(instance, path);
				AssetDatabase.Refresh();

				asset = AssetDatabase.LoadAssetAtPath<CustomPlayerLoopSettings>(path);

				Debug.Log($"[PlayerLoop]Create asset {path}");
			}

			public static void Reset()
			{
				if (asset is null)
				{
					return;
				}

				var list = asset.DataList as List<CustomPlayerLoopSettings.Data>;
				foreach (var parent in list)
				{
					parent.parameter.enabled = true;
					foreach (var sub in parent.subSystemParameters)
					{
						sub.enabled = true;
					}
				}
			}
		}

		const int LEFT_CONTENTS_WIDTH = 300;

		private bool _isInit = false;
		private bool[] _foldouts = null;
		private Vector2 _customScrollPosition = Vector2.zero;
		private Vector2 _currentScrollPosition = Vector2.zero;
		private PlayerLoopSystem _currentPlayerLoop;
		private Texture2D _texPlayingOn;
		private Texture2D _texPlaying;
		private Texture2D _texPause;
		private Texture2D _texPauseOn;
		private Texture2D _texStep;
		private readonly Dictionary<Type, string> _dicSmplerTag = new Dictionary<Type, string>();
		private readonly Dictionary<Type, string> _dicPrefsFoldouts = new Dictionary<Type, string>();

		[MenuItem("jwellone/Window/Custom Player Loop")]
		public static void Open()
		{
			var window = (CustomPlayerLoopWindow)EditorWindow.GetWindow(typeof(CustomPlayerLoopWindow));
			window.titleContent = new GUIContent("CustomPlayerLoop");
			window.minSize = new Vector2(400, 500);
			window.Show();
		}

		private void OnEnable()
		{
			_isInit = false;
			autoRepaintOnSceneChange = true;
		}

		private void OnDisable()
		{
		}

		private void OnInit()
		{
			if (_isInit)
			{
				return;
			}

			_currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

			if (!Application.isPlaying)
			{
				if (!(Settings.asset is null))
				{
					_foldouts = new bool[Settings.asset.DataList.Count];
				}
			}

			_dicSmplerTag.Clear();
			foreach (var system in _currentPlayerLoop.subSystemList)
			{
				_dicPrefsFoldouts.Add(system.type, $"CustomPlayerLoop_{system.type}_Foldout");

				if (system.subSystemList == null)
				{
					continue;
				}

				foreach (var sub in system.subSystemList)
				{
					_dicSmplerTag.Add(sub.type, $"{system.type.Name}.{sub.type.Name}");
				}
			}

			_texPlayingOn = EditorGUIUtility.FindTexture("d_PlayButton On@2x");
			_texPlaying = EditorGUIUtility.FindTexture("d_PlayButton@2x");
			_texPause = EditorGUIUtility.FindTexture("d_PauseButton@2x");
			_texPauseOn = EditorGUIUtility.FindTexture("d_PauseButton On@2x");
			_texStep = EditorGUIUtility.FindTexture("d_StepButton@2x");

			_isInit = true;
		}

		void OnGUI()
		{
			OnInit();

			DrawHeader();
			DrawContents();
		}

		void DrawHeader()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			var isPlaying = GUILayout.Toggle(EditorApplication.isPlaying,
				EditorApplication.isPlaying ? _texPlayingOn : _texPlaying, EditorStyles.miniButtonLeft, GUILayout.Width(48));

			var isPaused = GUILayout.Toggle(EditorApplication.isPaused,
				EditorApplication.isPaused ? _texPauseOn : _texPause,
				EditorStyles.miniButtonMid, GUILayout.Width(48));

			var isStep = GUILayout.Button(_texStep, EditorStyles.miniButtonRight, GUILayout.Width(48));
			EditorGUILayout.EndHorizontal();

			if (EditorApplication.isPlaying != isPlaying)
			{
				EditorApplication.isPlaying = isPlaying;
			}

			if (EditorApplication.isPaused != isPaused)
			{
				EditorApplication.isPaused = isPaused;
			}

			if (isStep)
			{
				EditorApplication.Step();
			}

			GUILayout.BeginHorizontal(EditorStyles.toolbar);

			using (new GUILayout.HorizontalScope(GUILayout.Width(LEFT_CONTENTS_WIDTH + 6)))
			{
				EditorGUILayout.LabelField("Custom", GUILayout.Width(64));

				GUILayout.FlexibleSpace();

				var width = GUILayout.Width(48);
				if (GUILayout.Button("Reset", EditorStyles.toolbarButton, width))
				{
					Settings.Reset();
				}

				if (GUILayout.Button("Select", EditorStyles.toolbarButton, width))
				{
					Selection.activeObject = Settings.asset;
				}

				if (GUILayout.Button("Apply", EditorStyles.toolbarButton, width))
				{
					EditorUtility.SetDirty(Settings.asset);
					AssetDatabase.SaveAssets();
				}

				GUILayout.Space(1);
			}

			using (new GUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Current");
				EditorGUILayout.LabelField("[ms]", GUILayout.Width(64));
				EditorGUILayout.LabelField("  [BlockCount]", GUILayout.Width(96));
			}


			GUILayout.EndHorizontal();
		}

		void DrawContents()
		{
			GUILayout.BeginHorizontal();
			var width = GUILayout.Width(LEFT_CONTENTS_WIDTH);

			_customScrollPosition = EditorGUILayout.BeginScrollView(_customScrollPosition, GUI.skin.box, width);
			DrawCustomPlayerLoop();
			EditorGUILayout.EndScrollView();

			_currentScrollPosition = EditorGUILayout.BeginScrollView(_currentScrollPosition, GUI.skin.box);
			DrawCurrentPlayerLoop();
			EditorGUILayout.EndScrollView();

			GUILayout.EndHorizontal();
		}

		void DrawCustomPlayerLoop()
		{
			if (!Settings.isValid)
			{
				return;
			}

			var asset = Settings.asset;
			for (var i = 0; i < asset.DataList.Count; ++i)
			{
				var system = asset.DataList[i];
				EditorGUILayout.BeginHorizontal();
				system.parameter.enabled =
					EditorGUILayout.Toggle(string.Empty, system.parameter.enabled, GUILayout.Width(16));
				GUI.color = system.parameter.enabled ? Color.white : Color.gray;
				_foldouts[i] = EditorGUILayout.Foldout(_foldouts[i],
					$"{system.parameter.type.Name}({system.subSystemParameters.Length})", true);
				GUI.color = Color.white;
				EditorGUILayout.EndHorizontal();

				if (!_foldouts[i])
				{
					continue;
				}

				using (new EditorGUI.IndentLevelScope(1))
				{
					foreach (var sub in system.subSystemParameters)
					{
						GUI.color = sub.enabled && system.parameter.enabled ? Color.white : Color.gray;
						sub.enabled = EditorGUILayout.ToggleLeft(sub.type.Name, sub.enabled);
						GUI.color = Color.white;
					}
				}
			}
		}

		void DrawCurrentPlayerLoop()
		{
			var col1 = new Color(0.0f, 1.0f, 1.0f, 0.2f);
			var col2 = new Color(1f, 1f, 1f, 0.2f);
			var count = 0;
			var width = position.width - LEFT_CONTENTS_WIDTH;
			foreach (var system in _currentPlayerLoop.subSystemList)
			{
				if (system.subSystemList == null || system.subSystemList.Length <= 0)
				{
					continue;
				}

				var prefsFoldout = EditorPrefs.GetBool(_dicPrefsFoldouts[system.type], false);
				var text = $"{system.type?.Name}({system.subSystemList.Length.ToString()})";
				var foldout = EditorGUILayout.Foldout(prefsFoldout, text, true);

				if (prefsFoldout != foldout)
				{
					EditorPrefs.SetBool(_dicPrefsFoldouts[system.type], foldout);
				}

				if (!foldout)
				{
					continue;
				}

				using (new EditorGUI.IndentLevelScope(1))
				{
					foreach (var sub in system.subSystemList)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField(sub.type.Name);

						var rect = GUILayoutUtility.GetLastRect();
						rect.x += 12;
						rect.y += 16;
						rect.height -= 16;
						rect.width = width;
						EditorGUI.DrawRect(rect, (count % 2 == 0) ? col1 : col2);

						var sampler = Sampler.Get(_dicSmplerTag[sub.type]);
						var ms = sampler.GetRecorder().elapsedNanoseconds * 0.000001f;
						EditorGUILayout.LabelField(ms.ToString("F6"), GUILayout.Width(128));
						EditorGUILayout.LabelField(sampler.GetRecorder().sampleBlockCount.ToString(), GUILayout.Width(64));

						EditorGUILayout.EndHorizontal();
						++count;
					}
				}
			}
		}
	}
}
