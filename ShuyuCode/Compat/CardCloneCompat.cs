using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Compat;

public static class CardCloneCompat
{
#if STS2_107
    public static CardModel CreateCloneForPlayer(this CardModel card, Player newOwner)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(newOwner);

        CardModel clone = card.CreateClone();
        if (ReferenceEquals(clone.Owner, newOwner))
        {
            return clone;
        }

        // 0.107.1 has CreateClone(), but not the 0.108 convenience APIs
        // CreateCloneForPlayer/GiveToAnotherPlayer. The Owner setter requires
        // ownership to be cleared before assigning a different player.
        clone.Owner = null!;
        clone.Owner = newOwner;
        return clone;
    }
#endif
}
