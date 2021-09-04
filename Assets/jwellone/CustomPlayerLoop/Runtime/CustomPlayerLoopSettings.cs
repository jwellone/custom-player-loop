using System;
using System.Collections.Generic;
using UnityEngine;

namespace jwellone
{
	public class CustomPlayerLoopSettings : ScriptableObject
	{
		[Serializable]
		public class Parameter
		{
			[SerializeField] private bool _enabled = true;
			[SerializeField] private string _assemblyQualifiedName;

			private Type _type = null;

			public bool enabled
			{
				get => _enabled;
				set => _enabled = value;
			}

			public Type type
			{
				get => _type ?? (_type = Type.GetType(_assemblyQualifiedName));
				set
				{
					_type = value;
					_assemblyQualifiedName = _type?.AssemblyQualifiedName;
				}
			}
		}

		[Serializable]
		public class Data
		{
			public Parameter parameter;
			public Parameter[] subSystemParameters;
		}

		[SerializeField] private List<Data> _data;

		public IReadOnlyList<Data> DataList => _data;
	}
}
