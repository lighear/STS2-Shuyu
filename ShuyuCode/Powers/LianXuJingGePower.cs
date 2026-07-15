using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class LianXuJingGePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(0, ValueProp.Unpowered),
        new CardsVar(0),
        new EnergyVar(0)
    ];

    public void ApplyStats(decimal block, decimal cards, decimal energy)
    {
        DynamicVars.Block.BaseValue = block;
        DynamicVars.Cards.BaseValue = cards;
        DynamicVars.Energy.BaseValue = energy;
    }
    
    private class Data
    {
        /// <summary>
        /// Keep track of the cards we've seen played and the power amount at the time they were played.
        /// This lets After Image avoid triggering on cards that started play before it was applied, and avoid gaining
        /// extra block on multiple plays of After Image.
        /// </summary>
        public readonly Dictionary<CardModel, bool> amountsForPlayedCards = new Dictionary<CardModel, bool>();
    }

    private bool nowActive = false;
    
    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Owner)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(cardPlay.Card, nowActive);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner && GetInternalData<Data>().amountsForPlayedCards.Remove(cardPlay.Card, out var active))
        {
            if (active && !(cardPlay.Card is BingZhen))
            {
                await PowerCmd.Remove(this);
            }
            else if (cardPlay.Card is BingZhen)
            {
                nowActive = true;
                await CreatureCmd.GainBlock(base.Owner, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null, fast: true);
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner.Player!);
                await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner.Player!);
            }
        }
    }
}