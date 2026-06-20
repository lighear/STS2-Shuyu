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
	public partial class NDuanXueVfx : Node2D
	{
		public static readonly string scenePath = $"{Entry.ResPath}/scenes/vfx/vfx_DuanXue.tscn";

		[Export(PropertyHint.None, "")]
		private GpuParticles2D? _beamParticle;

        [Export(PropertyHint.None, "")]
        private GpuParticles2D? _endParticle;

        [Export(PropertyHint.None, "")]
		private Array<GpuParticles2D> _endImpacts = new Array<GpuParticles2D>();

        private Array<GpuParticles2D> _beamParticles = new Array<GpuParticles2D>();

        private CancellationTokenSource? _cts;

		public static NDuanXueVfx? Create(Creature? target, int hitCount)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(target);
			if (nCreature == null)
			{
				return null;
			}
			return Create(nCreature.VfxSpawnPosition, hitCount);
		}

		public static NDuanXueVfx? Create(Vector2 targetCenterPosition, int hitCount)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NDuanXueVfx nDuanXueVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NDuanXueVfx>(PackedScene.GenEditState.Disabled);
			nDuanXueVfx.GlobalPosition = targetCenterPosition;

			if (nDuanXueVfx._beamParticle == null || nDuanXueVfx._endParticle == null)
			{
				return null;
			}
			for (int i = 0; i < hitCount; i++)
			{
				GpuParticles2D copy = (GpuParticles2D)nDuanXueVfx._beamParticle.Duplicate();
                if (copy.Material != null)
                {
                    copy.Material = (Material)copy.Material.Duplicate();
                }
                nDuanXueVfx.AddChild(copy);
				nDuanXueVfx._beamParticles.Add(copy);
			}

			nDuanXueVfx._endParticle.Amount = hitCount * 10;
			return nDuanXueVfx;
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

            for (int i = 0; i < _beamParticles.Count; i++)
            {
                _beamParticles[i].Restart();

                ShaderMaterial? mat = _beamParticles[i].Material as ShaderMaterial;
                if (mat != null)
                {
                    Tween tween = CreateTween();
					tween.TweenMethod(Callable.From<float>(val => mat.SetShaderParameter("showLength", val)), 0f, 2f, 0.15f)
						.SetEase(Tween.EaseType.Out);
                }

                await Cmd.Wait(0.15f, _cts.Token);
				_beamParticles[i].SpeedScale = 0;
            }

            for (int i = 0; i < _beamParticles.Count; i++)
            {
                _beamParticles[i].SpeedScale = 1;
                ShaderMaterial? mat = _beamParticles[i].Material as ShaderMaterial;
                if (mat != null)
                {
                    Tween tween = CreateTween();
                    tween.TweenMethod(Callable.From<float>(val => mat.SetShaderParameter("whiteWeight", val)), 0f, 1f, 0.5f)
                        .SetEase(Tween.EaseType.In)
						.SetTrans(Tween.TransitionType.Quad);
                }
            }
            await Cmd.Wait(0.5f, _cts.Token);

            for (int i = 0; i < _endImpacts.Count; i++)
            {
                _endImpacts[i].Restart();
            }
            _endParticle?.Restart();

            NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
            await Cmd.Wait(2f, _cts.Token);
			this.QueueFreeSafely();
		}
	}
}
