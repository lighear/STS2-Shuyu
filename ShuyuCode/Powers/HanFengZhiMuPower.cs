using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class HanFengZhiMuPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<ChillPower>()
    ];

    private class Data
    {
        public HashSet<Creature> creatureList;
        public Data()
        {
            creatureList = new HashSet<Creature>();
        }
    }

    private HashSet<Creature> CreatureList
    {
        get
        {
            return GetInternalData<Data>().creatureList;
        }
    }

    protected override object? InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult _, ValueProp props, Creature? dealer, CardModel? __)
    {
        if (target == Owner && dealer != null && props.IsPoweredAttack() && !CreatureList.Contains(dealer))
        {
            Flash();
            await PowerCmd.Apply<ChillPower>(choiceContext, dealer, Amount, Owner, null);
            CreatureList.Add(dealer);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner.Side != side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
