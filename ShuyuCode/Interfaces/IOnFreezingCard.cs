using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shuyu.Interfaces
{
    public interface IOnFreezingCard
    {
        // 返回false表示解除card的封冻
        public Task<bool> OnFreezingCard(PlayerChoiceContext choiceContext, CardModel card);
    }
}
