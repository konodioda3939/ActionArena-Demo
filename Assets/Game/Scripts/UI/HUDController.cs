using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ActionArena.Core;

namespace ActionArena.UI
{
    /// <summary>
    /// HUD 控制器：订阅分数 / 玩家血量 / 波次事件，刷新对应 UI 控件。
    /// 所有字段可选（没拖的控件会被跳过），方便先用部分 UI 跑起来。
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("文本")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _waveText;
        [SerializeField] private TMP_Text _enemiesText;

        [Header("血条")]
        [Tooltip("Image，Type = Filled")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private TMP_Text _healthText;

        private void OnEnable()
        {
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<WaveChangedEvent>(OnWaveChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<WaveChangedEvent>(OnWaveChanged);
        }

        private void OnScoreChanged(ScoreChangedEvent e)
            => SetText(_scoreText, $"分数 {e.Score}");

        private void OnHealthChanged(HealthChangedEvent e)
        {
            if (!e.IsPlayer) return; // HUD 只显示玩家血量
            if (_healthFill != null)
                _healthFill.fillAmount = e.Max > 0f ? e.Current / e.Max : 0f;
            SetText(_healthText, $"{Mathf.CeilToInt(e.Current)} / {Mathf.CeilToInt(e.Max)}");
        }

        private void OnWaveChanged(WaveChangedEvent e)
        {
            SetText(_waveText, $"第 {e.WaveIndex} 波");
            SetText(_enemiesText, $"剩余 {e.EnemiesRemaining}");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}
