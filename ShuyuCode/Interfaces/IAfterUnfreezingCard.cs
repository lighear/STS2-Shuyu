using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shuyu.Interfaces
{
    public interface IAfterUnfreezingCard
    {
        public Task AfterUnfreezingCard(PlayerChoiceContext choiceContext, CardModel card);
    }
}
