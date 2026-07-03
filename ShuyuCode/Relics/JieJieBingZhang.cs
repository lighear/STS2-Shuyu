using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Relics;

[RegisterRelic(typeof(ShuyuRelicPool))]
public sealed class JieJieBingZhang : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2, ValueProp.Unpowered)
    ];

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature))
        {
            decimal damage = PileType.Hand.GetPile(Owner).Cards.Count * DynamicVars.Damage.BaseValue;
            if (damage > 0)
            {
                Flash();
                await CreatureCmd.Damage(choiceContext, Owner.Creature.CombatState!.HittableEnemies, damage, ValueProp.Unpowered, Owner.Creature);
            }
        }
    }
}
