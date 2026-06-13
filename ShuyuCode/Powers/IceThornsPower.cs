using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
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
            if (dealer != null)
            {
                Flash();
                await ReflectionEffect(choiceContext, dealer);
            }
            if (amount > 0)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }

    private async Task ReflectionEffect(PlayerChoiceContext choiceContext, Creature target)
    {
        int damage = Amount;
        int extraDamagePercent = Owner.GetPowerAmount<PoPianPower>();
        if (target.HasPower<FragilePower>() || target.HasPower<VulnerablePower>())
        {
            damage = (int)(damage * (1 + extraDamagePercent / 100m));
        }
        IEnumerable<DamageResult> results = await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner, null);

        if (extraDamagePercent > 0)
        {
            foreach (DamageResult result in results)
            {
                if (result.TotalDamage > 0)
                {
                    await PowerCmd.Apply<FragilePower>(choiceContext, result.Receiver, 1, Owner, null);
                }
            }
        }
    }
}