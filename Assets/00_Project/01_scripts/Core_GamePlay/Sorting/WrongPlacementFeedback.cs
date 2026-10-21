using DG.Tweening;
using UnityEngine;

/// <summary> Wrong-shelf feedback on a placed item: tint, sound and floating text. </summary>
[DisallowMultipleComponent]
public class WrongPlacementFeedback : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("Tint flashed, then kept, while on a wrong-category shelf.")]
    [SerializeField] private Color tintColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private int flashCount = 3;
    [Tooltip("Seconds each flash on/off step lasts.")]
    [SerializeField] private float flashInterval = 0.08f;

    [Header("Sound")]
    [SerializeField] private bool playSound = true;

    [Header("Text")]
    [SerializeField] private bool showText = true;
    [SerializeField] private string message = "Wrong Section";
    [SerializeField] private Color textColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float textSize = 0.5f;
    [Tooltip("Metres the text floats up.")]
    [SerializeField] private float textRise = 0.1f;
    [SerializeField] private float textDuration = 1f;
    [Tooltip("Metres above the placed car where the text begins.")]
    [SerializeField] private float textHeightOffset = 0.55f;
    [Tooltip("Metres toward the aisle from the placed car.")]
    [SerializeField] private float textAisleOffset = 0.45f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MeshRenderer[] _models;
    private MaterialPropertyBlock _block;
    private Tween _flash;

    public bool IsFlashing => _flash != null && _flash.IsActive() && _flash.IsPlaying();

    /// <summary> Sets the renderer the tint is applied to. </summary>
    public void Bind(MeshRenderer model) => _models = model != null ? new[] { model } : null;

    /// <summary>Applies wrong-shelf tint to every visible part of a nested item.</summary>
    public void Bind(MeshRenderer[] models) => _models = models;

    /// <summary> Flashes the item's tint, then keeps it. </summary>
    public void PlayTint(bool flash)
    {
        _flash?.Kill();
        if (!flash)
        {
            SetTint(tintColor);
            return;
        }

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        for (int i = 0; i < flashCount; i++)
        {
            seq.AppendCallback(() => SetTint(tintColor)).AppendInterval(flashInterval);
            seq.AppendCallback(() => SetTint(Color.white)).AppendInterval(flashInterval);
        }
        seq.AppendCallback(() => SetTint(tintColor));
        _flash = seq;
    }

    /// <summary> Plays the wrong-section buzz at the item. </summary>
    public void PlaySound()
    {
        if (!playSound) return;

        SimpleAudioManager audio = SimpleAudioManager.instance;
        if (audio == null || audio.GameplaySounds == null) return;

        audio.PlayClipAt(audio.GameplaySounds.WrongSectionSFX, transform.position);
    }

    /// <summary>Shows floating text above the car and in front of its shelf.</summary>
    public void PlayText()
    {
        if (!showText) return;

        PlacedOnShelfItemBase placed = GetComponent<PlacedOnShelfItemBase>();
        Transform shelf = placed != null && placed.Shelf != null ? placed.Shelf.transform : transform;
        Vector3 start = transform.position + Vector3.up * textHeightOffset - shelf.forward * textAisleOffset;
        FloatingTextEffect.ShowKey(Localizer.KeyFromEnglish("feedback", message), start, textColor, textSize, textRise, textDuration);
    }

    /// <summary> Multiplies the renderer's base color via a property block. </summary>
    private void SetTint(Color color)
    {
        if (_models == null) return;
        _block ??= new MaterialPropertyBlock();
        foreach (MeshRenderer model in _models)
        {
            if (model == null) continue;
            _block.Clear();
            model.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            model.SetPropertyBlock(_block);
        }
    }
}
