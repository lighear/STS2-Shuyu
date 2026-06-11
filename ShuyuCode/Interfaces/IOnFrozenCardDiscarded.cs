using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Interfaces
{
    public interface IOnFrozenCardDiscarded
    {
        public Task OnFrozenCardDiscarded(PlayerChoiceContext choiceContext, CardModel card);
    }
}
