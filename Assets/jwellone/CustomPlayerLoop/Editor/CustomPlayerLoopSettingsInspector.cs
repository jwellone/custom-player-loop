using UnityEngine;
using UnityEditor;

namespace jwellone.Editor
{
	[CustomEditor(typeof(CustomPlayerLoopSettings))]
	public class CustomPlayerLoopSettingsInspector : UnityEditor.Editor
	{
		private bool[] _foldouts;

		public override void OnInspectorGUI()
		{
			var instance = target as CustomPlayerLoopSettings;
			if (instance is null)
			{
				return;
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Window"))
			{
				CustomPlayerLoopWindow.Open();
			}

			if (GUILayout.Button("Apply"))
			{
				EditorUtility.SetDirty(instance);
				AssetDatabase.SaveAssets();
			}
			GUILayout.EndHorizontal();

			var dataList = instance.DataList;
			if (_foldouts == null || _foldouts.Length != dataList.Count)
			{
				_foldouts = new bool[dataList.Count];
			}

			for (var i = 0; i < dataList.Count; ++i)
			{
				var data = dataList[i];

				EditorGUILayout.BeginHorizontal();
				data.parameter.enabled =
					EditorGUILayout.Toggle(string.Empty, data.parameter.enabled, GUILayout.Width(26));
				GUI.color = data.parameter.enabled ? Color.white : Color.gray;
				_foldouts[i] =
					EditorGUILayout.Foldout(_foldouts[i],
						$"{data.parameter.type?.Name}({data.subSystemParameters?.Length})", true);
				GUI.color = Color.white;
				EditorGUILayout.EndHorizontal();

				if (!_foldouts[i] || data.subSystemParameters == null)
				{
					continue;
				}

				using (new EditorGUI.IndentLevelScope(1))
				{
					foreach (var sub in data.subSystemParameters)
					{
						EditorGUILayout.BeginHorizontal();
						GUI.color = data.parameter.enabled && sub.enabled ? Color.white : Color.gray;
						sub.enabled = EditorGUILayout.Toggle(string.Empty, sub.enabled, GUILayout.Width(16));
						EditorGUILayout.LabelField(sub.type?.Name);
						GUI.color = Color.white;
						EditorGUILayout.EndHorizontal();
					}
				}
			}
		}
	}
}
