using UnityEngine;

namespace MemoryDiagnostics
{
    public sealed class MemoryDiagnosticsOverlay : MonoBehaviour
    {
        public enum OverlayAnchor
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3
        }

        [SerializeField] private OverlayAnchor _anchor = OverlayAnchor.TopLeft;
        [SerializeField] private Vector2 _margin = new Vector2(10f, 10f);
        [SerializeField] private Vector2 _size = new Vector2(240f, 48f);
        [SerializeField] private int _fontSize = 12;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private bool _showPeak = true;

        private string _text = string.Empty;
        private GUIStyle _style;

        public static MemoryDiagnosticsOverlay Show()
        {
            var existing = FindObjectOfType<MemoryDiagnosticsOverlay>();
            if (existing != null) return existing;
            var go = new GameObject("MemoryDiagnosticsOverlay");
            var overlay = go.AddComponent<MemoryDiagnosticsOverlay>();
            DontDestroyOnLoad(go);
            return overlay;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            var md = MemoryDiagnosticsManager.Initialize();
            md.OnSample += OnSample;
            EnsureStyle();
        }

        private void OnDisable()
        {
            var md = MemoryDiagnosticsManager.Instance;
            if (md != null) md.OnSample -= OnSample;
        }

        private void OnSample(MemoryDiagSnapshot s)
        {
            if (_showPeak)
            {
                _text = $"Mem: {s.currentMemoryMB:F1} MB\nPeak: {s.peakMemoryMB:F1} MB";
            }
            else
            {
                _text = $"Mem: {s.currentMemoryMB:F1} MB";
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_text)) return;
            if (_style == null) EnsureStyle();
            var rect = GetRect();
            GUI.Label(rect, _text, _style);
        }

        private void EnsureStyle()
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize
            };
            _style.normal.textColor = _textColor;
        }

        private Rect GetRect()
        {
            var size = _size;
            var safe = Screen.safeArea;
            Vector2 pos;
            switch (_anchor)
            {
                case OverlayAnchor.TopRight:
                    pos = new Vector2(safe.xMax - size.x - _margin.x, safe.yMin + _margin.y);
                    break;
                case OverlayAnchor.BottomLeft:
                    pos = new Vector2(safe.xMin + _margin.x, safe.yMax - size.y - _margin.y);
                    break;
                case OverlayAnchor.BottomRight:
                    pos = new Vector2(safe.xMax - size.x - _margin.x, safe.yMax - size.y - _margin.y);
                    break;
                default:
                    pos = new Vector2(safe.xMin + _margin.x, safe.yMin + _margin.y);
                    break;
            }
            return new Rect(pos.x, pos.y, size.x, size.y);
        }
    }
}
