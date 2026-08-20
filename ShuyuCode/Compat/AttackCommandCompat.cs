using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Compat;

public static class AttackCommandCompat
{
#if STS2_107
    public static AttackCommand FromCard(
        this AttackCommand command,
        CardModel card,
        CardPlay? cardPlay)
    {
        return command.FromCard(card);
    }
#endif
}
