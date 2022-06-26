using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CSGOAimbot.Entity
{
    internal class Entity
    {
        public int health, team;
        public float mag; // magnitude from our location
        public Vector3 feet, head; // space coords for entity
    }
}
