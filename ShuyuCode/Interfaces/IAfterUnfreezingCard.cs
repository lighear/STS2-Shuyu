using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Interfaces
{
    public interface IAfterUnfreezingCard
    {
        public Task AfterUnfreezingCard(CardModel card);
    }
}
