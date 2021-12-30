using System;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using Random = UnityEngine.Random;


namespace VTest
{
	public struct ObjectSnapshotData
	{
		/// <summary>
		/// Position of dynamic object
		/// </summary>
		public readonly Vector3 position;
		/// <summary>
		/// Velocity
		/// </summary>
		public readonly Vector3 velocity;
		/// <summary>
		/// Server time at this frame
		/// </summary>
		public readonly float time;
		/// <summary>
		/// 'true' if object teleported at this frame
		/// </summary>
		public readonly bool teleport;

		public ObjectSnapshotData(Vector3 p, Vector3 v, float t, bool tele)
		{
			position = p;
			velocity = v;
			time = t;
			teleport = tele;
		}
	}

	public class TestSceneMovement : MonoBehaviour
	{
		private class EmuData
		{
			public Vector3 position;
			public Vector3 velocity;

			public float dist;
			public Vector3 target;

			public Action<EmuData> emulate;
		}

		[SerializeField]
		private float _tickRate = 0.05f;

		[SerializeField]
		private DynObject[] _dyns;

		private EmuData[] _emuData;

		private Queue<KeyValuePair<DynObject, ObjectSnapshotData>> _moveDataQueue =
			new Queue<KeyValuePair<DynObject, ObjectSnapshotData>>();

		private float _nextDispatch;

		private float _svTime;
		private float _svRefTime;

		private Stopwatch _stopWatch = new Stopwatch();


		private void Start()
		{
			_emuData = new EmuData[_dyns.Length];

			for (int i = 0; i < _emuData.Length; i++)
			{
				Vector2 p = Random.insideUnitCircle * Random.Range(0.5f, 5.0f);
				Vector2 t = Random.insideUnitCircle * Random.Range(0.5f, 3.0f);
				Action<EmuData> act;

				switch ((int)Mathf.Repeat(i, 4))
				{
					case 0: act = EmuCircle; break;
					case 1: act = EmuRandom; break;
					case 2: act = EmuInput; break;
					case 3: act = EmuPath; break;
					default: act = EmuCircle; break;
				}

				_emuData[i] = new EmuData()
				{
					position = new Vector3(p.x, 1.0f, p.y),
					target = new Vector3(t.x, 1.0f, t.y),
					dist = Random.Range(15.0f, 22.0f),
					emulate = act
				};
			}

			ResetSvTime(Random.Range(1.0f, 1000.0f));
			_nextDispatch = Time.time + _tickRate + 0.01f;
		}

		private void Update()
		{
			_stopWatch.Reset();
			_stopWatch.Start();

			_svRefTime += Time.deltaTime;

			while (_svTime < _svRefTime)
			{
				_svTime += _tickRate;

				SvTick(_tickRate);

				if (_stopWatch.ElapsedMilliseconds > (int)(_tickRate * 2000.0f))
				{
					break;
				}
			}

			if (_svTime > 9999.0f)
			{
				ResetSvTime(0.0f);
			}

			if (Time.time > _nextDispatch)
			{
				while (_moveDataQueue.Count > 0)
				{
					var kv = _moveDataQueue.Dequeue();
					kv.Key.onData(kv.Value);
				}

				_nextDispatch = Time.time + Random.Range(_tickRate, 0.2f);
			}
		}

		private void SvTick(float delta)
		{
			for (int i = 0; i < _dyns.Length; i++)
			{
				var dyn = _dyns[i];
				var emu = _emuData[i];
				bool disableInterp = false;
				emu.emulate(emu);

				emu.position = emu.position + emu.velocity * delta;

				if (emu.position.magnitude > emu.dist)
				{
					emu.position = emu.target;
					disableInterp = true;
				}

				var r = new ObjectSnapshotData(emu.position, emu.velocity, _svTime, disableInterp);
				_moveDataQueue.Enqueue(new KeyValuePair<DynObject, ObjectSnapshotData>(dyn, r));
			}
		}

		private void ResetSvTime(float time)
		{
			_svTime = time;
			_svRefTime = time;
		}

		private void EmuCircle(EmuData d)
		{
			float x = Mathf.Cos(_svTime * 0.50f);
			float y = Mathf.Sin(_svTime * 0.50f);

			d.velocity = new Vector3(x, 0.0f, y) * 2.0f;
		}

		private void EmuInput(EmuData d)
		{
			float x = 0.0f, y = 0.0f;
			Vector2 dir;

			if (Input.GetKey(KeyCode.D)) x += 1.0f;
			if (Input.GetKey(KeyCode.A)) x -= 1.0f;
			if (Input.GetKey(KeyCode.W)) y += 1.0f;
			if (Input.GetKey(KeyCode.S)) y -= 1.0f;

			dir = new Vector2(x, y).normalized;

			d.velocity = Vector3.Lerp(d.velocity, new Vector3(dir.x, 0.0f, dir.y) * 6.0f, 3.0f * _tickRate);
		}

		private float _nextChange = 0.0f;
		private Vector2 _randomDir;
		private void EmuRandom(EmuData d)
		{
			float scale = Random.Range(3.0f, 7.0f);
			d.velocity = Vector3.Lerp(d.velocity, new Vector3(_randomDir.x, 0.0f, _randomDir.y) * scale, 2.0f * _tickRate);

			if (Time.time > _nextChange)
			{
				_randomDir = Random.insideUnitCircle.normalized;
				_nextChange = Time.time + 0.85f;
			}
		}

		[SerializeField]
		private Transform[] _path;
		private int _pathTarget = 0;

		private void EmuPath(EmuData d)
		{
			float speed = 5.0f;
			float maxD = _tickRate * 6.0f;

			Vector3 diff = _path[_pathTarget].position - d.position;
			Vector3 dir = diff.normalized;
			float magnitude = diff.magnitude;

			if (magnitude < maxD)
			{
				_pathTarget++;

				if (_pathTarget >= _path.Length)
				{
					_pathTarget = 0;
				}
			}
			else
			{
				d.velocity = dir * speed;
			}
		}
	}
}