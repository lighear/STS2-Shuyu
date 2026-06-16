using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shuyu.Cards;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class ChongYingZhenPower : ModPowerTemplate, IPowerExtraIconAmountLabelSpecsProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<BingZhen>(),
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(0)
    ];

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is ChongYingZhenPower && power.Owner == Owner)
        {
            IEnumerable<CardModel> cards = Owner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
            foreach (CardModel card in cards)
            {
                TryUpgrade(card, (int)amount);
            }
        }
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.IsClone)
        {
            return;
        }
        TryUpgrade(card, Amount);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        IEnumerable<CardModel> cards = oldOwner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
        foreach (CardModel card in cards)
        {
            if (card is BingZhen)
            {
                card.BaseReplayCount -= Amount;
            }
        }
    }

    private void TryUpgrade(CardModel card, int amount)
    {
        if (card.Owner == Owner.Player && card is BingZhen)
        {
            card.EnergyCost.AddThisCombat(amount);
            CardCmd.Upgrade(card);
            card.BaseReplayCount += amount;
        }
    }


    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is BingZhen && cardPlay.Card.Owner.Creature == Owner)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, DynamicVars.Strength.BaseValue, Owner, null);
        }
    }

    public void AddStrenthPowerAmount(decimal amount)
    {
        DynamicVars.Strength.BaseValue += amount;
        InvokeDisplayAmountChanged();
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        return
        [
            ExtraIconAmountLabelSpec.Plain(ExtraIconAmountLabelCorner.TopRight, DynamicVars.Strength.BaseValue.ToString())
        ];
    }
}
