using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(StatusCardPool))]
    public class EYun : ModCardTemplate
    {
        public EYun() : base(
            baseCost: 2,
            CardType.Status,
            CardRarity.Status,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override int MaxUpgradeLevel => 0;
        public override bool HasTurnEndInHandEffect => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new HpLossVar(6)
        ];

        protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
        {
            await CreatureCmdCompat.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, null);
        }

        public override void AfterCreated()
        {
            base.AfterCreated();
            if (Owner.Relics.Any(r => r is XueTianWaWa))
            {
                EnergyCost.AddThisCombat(-1);
                AddKeyword(CardKeyword.Exhaust);
            }
        }

        public static async Task CreateInDrawPile(Player owner, int amount, ICombatState combatState)
        {
            List<EYun> list = new List<EYun>();
            for (int i = 0; i < amount; i++)
            {
                var card = combatState.CreateCard<EYun>(owner);
                list.Add(card);
            }
            var result = await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Draw, owner, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}
