﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace PhysiXEngine
{
    class CollisionDetector
    {
        /// <summary>
        /// The World will be needed when the algorithm is reassembled to use spatial
        /// data structures that divide the space.... etc.
        /// </summary>
        // public BoundingBox World { private set; get; }

        private bool didDetect = false;
        private List<Contact> detections = null;

        public LinkedList<Collidable> Shapes { private set; get; }
        public List<Contact> Detections
        {
            get
            {
                if (!didDetect)
                    ReDetect();
                return detections;
            }
        }

        /// <summary>
        /// This method will be available when we use spatial data structures, which will need the
        /// world to divide the space to "rooms".... etc.
        /// </summary>
        /// <param name="World"></param>
        /// <param name="Shapes"></param>
        //public CollisionDetector(BoundingBox World, LinkedList<Collidable> Shapes)
        //{
        //    this.World = World;
        //    this.Shapes = Shapes;
        //    detections = new List<ContactData>(Shapes.Count);   // Shapes.Count is sth arbitrary.
        //}

        public CollisionDetector(LinkedList<Collidable> Shapes)
        {
            this.Shapes = Shapes;
            detections = new List<Contact>(Shapes.Count);   // Shapes.Count is sth arbitrary.
        }


        /// <summary>
        /// Since we are dealing with primatives only now, there is no need for a fine and
        /// coarse collision phases, just using ReDetect to generate the list, but when we
        /// expand the project to accept more complex structures, the whole algorithm will
        /// need reassembling.
        /// </summary>
        public List<Contact> ReDetect()
        {
            didDetect = true;
            BVHNode Hierarchy = new BVHNode(null, Shapes.First.Value);

            /// The following statements guarantee that the LinkedList is not modified.
            /// Other ways: defining a class for the hierarcy (will consume O(n) checks).
            ///             AsList() (will consume a lot).

            Collidable saver = Shapes.First.Value;
            Shapes.RemoveFirst();

            foreach (Collidable shape in Shapes)
            {
                Hierarchy.Insert(shape);
            }

            Shapes.AddFirst(saver);
            detections.Clear();
            Hierarchy.FindPotentialCollisions(detections);

            return detections;
        }
    }
}
