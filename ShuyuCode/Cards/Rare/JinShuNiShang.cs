using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JinShuNiShang : ModCardTemplate
    {
        public JinShuNiShang() : base(
            baseCost: 3,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromCard<EYun>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Ethereal
        ];

        protected override HashSet<CardTag> CanonicalTags => [
            ShuyuCardTags.Taboo
        ];

        private int _hpBeforeCombatStart;
        private int HpBeforeCombatStart
        {
            get
            {
                if (_hpBeforeCombatStart > 0)
                {
                    return _hpBeforeCombatStart;
                }
                return Owner.Creature.CurrentHp +
                    CombatManager.Instance.History.Entries
                        .OfType<DamageReceivedEntry>()
                        .Where(entry => entry.Receiver == Owner.Creature)
                        .Sum(entry => entry.Result.UnblockedDamage);
            }
            set
            {
                _hpBeforeCombatStart = value;
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            NJinShuNiShangEdgeVfx.Play();
            await NJinShuNiShangFogRingVfx.PlayOpening(Owner.Creature);

            int hp = Owner.Creature.CurrentHp;
            int hpBeforeCombatStart = HpBeforeCombatStart;
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);
            if (hp < hpBeforeCombatStart)
            {
                await CreatureCmd.Heal(Owner.Creature, hpBeforeCombatStart - hp);
            }
            await EYun.CreateInDrawPile(Owner, 2, CombatState!);
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }

        public override async Task BeforeCombatStart()
        {
            HpBeforeCombatStart = Owner.Creature.CurrentHp;
        }
    }
}
