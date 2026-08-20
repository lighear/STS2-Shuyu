using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Helpers;
using Shuyu.Characters;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class BingWuPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

#if STS2_107
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
#else
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
#endif
    {
        if (CombatState.CurrentSide == Owner.Side)
        {
            return 1;
        }

        if (target == Owner && props.IsPoweredAttack() && amount >= 1)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    public override async Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Flash();
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner.Side != side)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        var creatureVisuals = creatureNode?.Visuals;
        var creatureBounds = creatureVisuals?.Bounds;
        if (creatureNode != null && creatureVisuals != null && creatureBounds != null && creatureBounds.GetNodeOrNull<ColorRect>("VfxBingWuPower") == null)
        {
            string scenePath = $"{VFXUtil.PowerVfxPath}/vfx_BingWuPower.tscn";
            ColorRect vfxBingWuPower = VFXUtil.GenVFXNode<ColorRect>(scenePath);
            creatureBounds.AddChildSafely(vfxBingWuPower);

            Vector2 effectSize;
            Vector2 visualCenter;
            if (Owner.Player?.Character is ShuyuCharacter && creatureVisuals.IsSpineNode)
            {
                NPowerVfxLayout.Resolve(
                    Owner,
                    creatureNode,
                    out effectSize,
                    out visualCenter);
            }
            else if (creatureVisuals.GetCurrentBody() is Sprite2D sprite && sprite.Texture != null)
            {
                Vector2 spriteSize = sprite.Texture.GetSize() * sprite.Scale.Abs();
                effectSize = new Vector2(spriteSize.X * (1924f / 2378f), spriteSize.Y);
                visualCenter =
                    creatureBounds.GetGlobalTransform().AffineInverse()
                    * sprite.GlobalPosition;
            }
            else
            {
                effectSize = creatureBounds.Size * 1.4f;
                visualCenter = creatureBounds.Size * 0.5f;
            }

            vfxBingWuPower.AnchorLeft = 0f;
            vfxBingWuPower.AnchorTop = 0f;
            vfxBingWuPower.AnchorRight = 0f;
            vfxBingWuPower.AnchorBottom = 0f;
            vfxBingWuPower.Size = effectSize;
            vfxBingWuPower.Position = visualCenter - effectSize * 0.5f;

            if (vfxBingWuPower.Material is ShaderMaterial sharedMaterial)
            {
                ShaderMaterial material = (ShaderMaterial)sharedMaterial.Duplicate();
                material.SetShaderParameter("aspect_ratio", effectSize.Y / effectSize.X);
                material.SetShaderParameter("fog_color", Colors.White);
                vfxBingWuPower.Material = material;
            }
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        var creatureBounds = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals.Bounds;
        creatureBounds?.GetNodeOrNull<ColorRect>("VfxBingWuPower")?.QueueFree();
    }
}
