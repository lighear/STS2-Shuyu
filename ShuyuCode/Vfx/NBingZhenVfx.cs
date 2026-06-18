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
	public partial class NBingZhenVfx : Node2D
	{
		private static readonly StringName _color = new StringName("color");

		public static readonly string scenePath = $"{Entry.ResPath}/scenes/vfx/vfx_BingZhen/vfx_BingZhen.tscn";

		[Export(PropertyHint.None, "")]
		private Array<GpuParticles2D> _throwParticles = new Array<GpuParticles2D>();

		[Export(PropertyHint.None, "")]
		private Array<GpuParticles2D> _impactParticles = new Array<GpuParticles2D>();

		[Export(PropertyHint.None, "")]
		private Array<GpuParticles2D> _modulateParticles = new Array<GpuParticles2D>();

		private CancellationTokenSource? _cts;

		public static NBingZhenVfx? Create(Creature owner, Creature? target, Color tint)
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
			return Create(nCreature.VfxSpawnPosition, nCreature2.VfxSpawnPosition, tint);
		}

		public static NBingZhenVfx? Create(Vector2 throwerCenterPosition, Vector2 targetCenterPosition, Color tint)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NBingZhenVfx nBingZhenVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NBingZhenVfx>(PackedScene.GenEditState.Disabled);
			nBingZhenVfx.GlobalPosition = targetCenterPosition;
			nBingZhenVfx.ApplyRotation(throwerCenterPosition, targetCenterPosition);
			nBingZhenVfx.ApplyTint(tint);
			return nBingZhenVfx;
		}

		public void ApplyTint(Color tint)
		{
			for (int i = 0; i < _modulateParticles.Count; i++)
			{
				_modulateParticles[i].ProcessMaterial = (ParticleProcessMaterial)_modulateParticles[i].ProcessMaterial.Duplicate();
				//_modulateParticles[i].ProcessMaterial.Set(_color, tint);
			}
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
			for (int i = 0; i < _throwParticles.Count; i++)
			{
				_throwParticles[i].Restart();
			}
			await Cmd.Wait(0.5f, _cts.Token);
			for (int j = 0; j < _impactParticles.Count; j++)
			{
				_impactParticles[j].Restart();
			}
			NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
			await Cmd.Wait(2f, _cts.Token);
			this.QueueFreeSafely();
		}
	}
}
