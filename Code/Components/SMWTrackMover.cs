using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Entities;

public class SMWTrackMover : Component
{
	public Behaviour MoveBehaviour;
	public Facings Direction; // Right = move forward, Left = move back
	public float Gravity = 200f;
	public float FallSpeed = 200f;
	// Linear move behaviour options
	public float MoveSpeed = 100f;
	// Easing move behaviour options
	public Ease.Easer Easer = Ease.SineInOut;
	public float EaseDuration = 2f;
	public bool EaseTrackDir = false;

	public Func<Vector2> GetPosition;
	public Action<Vector2, Vector2> SetPosition;
	public Action<SMWTrackMover, bool> OnStop;

	public SMWTrack Track;
	public float Progress;
	public Vector2 Speed;
	public bool Activated = true;
	public bool StopAtNode;
	public bool StopAtEnd;
	public Facings StartDirection;

	private float easeTimer;
	private Vector2 lastMoveAngle;
	private Vector2 lastEasedMove;
	private SMWTrack.Section lastSection;
	private float lastSectionCooldown;

	public SMWTrackMover() : base(true, true) { }

	public SMWTrackMover(EntityData data) : this()
	{
		MoveBehaviour = data.Enum("moveBehaviour", Behaviour.Linear);
		Direction = data.Enum<Facings>("direction");
		Gravity = data.Float("gravity");
		FallSpeed = data.Float("fallSpeed");

		MoveSpeed = data.Float("moveSpeed");

		Easer = EeveeHelperModule.EaseTypes[data.Attr("easing", "SineInOut")];
		EaseDuration = data.Float("easeDuration", 2f);
		EaseTrackDir = data.Bool("easeTrackDirection");
	}

	public override void EntityAwake()
	{
		base.EntityAwake();
		foreach (SMWTrack track in Scene.Tracker.GetEntities<SMWTrack>())
		{
			var section = track.GetIntersect(GetAttachPos() - Vector2.UnitY * 2f, GetAttachPos() + Vector2.UnitY * 2f, out var intersect);
			if (section != null)
			{
				AttachTo(track, section, intersect);
				break;
			}
			section = track.GetIntersect(GetAttachPos() - Vector2.UnitX * 2f, GetAttachPos() + Vector2.UnitX * 2f, out intersect);
			if (section != null)
			{
				AttachTo(track, section, intersect);
				break;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (lastSectionCooldown > 0f)
		{
			lastSectionCooldown -= Engine.DeltaTime;
		}

		if (!Activated)
		{
			return;
		}

		if (Track != null)
		{
			switch (MoveBehaviour)
			{
				case Behaviour.Linear: MoveLinear(); break;
				case Behaviour.Easing: MoveEased(); break;
			}
		}
		else
		{
			if ((FallSpeed <= 0f && Speed.Y < FallSpeed) || (FallSpeed >= 0f && Speed.Y > FallSpeed))
			{
				Speed.Y = FallSpeed;
			}

			Speed.Y = Calc.Approach(Speed.Y, FallSpeed, Gravity * Engine.DeltaTime);

			foreach (SMWTrack track in Scene.Tracker.GetEntities<SMWTrack>())
			{
				var section = track.GetIntersect(GetAttachPos(), GetAttachPos() + Speed * Engine.DeltaTime, out var intersect, parallelTolerance: 0.1f);
				if (section != null && (section != lastSection || lastSectionCooldown <= 0f))
				{
					AttachTo(track, section, intersect);
					return;
				}
			}

			MoveTo(GetAttachPos() + Speed * Engine.DeltaTime, Speed);
		}
	}

	private void MoveLinear()
	{
		var move = MoveSpeed * Engine.DeltaTime * (Direction == Facings.Left ? -1f : 1f);
		SetProgress(Progress + move);
	}

	private void MoveEased()
	{
		easeTimer += Engine.DeltaTime * ((Direction == Facings.Left && EaseTrackDir) ? -1f : 1f);

		var progressStart = 0f;
		var progressLength = Track.Length;

		// If the mover stops at each node, ease between individual nodes
		if (StopAtNode)
		{
			progressStart = lastSection.Offset;
			progressLength = lastSection.Length;
		}

		float newProgress;
		if (easeTimer >= EaseDuration)
		{
			easeTimer = EaseDuration;
			newProgress = progressLength + 0.01f;
		}
		else if (easeTimer <= 0f)
		{
			easeTimer = 0f;
			newProgress = -0.01f;
		}
		else
		{
			newProgress = Easer(easeTimer / EaseDuration) * progressLength;
		}

		// If the ease is opposite of the track direction (only when "Ease Track Dir" is disabled), reverse the progress
		if (!EaseTrackDir && Direction == Facings.Left)
		{
			newProgress = progressLength - newProgress;
		}

		newProgress += progressStart;

		lastEasedMove = GetEasedMove();

		SetProgress(newProgress);
	}

	private bool TryFall(float progress)
	{
		var newProgress = progress;

		if (Track.Enabled)
		{
			if (progress < 0f && Track.StartOpen)
			{
				lastSection = Track.Sections.First();
				lastMoveAngle = lastSection.GetAngle(0f) * -1f;
				MoveTo(lastSection.Start);
			}
			else if (progress >= Track.Length && Track.EndOpen)
			{
				lastSection = Track.Sections.Last();
				lastMoveAngle = lastSection.GetAngle(lastSection.Length);
				MoveTo(lastSection.End);
				newProgress -= Track.Length;
			}
			else
			{
				return false;
			}
		}
		else
		{
			newProgress = 0;
		}

		if (MoveBehaviour == Behaviour.Linear)
		{
			Speed = lastMoveAngle * MoveSpeed;
		}
		else if (MoveBehaviour == Behaviour.Easing)
		{
			Speed = lastEasedMove;
		}

		if (Speed.X < 0)
		{
			Direction = Facings.Left;
		}
		else if (Speed.X > 0)
		{
			Direction = Facings.Right;
		}

		Progress = newProgress;
		Track = null;
		lastSectionCooldown = 0.3f;

		return true;
	}

	private Vector2 GetEasedMove()
	{
		// Keep progress to 0.01 less than track length since track behavior is exclusive of the full length
		var trackStart = 0f;
		var trackLength = Track.Length - 0.01f;

		// If the mover stops at each node, ease between individual nodes
		if (StopAtNode)
		{
			trackStart = lastSection.Offset;
			trackLength = lastSection.Length - 0.01f;
		}

		// Calculate speed using a short segment of the track
		var easeStart = Calc.Clamp(easeTimer + ((Direction == Facings.Left && EaseTrackDir) ? 0.01f : -0.01f), 0f, EaseDuration);
		var easeEnd = easeTimer;

		var progressStart = Easer(easeStart / EaseDuration) * trackLength;
		var progressEnd = Easer(easeEnd / EaseDuration) * trackLength;

		// Reverse progress if the ease is opposite of the track direction (only when "Ease Track Dir" is disabled)
		if (Direction == Facings.Left && !EaseTrackDir)
		{
			progressStart = trackLength - progressStart;
			progressEnd = trackLength - progressEnd;
		}

		progressStart += trackStart;
		progressEnd += trackStart;

		var move = Track.GetPos(progressEnd) - Track.GetPos(progressStart);

		// Multiply by 100 to get the final speed, since we used a 0.01 second difference in easing
		return move * 100f;
	}

	private void SetProgress(float progress)
	{
		if (TryFall(progress))
		{
			// Try immediate reattach to track, allowing for separate tracks to be effectively connected
			foreach (SMWTrack track in Scene.Tracker.GetEntities<SMWTrack>())
			{
				var section = track.TryQuickAttach(GetAttachPos(), out var hit, ignore: lastSectionCooldown <= 0f ? null : lastSection);
				if (section != null)
				{
					var progressAdd = Math.Abs(Progress);
					AttachTo(track, section, hit);
					if (StopAtNode)
					{
						Activated = false;
						OnStop?.Invoke(this, false);
					}
					else if (MoveBehaviour == Behaviour.Linear)
					{
						SetProgress(Progress + progressAdd * (Direction == Facings.Left ? -1f : 1f));
					}
					break;
				}
			}
			return;
		}

		if (Track.Length > 0f)
		{
			// Check if we're passing a node for the Stop At Node option
			if (StopAtNode && lastSection != null)
			{
				var sectionStart = lastSection.Offset;
				var sectionEnd = lastSection.Offset + lastSection.Length;

				if (progress < sectionStart)
				{
					var newPos = lastSection.Start;

					Progress = Math.Max(sectionStart - 0.01f, 0f);
					lastSection = Track.GetSection(Progress);
					lastMoveAngle = lastSection.GetAngle(Progress) * (int)Direction;
					MoveTo(newPos);

					if (progress < 0)
					{
						Flip();
					}

					ResetEaseTimer();

					Activated = false;
					OnStop?.Invoke(this, progress < 0);
					return;

				}
				else if (progress >= sectionEnd)
				{
					var newPos = lastSection.End;

					// The following is sometimes false:
					//   var test = section.Offset + section.Length;
					//   return test >= section.Offset + section.Length;
					// I hate floating points, add 0.01 to make sure we're on the next section
					Progress = Math.Min(sectionEnd + 0.01f, Track.Length - 0.01f);
					lastSection = Track.GetSection(Progress);
					lastMoveAngle = lastSection.GetAngle(Progress) * (int)Direction;
					MoveTo(newPos);

					if (progress >= Track.Length)
					{
						Flip();
					}

					ResetEaseTimer();

					Activated = false;
					OnStop?.Invoke(this, progress >= Track.Length);
					return;
				}
			}

			if (progress < 0f || progress >= Track.Length)
			{
				// Whether the movement past the end of the track should be used backwards; otherwise, snap to the end
				var keepGoing = MoveBehaviour == Behaviour.Linear && !StopAtEnd && !StopAtNode;

				var endDirection = Direction;

				if (progress < 0f)
				{
					progress = keepGoing ? -progress : 0f;
				}
				else if (progress >= Track.Length)
				{
					progress = Track.Length - (keepGoing ? (progress - Track.Length) : 0f);
				}
				progress = Calc.Clamp(progress, 0f, Track.Length - 0.01f); // Avoid infinite loop

				// Make sure the direction at the time of SetProgress is the accurate movement direction
				// This means that we only flip the direction *after* SetProgress if movement snaps to the end here
				if (keepGoing)
				{
					Flip();
					SetProgress(progress);
				}
				else
				{
					SetProgress(progress);
					Flip();
				}

				ResetEaseTimer();

				if (StopAtNode || StopAtEnd)
				{
					Activated = false;
					OnStop?.Invoke(this, true);
				}

				return;
			}

			var section = Track.GetSection(progress);
			lastSection = section;
			lastMoveAngle = section.GetAngle(progress) * (int)Direction;
			MoveTo(section.GetPos(progress - section.Offset));
			Progress = progress;
		}
		else
		{
			lastSection = null;
			lastMoveAngle = Vector2.Zero;
			MoveTo(Track.Position);
			Progress = 0f;
		}
	}

	private void AttachTo(SMWTrack track, SMWTrack.Section section, Vector2 hit)
	{
		var progress = section.GetProgress(hit);
		MoveTo(section.GetPos(progress));

		Track = track;
		Progress = Calc.Clamp(progress + section.Offset, 0f, track.Length - 0.01f);
		lastSection = Track.GetSection(Progress);

		if (Progress <= 0.0001f)
		{
			// If the mover is at the start of the track, set the direction to the track's direction
			Direction = Facings.Right;
		}
		else if (Progress >= Track.Length - 0.0101f)
		{
			// If the mover is at the end of the track, set the direction to the opposite of the track's direction
			Direction = Facings.Left;
		}
		else if (section.End.X < section.Start.X)
		{
			// Flip direction if track direction is left to maintain movement direction (Right = move forward, Left = move back)
			Flip();
		}
		else if (section.Start.X == section.End.X && Speed != Vector2.Zero)
		{
			// If track is vertical and we're moving, always move in the direction we fall
			Direction = section.End.Y < section.Start.Y ? Facings.Left : Facings.Right;
			if (Gravity < 0f)
			{
				Flip();
			}
		}

		StartDirection = Direction;

		if (MoveBehaviour == Behaviour.Easing)
		{
			var easeProgress = Progress / Track.Length;

			if (StopAtNode)
			{
				easeProgress = (Progress - lastSection.Offset) / lastSection.Length;
			}

			if (Direction == Facings.Left && !EaseTrackDir)
			{
				easeProgress = 1f - easeProgress;
			}

			var reverseEaser = ReverseEase.GetReverse(Easer);
			easeTimer = reverseEaser(easeProgress) * EaseDuration;

			lastEasedMove = GetEasedMove();
		}

		Speed = Vector2.Zero;
	}

	private void Flip()
	{
		Direction = (Direction == Facings.Left) ? Facings.Right : Facings.Left;
	}

	private void ResetEaseTimer()
	{
		easeTimer = (Direction == Facings.Left && EaseTrackDir) ? EaseDuration : 0f;
	}

	private Vector2 GetAttachPos()
	{
		return GetPosition?.Invoke() ?? Entity.Center;
	}

	public Vector2 GetLiftSpeed(Vector2 pos)
	{
		switch (MoveBehaviour)
		{
			case Behaviour.Linear:
				return lastMoveAngle * MoveSpeed;
			case Behaviour.Easing:
				return Track != null ? lastEasedMove : Speed;
			default:
				return Vector2.Zero;
		}
	}

	private void MoveTo(Vector2 pos, Vector2? liftSpeed = null)
	{
		if (SetPosition != null)
		{
			SetPosition(pos, liftSpeed ?? GetLiftSpeed(pos));
		}
		else
		{
			Entity.Center = pos;
		}
	}

	public enum Behaviour
	{
		Linear,
		Easing
	}
}
