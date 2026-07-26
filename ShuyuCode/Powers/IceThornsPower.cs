using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class IceThornsPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

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