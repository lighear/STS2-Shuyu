using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Interfaces
{
    public interface IAfterFreezingCard
    {
        public Task AfterFreezingCard(CardModel card);
    }
}
