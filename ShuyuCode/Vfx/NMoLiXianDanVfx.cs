using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx
{
	public partial class NMoLiXianDanVfx : Node2D
	{
		public static readonly string scenePath = $"{Entry.ResPath}/scenes/vfx/vfx_MoLiXianDan.tscn";

		[Export(PropertyHint.None, "")]
		private Array<GpuParticles2D> _firstBulletParticles = new Array<GpuParticles2D>();

		[Export(PropertyHint.None, "")]
		private Array<GpuParticles2D> _secondBulletParticles = new Array<GpuParticles2D>();

		[Export(PropertyHint.None, "")]
		private Array<GpuParticles2D> _impactParticles = new Array<GpuParticles2D>();

		[Export(PropertyHint.None, "")]
		private Node2D _throwContainer;

		private CancellationTokenSource? _cts;

		public static NMoLiXianDanVfx? Create(Creature owner, Creature? target)
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

		public static NMoLiXianDanVfx? Create(Vector2 throwerCenterPosition, Vector2 targetCenterPosition)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NMoLiXianDanVfx nMoLiXianDanVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NMoLiXianDanVfx>(PackedScene.GenEditState.Disabled);
			nMoLiXianDanVfx.GlobalPosition = targetCenterPosition;
			nMoLiXianDanVfx.ApplyRotation(throwerCenterPosition, targetCenterPosition);
			nMoLiXianDanVfx.ApplyDistance(throwerCenterPosition, targetCenterPosition);
			return nMoLiXianDanVfx;
		}

		public void ApplyRotation(Vector2 throwerPosition, Vector2 targetPosition)
		{
			Vector2 vector = targetPosition - throwerPosition;
			float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(vector.Y, vector.X));
			base.RotationDegrees = rotationDegrees;
		}

		public void ApplyDistance(Vector2 throwerPosition, Vector2 targetPosition)
		{
			float distance = throwerPosition.DistanceTo(targetPosition);
			float targetX = -distance + 100;
			_throwContainer.Position = new Vector2(targetX, _throwContainer.Position.Y - 50);
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
			for (int i = 0; i < _firstBulletParticles.Count; i++)
			{
				_firstBulletParticles[i].Restart();
			}
			NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
			await Cmd.Wait(0.15f, _cts.Token);
			for (int i = 0; i < _impactParticles.Count; i++)
			{
				_impactParticles[i].Restart();
			}

			await Cmd.Wait(0.2f, _cts.Token);
			for (int i = 0; i < _secondBulletParticles.Count; i++)
			{
				_secondBulletParticles[i].Restart();
			}
			NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Normal);
			await Cmd.Wait(2f, _cts.Token);
			this.QueueFreeSafely();
		}
	}
}
