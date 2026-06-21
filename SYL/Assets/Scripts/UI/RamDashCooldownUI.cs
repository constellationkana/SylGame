using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RamDashCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RamDashAbility ramDashAbility;
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color chargingColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private Color readyColor = new Color(0.4f, 0.85f, 1f);
    [FormerlySerializedAs("cooldownDrainingColor")]
    [SerializeField] private Color cooldownStartColor = Color.red;
    [FormerlySerializedAs("cooldownFillingColor")]
    [SerializeField] private Color cooldownEndColor = Color.blue;

    private void Awake()
    {
        ResolveSliderReferences();
    }

    private void Update()
    {
        Refresh();
    }

    public void SetRamDashAbility(RamDashAbility ability)
    {
        ramDashAbility = ability;
    }

    public void SetSliderReferences(Slider slider, Image fill)
    {
        cooldownSlider = slider;
        fillImage = fill;
        ResolveSliderReferences();
    }

    public void SetColors(Color charging, Color ready, Color cooldownStart, Color cooldownEnd)
    {
        chargingColor = charging;
        readyColor = ready;
        cooldownStartColor = cooldownStart;
        cooldownEndColor = cooldownEnd;
    }

    public void Refresh()
    {
        ResolveSliderReferences();

        if (cooldownSlider == null)
        {
            return;
        }

        if (ramDashAbility == null)
        {
            ramDashAbility = FindAnyObjectByType<RamDashAbility>();
        }

        float progress = ramDashAbility != null ? ramDashAbility.DisplayProgress : 1f;
        cooldownSlider.SetValueWithoutNotify(progress);

        if (fillImage != null)
        {
            fillImage.color = ramDashAbility != null ? GetFillColor(ramDashAbility.DisplayPhase) : readyColor;
        }
    }

    private Color GetFillColor(RamDashAbility.RamDashDisplayPhase displayPhase)
    {
        switch (displayPhase)
        {
            case RamDashAbility.RamDashDisplayPhase.Charging:
                return chargingColor;
            case RamDashAbility.RamDashDisplayPhase.Charged:
            case RamDashAbility.RamDashDisplayPhase.Ready:
                return readyColor;
            case RamDashAbility.RamDashDisplayPhase.Dashing:
            case RamDashAbility.RamDashDisplayPhase.CooldownDraining:
            case RamDashAbility.RamDashDisplayPhase.CooldownFilling:
                return Color.Lerp(cooldownStartColor, cooldownEndColor, ramDashAbility.DisplayProgress);
            default:
                return readyColor;
        }
    }

    private void ResolveSliderReferences()
    {
        if (cooldownSlider == null)
        {
            cooldownSlider = GetComponent<Slider>();
        }

        if (fillImage == null && cooldownSlider != null && cooldownSlider.fillRect != null)
        {
            fillImage = cooldownSlider.fillRect.GetComponent<Image>();
        }
    }
}
