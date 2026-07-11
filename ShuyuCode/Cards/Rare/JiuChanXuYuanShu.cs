using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JiuChanXuYuanShu : ModCardTemplate
    {
        public interface IChoosable
        {
            Task OnChosen(JiuChanXuYuanShu source);
        }
        
        public JiuChanXuYuanShu() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.None)
        { }

        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Innate,
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DynamicVar("CanUseTimes", 3)
        ];

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player == Owner && Owner.PlayerCombatState!.TurnNumber <= 1 && !IsClone)
            {
                foreach (Creature ally in CombatState!.GetTeammatesOf(Owner.Creature)
                    .Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature))
                {
                    CardModel card = CreateCloneForPlayer(ally.Player!);
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
                }
            }
        }
        
        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromCard<WishStrength>(),
            HoverTipFactory.FromCard<WishShield>(),
            HoverTipFactory.FromCard<WishGold>()
        ];
        
        private static readonly LocString _curseOfKnowledgeDoneLine = MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.doneLine");
        private static readonly LocString _curseOfKnowledgeStartLine = MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.startLine");
        
        private decimal _upgradeTimesStrength;
        private decimal _upgradeTimesShield;
        private decimal _upgradeTimesGold;
        private decimal _usedTimes;

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TalkCmd.Play(_curseOfKnowledgeStartLine, Owner.Creature, VfxColor.Gold, VfxDuration.Standard);
            
            WishStrength cardStrength = CombatState!.CreateCard<WishStrength>(Owner);
            WishShield cardShield = CombatState!.CreateCard<WishShield>(Owner);
            WishGold cardGold = CombatState!.CreateCard<WishGold>(Owner);

            for (int i = 0; i < _upgradeTimesStrength; i++) CardCmd.Upgrade(cardStrength);
            for (int i = 0; i < _upgradeTimesShield; i++) CardCmd.Upgrade(cardShield);
            for (int i = 0; i < _upgradeTimesGold; i++) CardCmd.Upgrade(cardGold);

            List<CardModel> wishes = new List<CardModel>{ cardStrength, cardShield, cardGold };
            
            CardModel cardModel = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), wishes, Owner);
            if (cardModel != null)
            {
                await ((IChoosable)cardModel).OnChosen(this);
            }
            
            foreach (Creature ally in CombatState!.GetTeammatesOf(Owner.Creature)
                    .Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature))
            {
                IEnumerable<JiuChanXuYuanShu> cards = ally.Player!.PlayerCombatState!.AllCards.OfType<JiuChanXuYuanShu>()
                    .Where(c =>
                    {
                        CardPile? pile = c.Pile;
                        return pile == null || pile.Type != PileType.Hand;
                    });
                await CardPileCmd.Add(cards, PileType.Hand);
            }

            TalkCmd.Play(_curseOfKnowledgeDoneLine, Owner.Creature, VfxColor.Gold, VfxDuration.Standard);

            _usedTimes ++;
            DynamicVars["CanUseTimes"].BaseValue--;

            if (DynamicVars["CanUseTimes"].BaseValue <= 0)
            {
                await EYun.CreateInDrawPile(Owner, 1, CombatState!);
                await CardPileCmd.RemoveFromCombat(this);
            }
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Retain);
        }

        public void UpgradeStrength()
        {
            _upgradeTimesStrength++;
        }
        
        public void UpgradeShield()
        {
            _upgradeTimesShield++;
        }
        
        public void UpgradeGold()
        {
            _upgradeTimesGold++;
        }

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars["CanUseTimes"].BaseValue -= _usedTimes;
        }
    }
}
