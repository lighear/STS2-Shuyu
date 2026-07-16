using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JinShuFengYin : ModCardTemplate
    {
        public JinShuFengYin() : base(
            baseCost: 3,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>(),
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<VulnerablePower>(),
            HoverTipFactory.FromCard<EYun>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Ethereal,
            CardKeyword.Exhaust
        ];

        protected override HashSet<CardTag> CanonicalTags => [
            ShuyuCardTags.Taboo
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DynamicVar("EnemyStrengthLoss", 24),
            new PowerVar<WeakPower>(5),
            new PowerVar<VulnerablePower>(5)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            Node2D? vfxNode = VFXUtil.PlaySimple(SceneHelper.GetScenePath("vfx/vfx_chain"), NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target)?.VfxSpawnPosition ?? Vector2.Zero, 2f);
            if (vfxNode != null)
            {
                vfxNode.Modulate = new Color("#00a2ff");
            }
            
            await PowerCmd.Apply<ChillPower>(choiceContext, cardPlay.Target!, 1, Owner.Creature, this);
            await PowerCmd.Apply<JinShuFengYinStrengthDownPower>(choiceContext, cardPlay.Target!, DynamicVars["EnemyStrengthLoss"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target!, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
            await EYun.CreateInDrawPile(Owner, 1, CombatState!);
        }

        protected override void OnUpgrade()
        {
            RemoveKeyword(CardKeyword.Ethereal);
            AddKeyword(CardKeyword.Retain);
        }
    }
}