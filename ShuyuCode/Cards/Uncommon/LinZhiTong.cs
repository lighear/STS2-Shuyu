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
    public class LinZhiTong : ModCardTemplate
    {
        public LinZhiTong() : base(
            baseCost: 2,
            CardType.Attack,
            CardRarity.Uncommon,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(12, ValueProp.Move),
            new DynamicVar("EnemyStrengthLoss", 3)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            Node2D vfxNode = VFXUtil.GenVFXNode($"{VFXUtil.CardVfxPath}/vfx_LinZhiTong.tscn");
            Node obj = ((Node)vfxNode).FindChild("StartPos", true, true);
            Node2D startNode = (Node2D)(object)((obj is Node2D) ? obj : null);
            
            Vector2 startPos = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.VfxSpawnPosition ?? Vector2.Zero;
            startPos.X += 128;
            Vector2 targetPos = NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target)?.VfxSpawnPosition ?? Vector2.Zero;
            vfxNode.FitVFX(startNode.GlobalPosition, Vector2.Zero, startPos, targetPos);
            vfxNode.GlobalPosition = targetPos;
            
            NCombatRoom instance = NCombatRoom.Instance;
            if (instance != null)
            {
                GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)vfxNode);
                SceneTreeTimer timer = vfxNode.GetTree().CreateTimer(2f);
                timer.Timeout += () => {
                    if (GodotObject.IsInstanceValid(vfxNode)) {
                        vfxNode.QueueFreeSafely();
                    }
                };
            }
            
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
                .Execute(choiceContext);
            await PowerCmd.Apply<LinZhiTongPower>(choiceContext, cardPlay.Target!, DynamicVars["EnemyStrengthLoss"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["EnemyStrengthLoss"].UpgradeValueBy(1);
        }
    }
}
