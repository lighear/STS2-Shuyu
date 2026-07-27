using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Powers;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(TokenCardPool))]
    public class BingZhen : ModCardTemplate
    {
        public BingZhen() : base(
            baseCost: 0,
            CardType.Attack,
            CardRarity.Token,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(1, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .WithAttackerAnim(null, 0f)
                .Targeting(cardPlay.Target!)
                .WithHitVfxNode((Creature target) => NBingZhenVfx.Create(Owner.Creature, target))
                .Execute(choiceContext);

            await PowerCmd.Apply<ChillPower>(choiceContext, cardPlay.Target!, 1, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(1);
            AddKeyword(CardKeyword.Retain);
        }

        private static IEnumerable<BingZhen> Create(Player owner, int amount, ICombatState combatState, bool upgrade)
        {
            List<BingZhen> list = new List<BingZhen>();
            for (int i = 0; i < amount; i++)
            {
                var card = combatState.CreateCard<BingZhen>(owner);
                if (upgrade)
                {
                    CardCmd.Upgrade(card);
                }
                list.Add(card);
            }
            return list;
        }

        public static async Task CreateInHand(Player owner, int amount, ICombatState combatState, bool upgrade)
        {
            if (CombatManager.Instance.IsOverOrEnding)
            {
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                var card = BingZhen.Create(owner, 1, combatState, upgrade);
                await CardPileCmd.AddGeneratedCardsToCombat(card, PileType.Hand, owner);
                await Cmd.Wait(0.1f);
            }
        }
    }
}
