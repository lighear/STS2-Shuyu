using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class ChillPower : ModPowerTemplate
{
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Debuff;
    // 叠加类型，Counter表示可叠加，Single表示不可叠加
    public override PowerStackType StackType => PowerStackType.Single;

    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner.Player)
        {
            await CreatureCmd.Damage(choiceContext, base.Owner, 6, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount != 0 && power.GetTypeForAmount(amount) == PowerType.Debuff && power.Owner == base.Owner && power is not ITemporaryPower)
        {
            Flash();
            if (power is ChillPower)
            {
                await CreatureCmd.Damage(choiceContext, base.Owner, 12, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            }
            else
            {
                await CreatureCmd.Damage(choiceContext, base.Owner, 6, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            }
        }
    }
}