using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shuyu.Interfaces
{
    public interface ICantDrawForHandFull
    {
        public Task CantDrawForHandFull(PlayerChoiceContext choiceContext, int count, Player player);
    }
}
