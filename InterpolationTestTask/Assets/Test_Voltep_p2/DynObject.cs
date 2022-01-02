using System;
using UnityEngine;


namespace VTest
{
    public class DynObject : MonoBehaviour
    {
        private Vector3 _newPosition;
        private Vector3 _oldPosition;
        private Vector3 _velocity;
        private float _time;
        private bool _teleport;

        private bool _canMove;

        public Action<ObjectSnapshotData> onData;


        private void Start()
        {
            SetStartSettings();
            onData += (ObjectSnapshotData data) =>
      {
          GetData(data);
          CanMoveCheck();
          if (_canMove)
          {
              SetOldPosition(data.position);
          }
      };
        }

        private void Update()
        {
            if (_canMove)
            {
                if (_teleport == false)
                {
                    Move();
                }
                else
                {
                    Teleport();
                }
            }
        }

        private void Move()
        {
            transform.position = Vector3.SmoothDamp(this.transform.position, _newPosition, ref _velocity, _time);
        }

        private void Teleport()
        {
            _canMove = false;
            _teleport = false;
            SetOldPosition(this.transform.position);
            this.transform.position = _newPosition;
        }

        private void SetStartSettings()
        {
            _newPosition = this.transform.position;
            SetOldPosition(_newPosition + Vector3.one);
        }

        private void GetData(ObjectSnapshotData data)
        {
            _newPosition = data.position;
            _velocity = data.velocity;
            _time = data.time;
            _teleport = data.teleport;
        }

        private void CanMoveCheck()
        {
            if (_newPosition != _oldPosition)
            {
                _canMove = true;
            }
            else
            {
                _canMove = false;
            }
        }

        private void SetOldPosition(Vector3 position)
        {
            _oldPosition = position;
        }
    }
}
