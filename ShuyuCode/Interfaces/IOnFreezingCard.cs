using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Interfaces
{
    public interface IOnFreezingCard
    {
        public Task OnFreezingCard(CardModel card);
    }
}
