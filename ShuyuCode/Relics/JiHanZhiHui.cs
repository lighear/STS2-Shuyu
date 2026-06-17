using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shuyu.Characters;
using Shuyu.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Relics;

[RegisterRelic(typeof(ShuyuRelicPool))]
public sealed class JiHanZhiHui : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3),
        new CardsVar("FrozenAmount", 2)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player == Owner && Owner.PlayerCombatState!.TurnNumber <= 1)
        {
            return count + DynamicVars.Cards.BaseValue;
        }
        return count;
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && Owner.PlayerCombatState!.TurnNumber <= 1)
        {
            await ShuyuMechanismCmd.ChooseFromHandAndFreeze(choiceContext, Owner, DynamicVars["FrozenAmount"].IntValue, this);
        }
    }
}
