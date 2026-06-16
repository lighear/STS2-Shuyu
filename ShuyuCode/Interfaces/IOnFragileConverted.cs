using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shuyu.Interfaces
{
    public interface IOnFragileConverted
    {
        public Task OnFragileConverted(PlayerChoiceContext choiceContext, Creature powerOwner, Creature? powerApplier);
    }
}
