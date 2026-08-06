using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Shuyu.Vfx;

/// <summary>
/// Displays the localized Fragile break callout using the base game's floating
/// "Blocked" text effect. This is presentation-only and does not participate in
/// Fragile conversion or damage resolution.
/// </summary>
public static class FragileBreakTextVfx
{
    private const string BreakSfx = "glass_orb_evoke.mp3";
    private static readonly LocString BrokenText = new("vfx", "SHUYU_FRAGILE_BROKEN");
    private const string ColoredLabelName = "FragileBreakLabel";

    public static void Play(Creature target)
    {
        if (!target.IsEnemy)
        {
            return;
        }

        NDamageBlockedVfx? vfx = NDamageBlockedVfx.Create(target);
        if (vfx == null)
        {
            return;
        }

        Control? vfxContainer = target.GetVfxContainer();
        if (vfxContainer == null)
        {
            vfx.QueueFreeSafely();
            return;
        }

        NDebugAudioManager.Instance?.Play(BreakSfx, variance: PitchVariance.Small);
        vfxContainer.AddChildSafely(vfx);
        SetLocalizedText();
        Callable.From(SetLocalizedText).CallDeferred();

        void SetLocalizedText()
        {
            if (!GodotObject.IsInstanceValid(vfx))
            {
                return;
            }

            MegaLabel? sourceLabel = vfx.GetNodeOrNull<MegaLabel>("Label");
            if (sourceLabel == null)
            {
                return;
            }

            MegaLabel? coloredLabel = vfx.GetNodeOrNull<MegaLabel>(ColoredLabelName);
            if (coloredLabel == null)
            {
                coloredLabel = sourceLabel.Duplicate() as MegaLabel;
                if (coloredLabel == null)
                {
                    return;
                }

                coloredLabel.Name = ColoredLabelName;
                coloredLabel.Modulate = Colors.White;
                coloredLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.gold);
                vfx.AddChild(coloredLabel);
                sourceLabel.Visible = false;
            }

            coloredLabel.SetTextAutoSize(BrokenText.GetFormattedText());
        }
    }
}
