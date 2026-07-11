using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
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
        Material shaderMaterial = PreloadManager.Cache.GetMaterial("res://Shuyu/assets/materials/vfx_WanBiBuPoPower.tres");
        Node2D? body = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals?.GetCurrentBody();
        if (shaderMaterial != null && body != null)
        {
            saveMaterial = body.Material;
            body.Material = (Material)shaderMaterial.Duplicate();
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        Node2D? body = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals?.GetCurrentBody();
        if (body != null)
        {
            body.Material = saveMaterial;
            saveMaterial = null;
        }
    }
}
