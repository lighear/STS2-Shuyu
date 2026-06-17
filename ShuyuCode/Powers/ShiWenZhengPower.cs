using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class ShiWenZhengPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("RemainCount", 3)
    ];

    private class Data
    {
        public bool selfApplied;
    }

    private bool SelfApplied
    {
        get
        {
            return GetInternalData<Data>().selfApplied;
        }
        set
        {
            GetInternalData<Data>().selfApplied = value;
        }
    }

    protected override object? InitInternalData()
    {
        return new Data() { selfApplied = false };
    }

    public void SetMaxStrengthLossCount(int count)
    {
        DynamicVars["RemainCount"].BaseValue = count;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount != 0 && power.GetTypeForAmount(amount) == PowerType.Debuff && power.Owner == Owner 
            && power is not StrengthPower && !(power is ITemporaryPower tempPower && tempPower.InternallyAppliedPower is StrengthPower))
        {
            if (SelfApplied)
            {
                SelfApplied = false;
                return;
            }
            if (DynamicVars["RemainCount"].BaseValue <= 0)
            {
                await PowerCmd.Remove(this);
                return;
            }

            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -Amount, applier, null);
            DynamicVars["RemainCount"].BaseValue--;
            if (DynamicVars["RemainCount"].BaseValue <= 0)
            {
                await PowerCmd.Remove(this);
            }
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SelfApplied = true;
        return base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}