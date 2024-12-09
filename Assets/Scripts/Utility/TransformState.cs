using System;
using System.Collections;
using UnityEngine;

namespace KHI.Utility
{
    /// <summary>
    /// Captures the state of a Transform: its global position and rotation, plus local scale.
    /// This can be used to calculate or compare states without actually updating a GameObject's transform,
    /// which is particularly useful for transitioning between states. 
    /// </summary>
    /// <remarks>
    /// Similar to MRTK's MixedRealityTransform (MIT license).
    /// </remarks>
    [Serializable]
    public struct TransformState : IEqualityComparer
    {
        public TransformState(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.rotation;
            this.scale = transform.localScale;
        }

        public TransformState(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        /// <summary>
        /// Create a transform with only given position
        /// </summary>
        public static TransformState FromPosition(Vector3 position)
        {
            return new TransformState(position, Quaternion.identity, Vector3.one);
        }
        
        /// <summary>
        /// Create a transform with altered position
        /// </summary>
        public TransformState WithPosition(Vector3 newPosition)
        {
            return new TransformState(newPosition, Rotation, Scale);
        }

        /// <summary>
        /// Create a transform with only given rotation
        /// </summary>
        public static TransformState FromRotation(Quaternion rotation)
        {
            return new TransformState(Vector3.zero, rotation, Vector3.one);
        }
        
        /// <summary>
        /// Create a transform with altered rotation
        /// </summary>
        public TransformState WithRotation(Quaternion newRotation)
        {
            return new TransformState(Position, newRotation, Scale);
        }

        public TransformState WithRotationAround(Quaternion relativeRotation, Vector3 pivotPoint)
        {
            return new TransformState(
                relativeRotation * (position - pivotPoint) + pivotPoint,
                relativeRotation * rotation,
                scale);
        }

        /// <summary>
        /// Create a transform with only given scale
        /// </summary>
        public static TransformState FromScale(Vector3 scale)
        {
            return new TransformState(Vector3.zero, Quaternion.identity, scale);
        }
        
        /// <summary>
        /// Create a transform with altered scale
        /// </summary>
        public TransformState WithScale(Vector3 newScale)
        {
            return new TransformState(Position, Rotation, newScale);
        }

        /// <summary>
        /// Returns the velocity between two states in meters per second.
        /// </summary>
        public float CalculateVelocity(TransformState other, float time)
        {
            if (time > float.Epsilon)
                return Vector3.Distance(Position, other.Position) / time;

            throw new System.ArgumentException(
                $"{nameof(TransformState)}::{nameof(CalculateVelocity)} Time should be non-negative and larger than zero.");
        }

        /// <summary>
        /// Returns the angular velocity between two states in degrees per second.
        /// </summary>
        public float CalculateAngularVelocity(TransformState other, float time)
        {
            if (time > float.Epsilon)
                return Quaternion.Angle(Rotation, other.Rotation) / time;
            
            throw new System.ArgumentException(
                $"{nameof(TransformState)}::{nameof(CalculateAngularVelocity)} Time should be non-negative and larger than zero.");
        }

        /// <summary>
        /// An unscientific representation of positional or rotational change.
        /// (meters per second) + (degrees per second)
        /// </summary>
        public float CalculateJitterVelocity(TransformState other, float time)
        {
            if (time > float.Epsilon)
                return CalculateVelocity(other, time) + CalculateAngularVelocity(other, time);
            
            throw new System.ArgumentException(
                $"{nameof(TransformState)}::{nameof(CalculateJitterVelocity)} Time should be non-negative and larger than zero.");
        }

        /// <summary>
        /// The default value for a Six DoF Transform.
        /// </summary>
        public static TransformState Identity { get; } = new TransformState(Vector3.zero, Quaternion.identity, Vector3.one);

        [SerializeField] [Tooltip("The (assumed) global position of the transform.")]
        Vector3 position;

        [SerializeField] [Tooltip("The (assumed) global rotation of the transform.")]
        Quaternion rotation;
        
        [SerializeField] [Tooltip("The local scale of the transform.")]
        private Vector3 scale;

        /// <summary>
        /// The position of the transform.
        /// </summary>
        public Vector3 Position => position;

        /// <summary>
        /// The rotation of the transform.
        /// </summary>
        public Quaternion Rotation => rotation;

        /// <summary>
        /// The scale of the transform.
        /// </summary>
        public Vector3 Scale => scale;

        public static TransformState operator +(TransformState left, TransformState right)
        {
            return new TransformState(left.Position + right.Position, left.Rotation * right.Rotation, Vector3.Scale(left.Scale, right.Scale));
        }

        public static bool operator ==(TransformState left, TransformState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransformState left, TransformState right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Position} | {Rotation} | {scale}";
        }

        /// <summary>
        /// The Z axis of the orientation in world space.
        /// </summary>
        public Vector3 Forward => (Rotation * Vector3.Scale(scale, Vector3.forward)).normalized;

        /// <summary>
        /// The Y axis of the orientation in world space.
        /// </summary>
        public Vector3 Up => (Rotation * Vector3.Scale(scale, Vector3.up)).normalized;

        /// <summary>
        /// The X axis of the orientation in world space.
        /// </summary>
        public Vector3 Right => (Rotation * Vector3.Scale(scale, Vector3.right)).normalized;

        public void ApplyTo(Transform target)
        {
            target.position = position;
            target.rotation = rotation;
            target.localScale = scale;
        }

#region IEqualityComparer Implementation

        /// <inheritdoc />
        bool IEqualityComparer.Equals(object left, object right)
        {
            if (ReferenceEquals(null, left) || ReferenceEquals(null, right)) { return false; }
            if (!(left is TransformState) || !(right is TransformState)) { return false; }
            return ((TransformState)left).Equals((TransformState)right);
        }

        public bool Equals(TransformState other)
        {
            return Position == other.Position &&
                   Rotation.Equals(other.Rotation);
        }
        
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) { return false; }
            return obj is TransformState transform && Equals(transform);
        }

        /// <inheritdoc />
        int IEqualityComparer.GetHashCode(object obj)
        {
            return obj is TransformState transform ? transform.GetHashCode() : 0;
        }

#endregion IEqualityComparer Implementation
    }
}
