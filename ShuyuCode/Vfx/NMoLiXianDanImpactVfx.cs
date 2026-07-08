using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx
{
	public partial class NMoLiXianDanImpactVfx : Node2D
	{
		public static readonly string scenePath = $"{Entry.ResPath}/scenes/vfx/vfx_MoLiXianDan_impact.tscn";

        [Export(PropertyHint.None, "")]
        private Array<GpuParticles2D> _impactParticles = new Array<GpuParticles2D>();

        private CancellationTokenSource? _cts;

        public static NMoLiXianDanImpactVfx? Create(Creature owner, Creature? target)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(owner);
			if (nCreature == null)
			{
				return null;
			}
			NCreature? nCreature2 = NCombatRoom.Instance?.GetCreatureNode(target);
			if (nCreature2 == null)
			{
				return null;
			}
			return Create(nCreature.VfxSpawnPosition, nCreature2.VfxSpawnPosition);
		}

		public static NMoLiXianDanImpactVfx? Create(Vector2 throwerCenterPosition, Vector2 targetCenterPosition)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
            NMoLiXianDanImpactVfx nMoLiXianDanImpactVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NMoLiXianDanImpactVfx>(PackedScene.GenEditState.Disabled);
			nMoLiXianDanImpactVfx.GlobalPosition = targetCenterPosition;
			nMoLiXianDanImpactVfx.ApplyRotation(throwerCenterPosition, targetCenterPosition);
			return nMoLiXianDanImpactVfx;
		}

		public void ApplyRotation(Vector2 throwerPosition, Vector2 targetPosition)
		{
			Vector2 vector = targetPosition - throwerPosition;
			float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(vector.Y, vector.X));
			base.RotationDegrees = rotationDegrees;
		}

		public override void _Ready()
		{
			TaskHelper.RunSafely(PlaySequence());
		}

		public override void _ExitTree()
		{
			_cts?.Cancel();
		}

		private async Task PlaySequence()
		{
			_cts = new CancellationTokenSource();
            await Cmd.Wait(0.15f, _cts.Token);
            for (int i = 0; i < _impactParticles.Count; i++)
            {
                _impactParticles[i].Restart();
            }            
			await Cmd.Wait(2f, _cts.Token);
			this.QueueFreeSafely();
		}
	}
}
