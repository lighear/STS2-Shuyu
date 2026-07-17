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
                nowActive = true;
                ColorRect? vfxNode = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals.Bounds.GetNodeOrNull<ColorRect>("VfxLianXuJingGePower");
                if (vfxNode != null)
                {
                    VfxActivate(vfxNode);
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
        if (creatureBounds != null && creatureBounds.GetNodeOrNull<ColorRect>("VfxLianXuJingGePower") == null)
        {
            string scenePath = $"{VFXUtil.PowerVfxPath}/vfx_LianXuJingGePower.tscn";
            ColorRect vfxLianXuJingGePower = VFXUtil.GenVFXNode<ColorRect>(scenePath);
            vfxLianXuJingGePower.Material = (ShaderMaterial)vfxLianXuJingGePower.Material.Duplicate();
            creatureBounds.AddChildSafely(vfxLianXuJingGePower);

            if (creatureVisual?.GetCurrentBody() is Sprite2D sprite && sprite.Texture != null)
            {
                Vector2 visualSize = sprite.Texture.GetSize() * sprite.Scale.Abs();
                float ringDiameter = Mathf.Max(visualSize.X, visualSize.Y);
                Vector2 visualCenter = creatureBounds.GetGlobalTransform().AffineInverse() * sprite.GlobalPosition;

                vfxLianXuJingGePower.AnchorLeft = 0f;
                vfxLianXuJingGePower.AnchorTop = 0f;
                vfxLianXuJingGePower.AnchorRight = 0f;
                vfxLianXuJingGePower.AnchorBottom = 0f;
                vfxLianXuJingGePower.Size = Vector2.One * ringDiameter;
                vfxLianXuJingGePower.Position = visualCenter - vfxLianXuJingGePower.Size * 0.5f;
            }
            else
            {
                vfxLianXuJingGePower.AnchorLeft = 0;
                vfxLianXuJingGePower.AnchorTop = 0;
                vfxLianXuJingGePower.AnchorRight = 1;
                vfxLianXuJingGePower.AnchorBottom = 1;
                Vector2 expandSize = creatureBounds.Size * 0.2f;
                vfxLianXuJingGePower.OffsetLeft = -expandSize.X;
                vfxLianXuJingGePower.OffsetTop = -expandSize.Y;
                vfxLianXuJingGePower.OffsetRight = expandSize.X;
                vfxLianXuJingGePower.OffsetBottom = expandSize.Y;
            }
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals.Bounds.GetNodeOrNull<ColorRect>("VfxLianXuJingGePower")?.QueueFree();
    }

    private void VfxActivate(ColorRect vfxNode)
    {
        if (vfxNode.Material is ShaderMaterial mat)
        {
            Tween tween = vfxNode.CreateTween();
            tween.TweenMethod(Callable.From<float>(val => mat.SetShaderParameter("active", val)), 0f, 1f, 0.2f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quad);
        }
    }
}