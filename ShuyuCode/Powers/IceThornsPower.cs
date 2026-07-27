using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class IceThornsPower : ModPowerTemplate
{
    private const string VfxNodeName = "VfxIceThornsPower";
    private readonly string VfxScenePath = $"{VFXUtil.PowerVfxPath}/vfx_IceThornsPower.tscn";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DisplayAmountChanged += OnDisplayAmountChanged;
        UpdateVfx();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        DisplayAmountChanged -= OnDisplayAmountChanged;
        var creatureBounds = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals.Bounds;
        creatureBounds?.GetNodeOrNull<NIceThornsPowerVfx>(VfxNodeName)?.QueueFree();
        return Task.CompletedTask;
    }

    private void OnDisplayAmountChanged()
    {
        UpdateVfx();
    }

    private void UpdateVfx()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        var creatureVisuals = creatureNode?.Visuals;
        var creatureBounds = creatureVisuals?.Bounds;
        if (creatureNode == null || creatureVisuals == null || creatureBounds == null)
        {
            return;
        }

        NPowerVfxLayout.Resolve(
            Owner,
            creatureNode,
            out Vector2 visualSize,
            out Vector2 visualCenter);

        NIceThornsPowerVfx? vfx = creatureBounds.GetNodeOrNull<NIceThornsPowerVfx>(VfxNodeName);
        if (vfx == null)
        {
            vfx = VFXUtil.GenVFXNode<NIceThornsPowerVfx>(VfxScenePath);
            creatureBounds.AddChildSafely(vfx);
        }

        vfx.Position = visualCenter;
        vfx.Configure(visualSize, Amount);
    }

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && props.IsPoweredAttack())
        {
            if (dealer != null && dealer.IsEnemy)
            {
                Flash();
                await ReflectionEffect(choiceContext, dealer);
            }
            if (amount >= 1)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }

    public async Task ReflectionEffect(PlayerChoiceContext choiceContext, Creature target)
    {
        ZhaMaoPower? power = Owner.GetPower<ZhaMaoPower>();
        int damage = await CalculateReflectionDamage(power, target);
        IEnumerable<DamageResult> results = await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner);

        if (power != null)
        {
            decimal amount = power.GetFragilePowerAmount();
            foreach (DamageResult result in results)
            {
                if (result.TotalDamage > 0)
                {
                    await PowerCmd.Apply<FragilePower>(choiceContext, result.Receiver, amount, Owner, null);
                }
            }
        }
    }

    public async Task<int> CalculateReflectionDamage(ZhaMaoPower? power, Creature target)
    {
        if (power != null && (target.HasPower<FragilePower>() || target.HasPower<VulnerablePower>()))
        {
            power.Flash();
            return (int)(Amount * (1 + power.Amount / 100m));
        }
        return Amount;
    }
}
