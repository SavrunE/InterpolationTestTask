using System;
using UnityEngine;


namespace VTest
{
	public class DynObject : MonoBehaviour
	{
		public Action<ObjectSnapshotData> onData;


		private void Start()
		{
			onData += (ObjectSnapshotData d) =>
			{
				// TODO: Remove me!
				transform.position = d.position;
			};
		}
	}
}
