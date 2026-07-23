using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class QianMianJingLangPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<IceThornsPower>()
    ];

    private bool hasAlreadyBeenGivenIceThorns;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is IceThornsPower && power.Owner == Owner && amount > 0 && !hasAlreadyBeenGivenIceThorns)
        {
            IEnumerable<Creature> enumerable = CombatState!.GetTeammatesOf(Owner).Where(c => c.IsAlive && c.IsPlayer && c != Owner);
            hasAlreadyBeenGivenIceThorns = true;
            foreach (Creature creature in enumerable)
            {
                await PowerCmd.Apply<IceThornsPower>(choiceContext, creature, amount, Owner, null);
            }
            hasAlreadyBeenGivenIceThorns = false;
        }
    }
}