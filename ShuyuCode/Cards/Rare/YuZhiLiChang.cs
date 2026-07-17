using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class YuZhiLiChang : ModCardTemplate
    {
        public YuZhiLiChang() : base(
            baseCost: 3,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceThornsPower>(),
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceThornsPower>(CurrentPowerAmount),
            new PowerVar<IceShieldPower>(CurrentPowerAmount),
            new DynamicVar("Increase", 1)
        ];

        private bool firstTurnAutoPlay;
        private int _currentPowerAmount = 5;
        private int _increasedPowerAmount;

        [SavedProperty]
        public int CurrentPowerAmount
        {
            get
            {
                return _currentPowerAmount;
            }
            set
            {
                AssertMutable();
                _currentPowerAmount = value;
                DynamicVars["IceThornsPower"].BaseValue = _currentPowerAmount;
                DynamicVars["IceShieldPower"].BaseValue = _currentPowerAmount;
            }
        }

        [SavedProperty]
        public int IncreasedPowerAmount
        {
            get
            {
                return _increasedPowerAmount;
            }
            set
            {
                AssertMutable();
                _increasedPowerAmount = value;
            }
        }

        public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
        {
            if (player == Owner && player.PlayerCombatState!.TurnNumber <= 1)
            {
                PileType.Draw.GetPile(player).MoveToBottomInternal(this);
            }
        }

        public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
        {
            if (player == Owner && player.PlayerCombatState!.TurnNumber <= 1)
            {
                firstTurnAutoPlay = true;
                await PlayerCmd.LoseEnergy(1, Owner);
                await CardCmd.AutoPlay(choiceContext, this, null);
                firstTurnAutoPlay = false;
            }
        }

        protected override CardLocation GetResultLocationForCardPlay()
        {
            CardLocation resultLocationForCardPlay = base.GetResultLocationForCardPlay();
            if (firstTurnAutoPlay)
            {
                resultLocationForCardPlay.pileType = PileType.Draw;
                resultLocationForCardPlay.position = CardPilePosition.Random;
            }
            return resultLocationForCardPlay;
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, DynamicVars["IceThornsPower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);

            if (!cardPlay.IsAutoPlay)
            {
                int intValue = DynamicVars["Increase"].IntValue;
                BuffFromManualPlay(intValue);
                (DeckVersion as YuZhiLiChang)?.BuffFromManualPlay(intValue);
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }

        protected override void AfterDowngraded()
        {
            UpdateAmount();
        }

        private void BuffFromManualPlay(int extraAmount)
        {
            IncreasedPowerAmount += extraAmount;
            UpdateAmount();
        }

        private void UpdateAmount()
        {
            CurrentPowerAmount = 5 + IncreasedPowerAmount;
        }
    }
}