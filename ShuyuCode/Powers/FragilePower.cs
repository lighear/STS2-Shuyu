using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class FragilePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("DamageIncrease", 1.25m)
    ];

    private int ExtraDamageWhenTransformation
    {
        get
        {
            IEnumerable<Creature> source = Owner.CombatState!.GetOpponentsOf(Owner).Where(c => c.IsAlive);
            return source.Sum(c => c.GetPowerAmount<SuiJiaQiangHuaPower>());
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack() || target.GetPower<VulnerablePower>() != null)
        {
            return 1;
        }
        return DynamicVars["DamageIncrease"].BaseValue;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && Amount >= 5)
        {
            Flash();
            await TransformationEffect(choiceContext);
            await PowerCmd.ModifyAmount(choiceContext, this, -5, null, null);
        }
    }

    private async Task TransformationEffect(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner, 3, Applier, null);

        int damage = ExtraDamageWhenTransformation;
        if (damage > 0)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, Owner, 3, Applier, null);
            await CreatureCmd.Damage(choiceContext, Owner, damage, ValueProp.Unpowered, Applier, null);
        }
    }
}