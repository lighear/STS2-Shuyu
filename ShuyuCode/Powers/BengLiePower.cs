using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class BengLiePower : ModPowerTemplate, IOnFragileConverted
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FragilePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(0, ValueProp.Unpowered),
        new PowerVar<FragilePower>(0)
    ];

    public void AddExtraDamage(decimal damage)
    {
        DynamicVars.Damage.BaseValue += damage;
    }

    public void AddFragilePowerAmount(decimal amount)
    {
        DynamicVars["FragilePower"].BaseValue += amount;
    }

    public async Task OnFragileConverted(PlayerChoiceContext choiceContext, Creature powerOwner, Creature? powerApplier)
    {
        await CreatureCmd.Damage(choiceContext, powerOwner, Amount, ValueProp.Unpowered, powerApplier, null);

        var otherEnemies = CombatState.HittableEnemies.Where(enemy => enemy != powerOwner);
        await CreatureCmd.Damage(choiceContext, otherEnemies, DynamicVars.Damage, powerApplier, null);
        await PowerCmd.Apply<FragilePower>(choiceContext, otherEnemies, DynamicVars["FragilePower"].BaseValue, powerApplier, null);
    }
}