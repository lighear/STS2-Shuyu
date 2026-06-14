using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Characters;
using Shuyu.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Relics;

// RegisterRelic 会把遗物注册进指定遗物池。
// RegisterCharacterStarterRelic 会把它作为 ShuyuCharacter 的初始遗物。
[RegisterRelic(typeof(ShuyuRelicPool))]
[RegisterCharacterStarterRelic(typeof(ShuyuCharacter))]
public sealed class BingLengZhiHui : ModRelicTemplate
{
    // 稀有度。
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 遗物的数值。这里会替换本地化中的 {Cards}。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new CardsVar("FrozenAmount", 1)
    ];

    // 图片资源统一放在 AssetProfile 里配置。
    // 三个路径可以先指向同一张图。后续有高清图或轮廓图时再拆开。
    public override RelicAssetProfile AssetProfile => new(
        // 小图标（原版 85x85）。
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        // 轮廓图标（原版 85x85）。
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        // 大图标（原版 256x256）。
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner)
        {
            return count;
        }
        if (Owner.PlayerCombatState!.TurnNumber > 1)
        {
            return count;
        }
        return count + DynamicVars.Cards.BaseValue;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        if (Owner.PlayerCombatState!.TurnNumber > 1)
        {
            return;
        }
        await ShuyuMechanismCmd.ChooseFromHandAndFreeze(choiceContext, Owner, DynamicVars["FrozenAmount"].IntValue, this);
    }
}
