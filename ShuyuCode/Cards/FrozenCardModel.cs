using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Commands;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards;

[RegisterCard(typeof(ShuyuCardPool))]
public sealed class FrozenCardModel : ModCardTemplate
{
    public CardModel? _visualCardModel;
    private List<Creature> targets = new List<Creature>();
    private int count;

    public FrozenCardModel() : base(
        baseCost: 0,
        CardType.None,
        CardRarity.None,
        TargetType.None,
        showInCardLibrary: false)
    { }

    public FrozenCardModel InitFrom(CardModel original)
    {
        AssertMutable();
        _visualCardModel = original;
        targets.Clear();
        count = 1;
        Owner ??= original.Owner;
        return this;
    }

    public void SetIcyDamageTargets(Creature target)
    {
        this.targets.Clear();
        this.targets.Add(target);
    }

    public void SetIcyDamageTargets(IEnumerable<Creature> targets)
    {
        this.targets.Clear();
        this.targets.AddRange(targets);
    }

    public void SetIcyDamageCount(int count)
    {
        this.count = count;
    }

    public override string Title => _visualCardModel?.Title ?? base.Title;
    public override string PortraitPath => _visualCardModel?.PortraitPath ?? base.PortraitPath;
    public override CardType Type => _visualCardModel?.Type ?? base.Type;
    public override CardRarity Rarity => _visualCardModel?.Rarity ?? base.Rarity;
    protected override int CanonicalEnergyCost => _visualCardModel?.EnergyCost.Canonical ?? base.CanonicalEnergyCost;
    public override CardPoolModel Pool => _visualCardModel?.Pool ?? base.Pool;
    public override CardPoolModel VisualCardPool => _visualCardModel?.VisualCardPool ?? base.VisualCardPool;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Unplayable
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(5),
        new ExtraDamageVar(5),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) => Math.Max(card.EnergyCost.GetAmountToSpend(), 0))
    ];

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card == this)
        {
            foreach (IOnFrozenCardDiscarded ip in CombatState!.IterateHookListeners().OfType<IOnFrozenCardDiscarded>())
            {
                await ip.OnFrozenCardDiscarded(choiceContext, this, Owner);
            }

            for (int i = 0; i < this.count; i++)
            {
                await ShuyuMechanismCmd.IcyDamage(choiceContext, 5 + 5 * Math.Max(EnergyCost.GetAmountToSpend(), 0), targets, this);
            }
            await ShuyuMechanismCmd.UnfreezeCard(this);
        }
    }
}