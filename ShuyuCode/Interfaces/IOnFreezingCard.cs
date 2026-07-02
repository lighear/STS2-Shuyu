using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Interfaces
{
    public interface IOnFreezingCard
    {
        // 返回false表示解除card的封冻
        public Task<bool> OnFreezingCard(CardModel card);
    }
}
