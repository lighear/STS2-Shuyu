using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class WanBiBuPoPower : ModPowerTemplate, IModifyDamageFinal
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    private class Data
    {
        public decimal damageReceiving;
        public Creature? damageReturnTarget;
    }

    protected override object? InitInternalData()
    {
        return new Data()
        {
            damageReceiving = 0,
            damageReturnTarget = null
        };
    }

    public decimal ModifyDamageFinal(ICombatState combatState, Creature? target, Creature? dealer, ValueProp props, decimal modifiedAmount, ref IEnumerable<AbstractModel> modifiers)
    {
        if (target == Owner)
        {
            if (combatState.CurrentSide == Owner.Side && props.IsPoweredAttack() && dealer != null && dealer.IsEnemy)
            {
                return modifiedAmount;
            }

            Data data = GetInternalData<Data>();
            if (props.IsPoweredAttack())
            {
                data.damageReceiving = modifiedAmount;
                data.damageReturnTarget = dealer;
            }
            else
            {
                data.damageReceiving = 0;
                data.damageReturnTarget = null;
            }
            List<AbstractModel> list = [..modifiers, this];
            modifiers = list;
            return 0;
        }
        return modifiedAmount;
    }

    public override async Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();
        if (data.damageReturnTarget != null)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), data.damageReturnTarget, data.damageReceiving, ValueProp.Unpowered, Owner);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner.Side != side)
        {
            await PowerCmd.Decrement(this);
        }
    }

    private Material? saveMaterial;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        NCreatureVisuals? creatureVisual = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals;
        Material shaderMaterial = PreloadManager.Cache.GetMaterial("res://Shuyu/assets/materials/vfx_WanBiBuPoPower.tres");
        if (creatureVisual == null || shaderMaterial == null)
        {
            return;
        }

        if (creatureVisual.IsSpineNode)
        {
            saveMaterial = creatureVisual.SpineBody?.GetNormalMaterial();
            creatureVisual.SpineBody?.SetNormalMaterial((Material)shaderMaterial.Duplicate());
        }
        else
        {
            Node2D body = creatureVisual.GetCurrentBody();
            saveMaterial = body.Material;
            body.Material = (Material)shaderMaterial.Duplicate();
        }


        var creatureBounds = creatureVisual.Bounds;
        if (creatureBounds != null && creatureBounds.GetNodeOrNull<ColorRect>("VfxWanBiBuPoPowerRing") == null)
        {
            string scenePath = "res://Shuyu/scenes/vfx_WanBiBuPoPower_ring.tscn";
            ColorRect vfxWanBiBuPoPowerRing = VFXUtil.GenVFXNode<ColorRect>(scenePath);
            creatureBounds.AddChildSafely(vfxWanBiBuPoPowerRing);

            if (Owner.Player?.Character is ShuyuCharacter)
            {
                vfxWanBiBuPoPowerRing.Size = new Vector2(2378 * 0.194f, 2378 * 0.194f);
                vfxWanBiBuPoPowerRing.Position = new Vector2(-1189 * 0.194f + 82, -1189 * 0.194f);
            }
            else
            {
                vfxWanBiBuPoPowerRing.AnchorLeft = 0;
                vfxWanBiBuPoPowerRing.AnchorTop = 0;
                vfxWanBiBuPoPowerRing.AnchorRight = 1;
                vfxWanBiBuPoPowerRing.AnchorBottom = 1;
                Vector2 expandSize = creatureBounds.Size * 0.2f;
                vfxWanBiBuPoPowerRing.OffsetLeft = -expandSize.X;
                vfxWanBiBuPoPowerRing.OffsetTop = -expandSize.Y;
                vfxWanBiBuPoPowerRing.OffsetRight = expandSize.X;
                vfxWanBiBuPoPowerRing.OffsetBottom = expandSize.Y;
            }
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        NCreatureVisuals? creatureVisual = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals;
        if (creatureVisual == null)
        {
            return;
        }

        if (creatureVisual.IsSpineNode)
        {
            creatureVisual.SpineBody?.SetNormalMaterial(saveMaterial);
            saveMaterial = null;
        }
        else
        {
            Node2D body = creatureVisual.GetCurrentBody();
            body.Material = saveMaterial;
            saveMaterial = null;
        }


        creatureVisual.Bounds.GetNodeOrNull<ColorRect>("VfxWanBiBuPoPowerRing")?.QueueFree();
    }
}
