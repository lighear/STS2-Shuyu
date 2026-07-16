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
using Shuyu.Characters;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class IceThornsPower : ModPowerTemplate
{
    private const string VfxNodeName = "VfxIceThornsPower";
    private const string VfxScenePath = "res://Shuyu/scenes/vfx_IceThornsPower.tscn";

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
        var creatureVisuals = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals;
        var creatureBounds = creatureVisuals?.Bounds;
        if (creatureVisuals == null || creatureBounds == null)
        {
            return;
        }

        Vector2 visualSize = creatureBounds.Size;
        Vector2 visualCenter = creatureBounds.Size * 0.5f;

        // Shuyu's portrait is much taller than her gameplay hitbox. Use the
        // actual sprite rectangle so the thorn ring follows the full artwork.
        if (Owner.Player?.Character is ShuyuCharacter &&
            creatureVisuals.GetCurrentBody() is Sprite2D sprite &&
            sprite.Texture != null)
        {
            visualSize = sprite.Texture.GetSize() * sprite.Scale.Abs();
            visualCenter = creatureBounds.GetGlobalTransform().AffineInverse() * sprite.GlobalPosition;
        }

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
        PoPianPower? power = Owner.GetPower<PoPianPower>();
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

    public async Task<int> CalculateReflectionDamage(PoPianPower? power, Creature target)
    {
        if (power != null && (target.HasPower<FragilePower>() || target.HasPower<VulnerablePower>()))
        {
            power.Flash();
            return (int)(Amount * (1 + power.Amount / 100m));
        }
        return Amount;
    }
}
