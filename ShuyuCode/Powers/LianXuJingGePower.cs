using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Cards;
using Shuyu.Characters;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class LianXuJingGePower : ModPowerTemplate
{
    private const float GroundWidthFactor = 0.967f;
    private const float GroundHeightFactor = 0.213f;
    private const float FootOffsetFactor = 0.39f;
    private const float RisingLightWidthFactor = 0.94f;
    private const float RisingLightHeightFactor = 0.31f;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(0, ValueProp.Unpowered),
        new CardsVar(0),
        new EnergyVar(0)
    ];

    public void ApplyStats(decimal block, decimal cards, decimal energy)
    {
        DynamicVars.Block.BaseValue = block;
        DynamicVars.Cards.BaseValue = cards;
        DynamicVars.Energy.BaseValue = energy;
    }
    
    private class Data
    {
        /// <summary>
        /// Keep track of the cards we've seen played and the power amount at the time they were played.
        /// This lets After Image avoid triggering on cards that started play before it was applied, and avoid gaining
        /// extra block on multiple plays of After Image.
        /// </summary>
        public readonly Dictionary<CardModel, bool> amountsForPlayedCards = new Dictionary<CardModel, bool>();
    }

    private bool nowActive = false;
    
    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Owner)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(cardPlay.Card, nowActive);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner && GetInternalData<Data>().amountsForPlayedCards.Remove(cardPlay.Card, out var active))
        {
            if (active && !(cardPlay.Card is BingZhen))
            {
                await PowerCmd.Remove(this);
            }
            else if (cardPlay.Card is BingZhen)
            {
                bool wasActive = nowActive;
                nowActive = true;
                NCreatureVisuals? creatureVisual = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals;
                if (!wasActive && creatureVisual != null)
                {
                    VfxActivate(creatureVisual);
                }

                await CreatureCmd.GainBlock(base.Owner, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null, fast: true);
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner.Player!);
                await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner.Player!);
            }
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        NCreatureVisuals? creatureVisual = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals;
        var creatureBounds = creatureVisual?.Bounds;
        if (creatureVisual != null && creatureBounds != null && GetVfxNode(creatureVisual) == null)
        {
            string scenePath = $"{VFXUtil.PowerVfxPath}/vfx_LianXuJingGePower.tscn";
            ColorRect vfxLianXuJingGePower = VFXUtil.GenVFXNode<ColorRect>(scenePath);
            vfxLianXuJingGePower.Material = (ShaderMaterial)vfxLianXuJingGePower.Material.Duplicate();
            creatureVisual.AddChildSafely(vfxLianXuJingGePower);

            // Bounds is a sibling drawn after Visuals, so anything parented to
            // Bounds will cover the character. Parent the floor beside the
            // body and insert it immediately before the body instead.
            Node2D body = creatureVisual.GetCurrentBody();
            creatureVisual.MoveChild(vfxLianXuJingGePower, body.GetIndex());

            string risingLightScenePath = $"{VFXUtil.PowerVfxPath}/vfx_LianXuJingGePower_rising_light.tscn";
            ColorRect risingLight = VFXUtil.GenVFXNode<ColorRect>(risingLightScenePath);
            risingLight.Material = (ShaderMaterial)risingLight.Material.Duplicate();
            if (risingLight.Material is ShaderMaterial risingLightMaterial)
            {
                risingLightMaterial.SetShaderParameter("ellipse_width_ratio", RisingLightWidthFactor);
                risingLightMaterial.SetShaderParameter(
                    "ground_curve_ratio",
                    GroundHeightFactor * 0.5f / RisingLightHeightFactor
                );
            }
            creatureVisual.AddChildSafely(risingLight);
            creatureVisual.MoveChild(risingLight, body.GetIndex() + 1);

            if (body is Sprite2D sprite && sprite.Texture != null)
            {
                Vector2 visualSize = sprite.Texture.GetSize() * sprite.Scale.Abs();
                Vector2 visualCenter = creatureVisual.GetGlobalTransform().AffineInverse() * sprite.GlobalPosition;
                Vector2 groundSize = new(
                    visualSize.X * GroundWidthFactor,
                    visualSize.Y * GroundHeightFactor
                );
                Vector2 footCenter = visualCenter + Vector2.Down * (visualSize.Y * FootOffsetFactor);

                vfxLianXuJingGePower.AnchorLeft = 0f;
                vfxLianXuJingGePower.AnchorTop = 0f;
                vfxLianXuJingGePower.AnchorRight = 0f;
                vfxLianXuJingGePower.AnchorBottom = 0f;
                vfxLianXuJingGePower.Size = groundSize;
                vfxLianXuJingGePower.Position = footCenter - groundSize * 0.5f;

                Vector2 risingLightSize = new(
                    groundSize.X * RisingLightWidthFactor,
                    visualSize.Y * RisingLightHeightFactor
                );
                float risingLightBottom = footCenter.Y + groundSize.Y * 0.5f;
                risingLight.Size = risingLightSize;
                risingLight.Position = new Vector2(
                    footCenter.X - risingLightSize.X * 0.5f,
                    risingLightBottom - risingLightSize.Y
                );
            }
            else
            {
                Vector2 groundSize = new(
                    creatureBounds.Size.X * GroundWidthFactor,
                    creatureBounds.Size.Y * GroundHeightFactor
                );
                Vector2 footCenter = creatureBounds.Position + new Vector2(
                    creatureBounds.Size.X * 0.5f,
                    creatureBounds.Size.Y * 0.86f
                );

                vfxLianXuJingGePower.AnchorLeft = 0f;
                vfxLianXuJingGePower.AnchorTop = 0f;
                vfxLianXuJingGePower.AnchorRight = 0f;
                vfxLianXuJingGePower.AnchorBottom = 0f;
                vfxLianXuJingGePower.Size = groundSize;
                vfxLianXuJingGePower.Position = footCenter - groundSize * 0.5f;

                Vector2 risingLightSize = new(
                    groundSize.X * RisingLightWidthFactor,
                    creatureBounds.Size.Y * RisingLightHeightFactor
                );
                float risingLightBottom = footCenter.Y + groundSize.Y * 0.5f;
                risingLight.Size = risingLightSize;
                risingLight.Position = new Vector2(
                    footCenter.X - risingLightSize.X * 0.5f,
                    risingLightBottom - risingLightSize.Y
                );
            }
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        NCreatureVisuals? creatureVisual = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals;
        GetVfxNode(creatureVisual)?.QueueFree();
        GetRisingLightNode(creatureVisual)?.QueueFree();
    }

    private static ColorRect? GetVfxNode(NCreatureVisuals? creatureVisual)
    {
        return creatureVisual?.GetNodeOrNull<ColorRect>("VfxLianXuJingGePower");
    }

    private static ColorRect? GetRisingLightNode(NCreatureVisuals? creatureVisual)
    {
        return creatureVisual?.GetNodeOrNull<ColorRect>("VfxLianXuJingGePowerRisingLight");
    }

    private void VfxActivate(NCreatureVisuals creatureVisual)
    {
        Tween tween = creatureVisual.CreateTween().SetParallel();
        foreach (ColorRect? vfxNode in new[] { GetVfxNode(creatureVisual), GetRisingLightNode(creatureVisual) })
        {
            if (vfxNode?.Material is not ShaderMaterial mat)
            {
                continue;
            }

            mat.SetShaderParameter("active", 0f);
            tween.TweenMethod(Callable.From<float>(val => mat.SetShaderParameter("active", val)), 0f, 1f, 0.65f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
    }
}
