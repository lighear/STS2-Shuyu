using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Relics;

[RegisterRelic(typeof(ShuyuRelicPool))]
public sealed class ZhaRenSuiPian : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<IceThornsPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Unpowered),
        new PowerVar<IceThornsPower>(1)
    ];
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner.Creature))
        {
            decimal num = Owner.Creature.Block / DynamicVars.Block.BaseValue * DynamicVars["IceThornsPower"].BaseValue;
            await PowerCmd.Apply<IceThornsPower>(choiceContext, base.Owner.Creature, num, base.Owner.Creature, null);
        }
    }
}