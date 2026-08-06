using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shuyu.DynamicVars;

/// <summary>
/// A calculated block variable whose combat preview uses the energy that an
/// X-cost card would currently spend. Its gameplay value still uses the
/// captured X value through <see cref="CardModel.ResolveEnergyXValue"/>.
/// </summary>
public sealed class CurrentEnergyXBlockVar : CalculatedBlockVar
{
    public CurrentEnergyXBlockVar(ValueProp props) : base(props)
    {
        WithMultiplier(static (card, _) => card.ResolveEnergyXValue());
    }

    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks)
    {
        EnchantmentModel? enchantment = card.Enchantment;
        if (enchantment is not null)
        {
            decimal baseValue = GetBaseVar().BaseValue;
            baseValue += enchantment.EnchantBlockAdditive(baseValue);
            baseValue *= enchantment.EnchantBlockMultiplicative(baseValue);
            if (card.IsEnchantmentPreview)
            {
                PreviewValue = baseValue;
            }
            else
            {
                EnchantedValue = baseValue;
            }
        }

        decimal previewBlock = CalculatePreviewBlock(card);
        ICombatState? combatState = card.CombatState;
        if (runGlobalHooks && combatState is not null)
        {
            PreviewValue = Hook.ModifyBlock(
                combatState,
                card.Owner.Creature,
                previewBlock,
                Props,
                card,
                null,
                out IEnumerable<AbstractModel> _);
        }
        else if (!card.IsEnchantmentPreview)
        {
            if (enchantment is not null)
            {
                previewBlock += enchantment.EnchantBlockAdditive(previewBlock);
                previewBlock *= enchantment.EnchantBlockMultiplicative(previewBlock);
            }

            PreviewValue = previewBlock;
        }
    }

    private decimal CalculatePreviewBlock(CardModel card)
    {
        decimal xValue = 0m;
        if (CombatManager.Instance.IsInProgress && card.CombatState is not null)
        {
            xValue = Hook.ModifyXValue(
                card.CombatState,
                card,
                card.EnergyCost.GetAmountToSpend());
        }

        return GetBaseVar().BaseValue + GetExtraVar().BaseValue * xValue;
    }
}
