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
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
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

    private class Data
    {
        public bool isConverting;
    }

    private bool IsConverting
    {
        get
        {
            return GetInternalData<Data>().isConverting;
        }
        set
        {
            GetInternalData<Data>().isConverting = value;
        }
    }

    protected override object? InitInternalData()
    {
        return new Data() { isConverting = false };
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        if (target != Owner || !props.IsPoweredAttack() || target.GetPower<VulnerablePower>() != null)
        {
            return 1;
        }
        return DynamicVars["DamageIncrease"].BaseValue;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            await ConvertIfThresholdMet(choiceContext, applier);
        }
    }

    public async Task ConvertIfThresholdMet(PlayerChoiceContext choiceContext, Creature? applier)
    {
        if (Amount < 5 || IsConverting)
        {
            return;
        }

        IsConverting = true;
        try
        {
            Flash();
            IOnFragileConverted[] listeners =
                CombatState?.IterateHookListeners().OfType<IOnFragileConverted>().ToArray() ?? [];

            await PowerCmd.ModifyAmount(choiceContext, this, -5, null, null);

            foreach (IOnFragileConverted ip in listeners.OrderBy(ip => ip is ZhiHuanShuShiPower ? 0 : 1))
            {
                await ip.OnFragileConverted(choiceContext, Owner, applier);
            }

            if (Owner.CombatState != null && Owner.IsAlive)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner, 3, applier, null);
            }
        }
        finally
        {
            IsConverting = false;
        }
    }
}
