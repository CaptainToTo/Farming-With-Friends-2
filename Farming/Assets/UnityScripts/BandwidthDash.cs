using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OwlTree.Unity
{
    public class BandwidthDash : MonoBehaviour
    {
        [SerializeField] private UnityConnection _connection;

        [SerializeField] private float _updateFrequency = 0.5f;
        [SerializeField] private TextMeshProUGUI _clientText;
        [SerializeField] private TextMeshProUGUI _recvText;
        [SerializeField] private TextMeshProUGUI _sendText;

        private float _lastUpdate;

        void Awake()
        {
            _connection.OnReady.AddListener((id) => _clientText.text = id.ToString() + " Bandwidth");
        }

        void Update()
        {
            if (Time.time - _lastUpdate > _updateFrequency)
            {
                var b = _connection.Bandwidth;
                _recvText.text = $"Recv: {b.IncomingKbPerSecond():F2} Kb/s";
                _sendText.text = $"Send: {b.OutgoingKbPerSecond():F2} Kb/s";
                _lastUpdate = Time.time;
            }
        }
    }
}
