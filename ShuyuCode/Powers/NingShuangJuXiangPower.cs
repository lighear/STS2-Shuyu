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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class NingShuangJuXiangPower : ModPowerTemplate
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

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("DamageDecrease", 0.5m)
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

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && props.IsPoweredAttack() && dealer != null && (dealer.HasPower<ChillPower>() || CreatureList.Contains(dealer))
        {
            return DynamicVars["DamageDecrease"].BaseValue;
        }
        else
        {
            return 1;
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
        {
            foreach (Creature enemy in CombatState.HittableEnemies.Where(c => c.HasPower<ChillPower>()))
            {
                CreatureList.Add(enemy);
            }
        }

        if (side == CombatSide.Enemy)
        {
            CreatureList.Clear();
            await PowerCmd.Decrement(this);
        }
    }
}