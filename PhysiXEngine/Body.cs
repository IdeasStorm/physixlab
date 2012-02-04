﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using PhysiXEngine.Helpers;

namespace PhysiXEngine
{
    public class Body
    {
        public bool HasFiniteMass { get; private set; }
        private float inverseMass;
        public float InverseMass
        {
            get
            {
                return inverseMass;
            }
            set
            {
                HasFiniteMass = (value != 0.0f);
                inverseMass = value;
            }
        }

        /// <summary>
        /// the Mass of the body in Kg
        /// </summary>
        public float Mass 
        {
            get { return 1.0f / inverseMass; }
            set 
            {
                HasFiniteMass = !float.IsInfinity(value);
                inverseMass = 1.0f / value;
            }
        }


        private Vector3 position;
        public Vector3 Position { 
            get { return position; } 
            set { 
                _oldPosition = position; 
                position = value;
                onSituationChanged();
            } 
        }
        public Vector3 Velocity {  get; protected set; }
        public Vector3 Acceleration {  get; protected set; }
        public Vector3 LastFrameAcceleration {  get; protected set; }
        public Vector3 AngularAcceleration {  get; set; }

        private Vector3 forceAccumulator;
        private Vector3 torqueAccumulator;

        private Matrix inverseInertiaTensor = new Matrix();


        public bool IsAsleep { 
        set
        {
            IsAwake = !value;
        } 
        get
        {
            return !IsAwake;
        }
        }
        public bool IsAwake { set; get; }
        public void Awake()
        {
            IsAwake = true;
        }

        /// <summary>
        /// holds the inertia (independent of the axis)
        /// warning : this is in the body space
        /// </summary>
        public Matrix InverseInertiaTensor {
            get{
                return inverseInertiaTensor;
            }
            set{
                inverseInertiaTensor = value;
            }
        }

        public Matrix InertiaTensor 
        {
            get
            {
                return Matrix.Invert(inverseInertiaTensor);
            }
            set
            {
                inverseInertiaTensor = Matrix.Invert(value);
            }
        }

        /// <summary>
        /// holds the inertia (independent of the axis)
        /// in the world space
        /// </summary>
        public Matrix3 InverseInertiaTensorWorld { get; protected set; }

        private Quaternion _oldOrientation;
        private Quaternion orientation = Quaternion.CreateFromAxisAngle(Vector3.Forward,0);

        /// <summary>
        /// Angular orientation in world space
        /// </summary>
        public Quaternion Orientation {
            set
            {                    
                _oldOrientation = orientation;
                orientation = value;
                onSituationChanged();
            }
            get { return orientation; }
        }

        /// <summary>
        /// stores the old position for this object providing rollingback capability
        /// </summary>
        private Vector3 _oldPosition;

        /// <summary>
        /// the angular velocity
        /// </summary>
        public Vector3 Rotation { get; set; }

        /// <summary>
        /// the matrix that converts a vector from the body space to the world space
        /// </summary>
        public Matrix TransformMatrix { get; protected set; }

        //TODO add angular/linear damping if needed


        public Body()
        {
            Mass = 1;
            InertiaTensor = Matrix.Identity;
            Awake();
            UpdateMatices();
        }

        /// <summary>
        /// updates the body to the next state
        /// </summary>
        /// <param name="duration">the time elapsed from the past frame</param>
        public virtual void Update(float duration)
        {
            if (IsAsleep) return;
            LastFrameAcceleration = Acceleration;
            LastFrameAcceleration += forceAccumulator * inverseMass;
            AngularAcceleration = InverseInertiaTensorWorld.transform(torqueAccumulator);

            Velocity += LastFrameAcceleration * duration;
            Rotation += AngularAcceleration * duration;

            // saving old situation
            _oldPosition = position;
            _oldOrientation= orientation;

            position += Velocity * duration;
            //orientation.AddScaledVector(Rotation, duration);
            if (Rotation.Length() > 0 )
                AddScaledOrientation(Rotation,duration);

            onSituationChanged(); //trigger situation changed
            clearAccumulators();
            // add damping 
        }

        public void AddScaledOrientation(Vector3 rotation,float scale=1)
        {
            //orientation += Quaternion.CreateFromYawPitchRoll(rotation.Y * scale, rotation.X * scale, rotation.Z * scale);
            orientation = orientation.AddScaledVector(rotation, scale);
            onSituationChanged();
        }

        protected void UpdateMatices()
        {
            PhysiXEngine.Helpers.ExtensionMethods.normalized(ref orientation);

            // Calculate the transform matrix for the body.
            TransformMatrix = Matrix.CreateFromQuaternion(Orientation) *  Matrix.CreateTranslation(Position);
            
            // Calculate the inertiaTensor in world space.
            InverseInertiaTensorWorld = TransformMatrix * InverseInertiaTensor;
            //TODO ho is first ?            
        }

        private void clearAccumulators()
        {
            forceAccumulator = Vector3.Zero;
            torqueAccumulator = Vector3.Zero;
        }

        /// <summary>
        /// Adds a force to force accumulator
        /// </summary>
        /// <param name="force">the force to be added </param>
        public void AddForce(Vector3 force)
        {
            forceAccumulator += force;
        }

        /// <summary>
        /// Adds a Velocity vector to the current velocity
        /// </summary>
        /// <param name="velocity"></param>
        public void AddVelocity(Vector3 velocity)
        {
            this.Velocity += velocity;
        }


        public Vector3 GetPointInWorldSpace(Vector3 point)
        {
            return Vector3.Transform(point, TransformMatrix);            
        }

        public void AddForceAtPoint(Vector3 force, Vector3 point)
        {
            // Convert to coordinates relative to center of mass.
            Vector3 pt = point;
            pt -= Position;            

            forceAccumulator += force;
            torqueAccumulator += Vector3.Cross(pt , force);
        }

        /// <summary>
        /// an alias of AddForceAtPoint 
        /// </summary>
        /// <see cref="AddForceAtPoint"/>
        /// <param name="force"></param>
        /// <param name="point"></param>
        public void AddForce(Vector3 force, Vector3 point)
        {
            AddForceAtPoint(force, point);
        }

        /// <summary>
        /// Sets the value of the matrix from inertia tensor values.
        /// </summary>
        protected void setInertiaTensorCoeffs(float ix, float iy, float iz,
            float ixy = 0, float ixz = 0, float iyz = 0)
        {
            Matrix matrix = Matrix.Identity;
            matrix.M11 = ix;
            matrix.M22 = iy;
            matrix.M33 = iz;
            matrix.M12 = matrix.M21 = -ixy;
            matrix.M13 = matrix.M31 = -ixz;
            matrix.M23 = matrix.M32 = -iyz;
            matrix.M44 = 1;
            InverseInertiaTensor = Matrix.Invert(matrix);
        }

        /// <summary>
        /// gets a vector of an axis by index (from 0 to 2)      
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector3 GetAxis(int index)
        {
            return TransformMatrix.GetAxisVector(index);
        }

        /// <summary>
        /// revert Position changes in the last frame
        /// </summary>
        public void RevertChanges()
        {
            position = this._oldPosition;
            orientation = _oldOrientation;
            onSituationChanged();
        }

        /// <summary>
        /// indicates the state of this body (moving or not)
        /// </summary>
        public bool IsMoving {
            get { 
                return (Velocity != Vector3.Zero);
            }
        }

        protected virtual void onSituationChanged()
        {
            UpdateMatices();
        }

        /// <summary>
        /// Make this Body immovable 
        /// warning : if you want to re-Enable Movement, you should Set Mass and inertia
        /// </summary>
        public void Lock()
        {
            inverseInertiaTensor = new Matrix();
            InverseMass = 0;
        }
    }
}
