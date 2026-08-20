using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shuyu.Compat;

public static class CreatureCmdCompat
{
    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        CardModel cardSource,
        CardPlay? cardPlay)
    {
#if STS2_107
        return CreatureCmd.Damage(choiceContext, target, amount, props, cardSource);
#else
        return CreatureCmd.Damage(choiceContext, target, amount, props, cardSource, cardPlay);
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
#if STS2_107
        return CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource);
#else
        return CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource, cardPlay);
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
#if STS2_107
        return CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource);
#else
        return CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource, cardPlay);
#endif
    }

    public static Task LoseBlock(
        PlayerChoiceContext choiceContext,
        Creature creature,
        decimal amount,
        Creature? remover)
    {
#if STS2_107
        return CreatureCmd.LoseBlock(creature, amount);
#else
        return CreatureCmd.LoseBlock(choiceContext, creature, amount, remover);
#endif
    }
}
